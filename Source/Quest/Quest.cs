using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cities {
    public abstract class Quest : IExposable {
        static readonly IntRange ExpirationDaysRange = new IntRange(10, 30);

        public QuestDef def;
        public int expireTime;

        QuestState state = QuestState.BeforeStart;
        Map home;
        List<Result> completeResults = new List<Result>();
        List<Result> cancelResults = new List<Result>();
        List<Result> expireResults = new List<Result>();

        public bool Started => state != QuestState.BeforeStart;
        public bool Ended => state != QuestState.BeforeStart && state != QuestState.Started;
        public bool Expired => state == QuestState.Expired;

        public int TicksLeft => Started ? expireTime - Find.TickManager.TicksGame : -1;

        public virtual int MinCapableColonists => 2;

        public Map HomeMap {
            get {
                UpdateHome();
                return home;
            }
        }

        public virtual string Name => def.letterLabel;

        public abstract LookTargets Targets { get; }

        public virtual Faction Issuer => null;

        public virtual NamedArgument[] FormatArgs => new NamedArgument[0];

        public virtual string DetailText {
            get {
                var text = def.letterText.Formatted(FormatArgs);
                var complete = ResultLabel("QuestResultComplete", completeResults);
                var cancel = ResultLabel("QuestResultCancel", cancelResults);
                var expire = ResultLabel("QuestResultExpire", expireResults);
                if (complete != null) {
                    text += "\n\n" + complete;
                }

                if (cancel != null) {
                    text += "\n\n" + cancel;
                }

                if (expire != null) {
                    text += "\n\n" + expire;
                }

                text += "\n\n" + "QuestExpireTime".Translate().Formatted(TicksLeft.ToStringTicksToPeriod());
                return text;
            }
        }

        string ResultLabel(string format, IEnumerable<Result> results) {
            var labels = results.Select(r => r.Label).Where(s => s != null).ToArray();
            return labels.Length > 0 ? format.Translate().Formatted(labels.ToCommaList()) : null;
        }

        public virtual void ExposeData() {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref expireTime, "expireTime");
            Scribe_Values.Look(ref state, "state");
            Scribe_References.Look(ref home, "home");
            Scribe_Collections.Look(ref completeResults, "completeResults");
            Scribe_Collections.Look(ref cancelResults, "cancelResults");
            Scribe_Collections.Look(ref expireResults, "expireResults");
        }

        protected virtual bool IsValidHome(Map map) {
            return map?.Parent?.Faction != null && !map.ParentFaction.HostileTo(Faction.OfPlayer) &&
                   HasSufficientColonists(map);
        }

        protected virtual bool HasSufficientColonists(Map map) {
            var list = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            var num = 0;
            foreach (var pawn in list) {
                if (pawn.IsFreeColonist && !HealthAIUtility.ShouldSeekMedicalRest(pawn)) {
                    if (++num >= MinCapableColonists) {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual int RandomExpiryTicks() {
            var tileIdFrom = home.Tile;
            var info = Targets.TryGetPrimaryTarget();
            var tileIdTo = info.IsValid ? info.Tile : tileIdFrom;

            var arriveTicks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(tileIdFrom, tileIdTo, null);
            var arriveDays = arriveTicks / 60000F;
            var minDays = Mathf.CeilToInt(Mathf.Max(arriveDays + 6, arriveDays * 1.35F));

            var days = Mathf.Max(ExpirationDaysRange.RandomInRange, Mathf.Min(ExpirationDaysRange.max, minDays));
            return 60000 * days;
        }

        public virtual void UpdateHome() {
            if (home == null || !Find.Maps.Contains(home)) {
                home = Find.Maps.Where(IsValidHome).MaxByWithFallback(m => m.mapPawns.FreeColonistsSpawnedCount)
                       ?? Find.CurrentMap
                       ?? Find.Maps.MaxByWithFallback(m => m.mapPawns.FreeColonistsSpawnedCount)
                       ?? home;
            }
        }

        public virtual void ChooseParts() {
            UpdateHome();
            /*if(home == null) {
                return;
            }*/
            foreach (var part in def.questParts) {
                part.OnChoose(this);
                /*if(!part.IsValid(this)) {
                    return;
                }*/
            }
        }

        public virtual bool AllPartsValid() {
            if (home == null) {
                return false;
            }

            foreach (var part in def.questParts) {
                if (!part.IsValid(this)) {
                    return false;
                }
            }

            return true;
        }

        public virtual bool CanReceiveRandomly() {
            return AllPartsValid() && IsValidHome(home);
        }

        public virtual void Listen(string key, Result result) {
            // TODO: convert to multimap data structure
            if (key == "Complete") {
                completeResults.Add(result);
            }
            else if (key == "Cancel") {
                cancelResults.Add(result);
            }
            else if (key == "Expire") {
                expireResults.Add(result);
            }
            else {
                Log.Error("Unknown quest event: " + key);
            }
        }

        public void Start() {
            if (Started) {
                Log.Error("Quest already started: " + Name);
                return;
            }

            state = QuestState.Started;
            expireTime = Find.TickManager.TicksGame + RandomExpiryTicks();
            Find.World.GetComponent<WorldComponent_QuestTracker>().quests.Add(this);
            foreach (var part in def.questParts) {
                part.OnStart(this);
            }

            OnStart();
        }

        public void Complete() {
            if (!TryEnd(QuestState.Completed, "QuestComplete", MessageTypeDefOf.PositiveEvent)) {
                return;
            }

            foreach (var part in def.questParts) {
                part.OnComplete(this);
            }

            OnComplete();
        }

        public void Cancel() {
            if (!TryEnd(QuestState.Cancelled, "QuestCancelled", MessageTypeDefOf.NegativeEvent)) {
                return;
            }

            foreach (var part in def.questParts) {
                part.OnCancel(this);
            }

            OnCancel();
        }

        public void Expire() {
            if (!TryEnd(QuestState.Expired, "QuestExpired", MessageTypeDefOf.NeutralEvent)) {
                return;
            }

            foreach (var part in def.questParts) {
                part.OnExpire(this);
            }

            OnExpire();
        }

        bool TryEnd(QuestState state, string formatter, MessageTypeDef messageType) {
            if (!Started) {
                Log.Error("Could not set quest to " + state + " (not started): " + Name);
                return false;
            }

            if (Ended) {
                Log.Error("Could not set quest to " + state + " (already ended): " + Name);
                return false;
            }

            this.state = state;
            UpdateHome(); ///
            OnEnd();
            Messages.Message(formatter.Translate().Formatted(Name), Targets, messageType);
            return true;
        }

        public void MapGenerated(Map map) {
            UpdateHome();
            foreach (var part in def.questParts) {
                part.OnMapGenerated(this);
            }

            OnMapGenerated(map);
        }

        public void MapRemoved(Map map) {
            UpdateHome();
            foreach (var part in def.questParts) {
                part.OnMapRemoved(this);
            }

            OnMapRemoved(map);
        }

        protected bool AtInterval(int interval) {
            return (Find.TickManager.TicksGame + def.shortHash) % interval == 0;
        }

        public virtual void OnStart() {
        }

        public virtual void OnEnd() {
        }

        public virtual void OnComplete() {
        }

        public virtual void OnCancel() {
        }

        public virtual void OnExpire() {
        }

        public virtual void OnTick() {
        }

        public virtual void OnMapGenerated(Map map) {
        }

        public virtual void OnMapRemoved(Map map) {
        }

        public virtual IEnumerable<Gizmo> GetGizmos(WorldObject obj) {
            yield break;
        }

        public virtual IEnumerable<Gizmo> GetGizmos(Thing thing) {
            yield break;
        }

        public virtual IEnumerable<FloatMenuOption> GetFloatMenuOptions(Thing thing, Pawn pawn) {
            yield break;
        }
    }
}