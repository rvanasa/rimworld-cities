using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace Cities {

	[StaticConstructorOnStartup]
	public static class QuestIcons {
		public static readonly Texture2D InfoIcon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");
		//public static readonly Texture2D AttackIcon = ContentFinder<Texture2D>.Get("UI/Commands/SquadAttack");
	}

	public static class QuestUtility {
		public static bool Reachable(WorldObject from, WorldObject to, int minDist = int.MaxValue) {
			if(from == null || to == null) {
				return false;
			}
			return Reachable(from.Tile, to.Tile, minDist);
		}

		public static bool Reachable(int from, int to, int minDist = int.MaxValue) {
			return Find.WorldGrid.ApproxDistanceInTiles(from, to) < minDist && Find.WorldReachability.CanReach(from, to);
		}

		public static IntVec3 FindDropSpot(Map map, int dropArea = 10) {
			var attempts = 20;
			Stencil s;
			do {
				s = new Stencil(map).MoveRand()
					.ExpandRegion(p => p.GetFirstThing<Thing>(map) == null, dropArea)
					.Center();
				if(s.Area >= dropArea) {
					break;
				}
			}
			while(--attempts >= 0);
			return s.pos;
		}
	}

	public class QuestDef : IncidentDef {
		public System.Type questClass;
		public List<QuestPart> questParts = new List<QuestPart>();

		public QuestDef() {
			workerClass = typeof(IncidentWorker_Quest);
			questClass = typeof(Quest);
			//category = IncidentCategoryDefOf.WorldQuest;
			//targetTags = new List<IncidentTargetTagDef>(new[] { IncidentTargetTagDefOf.World });
		}
	}

	public abstract class QuestPart {
		public virtual void OnChoose(Quest quest) {
		}
		public virtual bool IsValid(Quest quest) {
			return true;
		}
		public virtual void OnStart(Quest quest) {
		}
		public virtual void OnComplete(Quest quest) {
		}
		public virtual void OnCancel(Quest quest) {
		}
		public virtual void OnExpire(Quest quest) {
		}
		public virtual void OnMapGenerated(Quest quest) {
		}
		public virtual void OnMapRemoved(Quest quest) {
		}
	}

	public abstract class QuestListener : QuestPart {
		public List<string> events = new List<string>();

		public override void OnStart(Quest quest) {
			foreach(var result in GetResults(quest)) {
				foreach(var key in events) {
					quest.Listen(key, result);
				}
			}
		}

		public abstract IEnumerable<Result> GetResults(Quest quest);
	}

	public class QuestListener_GiveThings : QuestListener {
		public FloatRange value = new FloatRange(1000, 2000);

		public List<ThingSetMakerDef> thingSetMakerOptions = new List<ThingSetMakerDef>();

		public override IEnumerable<Result> GetResults(Quest quest) {
			var maker = thingSetMakerOptions.RandomElementWithFallback()
				?? ThingSetMakerDefOf.Reward_StandardByDropPod;
			var things = maker.root.Generate(new ThingSetMakerParams {
				totalMarketValueRange = value,
			});
			yield return new DropPodResult(things);
		}
	}

	public class QuestListener_Message : QuestListener {
		public string letter;
		public string message;
		public MessageTypeDef messageType;

		public List<ThingSetMakerDef> thingSetMakerOptions = new List<ThingSetMakerDef>();

		public override IEnumerable<Result> GetResults(Quest quest) {
			yield return new MessageResult(message, messageType ?? MessageTypeDefOf.NeutralEvent);
			if(letter != null) {
				yield return new LetterResult(letter, message, DefDatabase<LetterDef>.GetNamed(messageType.defName) ?? LetterDefOf.NeutralEvent);
			}
		}
	}

	public abstract class Result : IExposable {
		public virtual string Label => null;

		public virtual void ExposeData() {
		}

		public virtual void OnResult(Quest quest) {
		}
	}

	public class DropPodResult : Result {
		public List<Thing> things;

		public override string Label => GenThing.ThingsToCommaList(things) + " (" + "QuestAppendWorth".Translate().Formatted(GenThing.GetMarketValue(things).ToStringMoney()) + ")";

		public DropPodResult() {
		}

		public DropPodResult(List<Thing> things) {
			this.things = things;
		}

		public override void ExposeData() {
			Scribe_Collections.Look(ref things, "things", LookMode.Deep);
		}

		public override void OnResult(Quest quest) {
			var map = quest.HomeMap;
			var pos = QuestUtility.FindDropSpot(map);
			DropPodUtility.DropThingsNear(pos, map, things, canRoofPunch: false);
			Messages.Message("QuestReceived".Translate().Formatted(Label), new LookTargets(pos, map), MessageTypeDefOf.PositiveEvent);
		}
	}

	public class MessageResult : Result {
		public string message;
		public MessageTypeDef type;

		public MessageResult() {
		}

		public MessageResult(string message, MessageTypeDef type) {
			this.message = message;
			this.type = type;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref message, "message");
			Scribe_Defs.Look(ref type, "type");
		}

		public override void OnResult(Quest quest) {
			var text = message.Formatted(quest.FormatArgs);
			Messages.Message(text, type);
		}
	}

	public class LetterResult : Result {
		public string label;
		public string text;
		public LetterDef type;

		public LetterResult() {
		}

		public LetterResult(string label, string text, LetterDef type) {
			this.label = label;
			this.text = text;
			this.type = type;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref label, "label");
			Scribe_Values.Look(ref text, "text");
			Scribe_Defs.Look(ref type, "type");
		}

		public override void OnResult(Quest quest) {
			var text = this.text.Formatted(quest.FormatArgs);
			if(label != null) {
				Find.LetterStack.ReceiveLetter(label, text, type);
			}
		}
	}

	public abstract class Quest : IExposable {
		static readonly IntRange ExpirationDaysRange = new IntRange(10, 30);

		public QuestDef def;
		public int expireTime;

		QuestState state = QuestState.BeforeStart;
		Map home;
		List<Result> completeResults = new List<Result>();
		List<Result> cancelResults = new List<Result>();
		List<Result> expireResults = new List<Result>();

		// public QuestState State => state;
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
				if(complete != null) {
					text += "\n\n" + complete;
				}
				if(cancel != null) {
					text += "\n\n" + cancel;
				}
				if(expire != null) {
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
			return map?.Parent?.Faction != null && !map.ParentFaction.HostileTo(Faction.OfPlayer) && HasSufficientColonists(map);
		}

		protected virtual bool HasSufficientColonists(Map map) {
			var list = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
			int num = 0;
			for(int i = 0; i < list.Count; i++) {
				if(list[i].IsFreeColonist && !HealthAIUtility.ShouldSeekMedicalRest(list[i])) {
					if(++num >= MinCapableColonists) {
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
			if(home == null || !Find.Maps.Contains(home)) {
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
			foreach(var part in def.questParts) {
				part.OnChoose(this);
				/*if(!part.IsValid(this)) {
					return;
				}*/
			}
		}

		public virtual bool AllPartsValid() {
			if(home == null) {
				return false;
			}
			foreach(var part in def.questParts) {
				if(!part.IsValid(this)) {
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
			if(key == "Complete") {
				completeResults.Add(result);
			}
			else if(key == "Cancel") {
				cancelResults.Add(result);
			}
			else if(key == "Expire") {
				expireResults.Add(result);
			}
			else {
				Log.Error("Unknown quest event: " + key);
			}
		}

		public void Start() {
			if(Started) {
				Log.Error("Quest already started: " + Name);
				return;
			}
			state = QuestState.Started;
			expireTime = Find.TickManager.TicksGame + RandomExpiryTicks();
			Find.World.GetComponent<WorldComponent_QuestTracker>().quests.Add(this);
			foreach(var part in def.questParts) {
				part.OnStart(this);
			}
			OnStart();
		}

		public void Complete() {
			if(!TryEnd(QuestState.Completed, "QuestComplete", MessageTypeDefOf.PositiveEvent)) {
				return;
			}
			foreach(var part in def.questParts) {
				part.OnComplete(this);
			}
			OnComplete();
		}

		public void Cancel() {
			if(!TryEnd(QuestState.Cancelled, "QuestCancelled", MessageTypeDefOf.NegativeEvent)) {
				return;
			}
			foreach(var part in def.questParts) {
				part.OnCancel(this);
			}
			OnCancel();
		}

		public void Expire() {
			if(!TryEnd(QuestState.Expired, "QuestExpired", MessageTypeDefOf.NeutralEvent)) {
				return;
			}
			foreach(var part in def.questParts) {
				part.OnExpire(this);
			}
			OnExpire();
		}

		bool TryEnd(QuestState state, string formatter, MessageTypeDef messageType) {
			if(!Started) {
				Log.Error("Could not set quest to " + state + " (not started): " + Name);
				return false;
			}
			if(Ended) {
				Log.Error("Could not set quest to " + state + " (already ended): " + Name);
				return false;
			}
			this.state = state;
			UpdateHome();///
			OnEnd();
			Messages.Message(formatter.Translate().Formatted(Name), Targets, messageType);
			return true;
		}

		public void MapGenerated(Map map) {
			UpdateHome();
			foreach(var part in def.questParts) {
				part.OnMapGenerated(this);
			}
			OnMapGenerated(map);
		}

		public void MapRemoved(Map map) {
			UpdateHome();
			foreach(var part in def.questParts) {
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

	public enum QuestState {
		BeforeStart,
		Started,
		Completed,
		Cancelled,
		Expired,
	}
}