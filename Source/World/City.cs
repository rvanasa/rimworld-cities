using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cities {
    public class City : Settlement {
        public Faction inhabitantFaction;

        public bool Abandoned => inhabitantFaction == null;

        public override MapGeneratorDef MapGeneratorDef => def.mapGenerator;

        public override Texture2D ExpandingIcon => def.ExpandingIconTexture;

        public override Color ExpandingIconColor => inhabitantFaction != null ? base.ExpandingIconColor : Color.grey;

        public override bool Visitable => base.Visitable || Abandoned;

        public override bool Attackable => base.Attackable && !Abandoned;

        public IEnumerable<Quest> QuestsHere => Find.World.GetComponent<WorldComponent_QuestTracker>().quests
            .Where(q => q.Targets.targets.Contains(this));

        public virtual int RaidPointIncrease => 500;

        public virtual TraderKindDef WorldTraderKind => DefDatabase<TraderKindDef>.GetNamed("Base_City");

        public City() {
            trader = new Settlement_TraderTracker(this);
        }

        public override void SetFaction(Faction newFaction) {
            base.SetFaction(newFaction);
            if (inhabitantFaction != null) {
                inhabitantFaction = newFaction;
            }
        }

        protected override void Tick() {
            base.Tick();

            // if (++cityCt > 100) {
            //     cityCt = 0;
            // }
        }

        public override void PostMapGenerate() {
            base.PostMapGenerate();
            if (Abandoned) {
                SetFaction(Faction.OfPlayer);
            }

            GetComponent<TimedDetectionRaids>().ResetCountdown();
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan) {
            if (Visitable) {
                // TODO refactor
                if (!(this is Citadel)) {
                    var enterLabel = Find.World.GetComponent<WorldComponent_QuestTracker>().quests
                        .Where(q => q.Targets != null && q.Targets.targets.Contains(this))
                        .Select(q => q.def.EnterCityLabel)
                        .FirstOrDefault() ?? "EnterCity".Translate();

                    var action = new Command_Action
                    {
                        icon = SettleUtility.SettleCommandTex,
                        defaultLabel = enterLabel,
                        defaultDesc = "EnterCityDesc".Translate(),
                        action = () => {
                            LongEventHandler.QueueLongEvent(() => {
                                var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(Tile, null);
                                CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge,
                                    CaravanDropInventoryMode.DoNotDrop, true);
                            }, "GeneratingMapForNewEncounter", false, null);
                        },
                    };
                    if (this.EnterCooldownBlocksEntering()) {
                        action.Disable("MessageEnterCooldownBlocksEntering".Translate(this.EnterCooldownDaysLeft().ToString("0.#")));
                    }

                    yield return action;
                }

                if (!Abandoned) {
                    yield return CaravanVisitUtility.TradeCommand(caravan);
                }
            }

            foreach (var gizmo in base.GetCaravanGizmos(caravan)) {
                if (gizmo is Command command && command.defaultLabel == "CommandTrade".Translate()) {
                    continue;
                }

                yield return gizmo;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan) {
            foreach (var option in base.GetFloatMenuOptions(caravan)) {
                if (option.Label.Contains("CommandTrade".Translate())) {
                    continue;
                }
                yield return option;
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref inhabitantFaction, "inhabitantFaction");
        }

        public virtual void PreMapGenerate(Map map) {
            SetFaction(inhabitantFaction ?? GenCity.RandomCityFaction(f => f.HostileTo(Faction.OfPlayer)));
        }

        public override string GetInspectString() {
            var s = base.GetInspectString();
            var hasQuests = false;
            foreach (var quest in QuestsHere) {
                if (!hasQuests) {
                    hasQuests = true;
                    s += "\n--";
                }

                var ticksLeft = quest.TicksLeft;
                if (ticksLeft > 0) {
                    s += "\n" + quest.Name + " (" + ticksLeft.ToStringTicksToPeriod() + ")";
                }
            }

            return s;
        }

        public virtual string ChooseName() {
            return SettlementNameGenerator.GenerateSettlementName(this);
        }

        public virtual IntVec3 ChooseMapSize(IntVec3 mapSize) {
            if (Config_Cities.Instance.customCitySize) {
                mapSize.x = Config_Cities.Instance.citySizeScale;
                mapSize.z = Config_Cities.Instance.citySizeScale;
            }
            return mapSize;
        }

        public List<Quest> FindQuests() {
            return Find.World.GetComponent<WorldComponent_QuestTracker>().quests
                .Where(quest => !quest.Ended && quest.Targets.targets.Any(target => target.WorldObject == this))
                .ToList();
        }
    }
}