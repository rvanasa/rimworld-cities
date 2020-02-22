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

        int abandonCt;
        bool isInhabitantFactionDefined;

        // Quest Tab mod metadata
        // bool QuestTab_IsQuest => QuestsHere.Any();
        // string QuestTab_Label => Name;
        // int QuestTab_Hostility => Faction.IsPlayer ? -1000 : -Faction.PlayerGoodwill;
        // int QuestTab_TicksLeft => QuestsHere
        //     .Where(q => q.TicksLeft > 0)
        //     .TryMinBy(q => q.TicksLeft, out var min)
        //     ? min.TicksLeft
        //     : -1;

        public City() {
            trader = new Settlement_TraderTracker(this);
        }

        public override void SetFaction(Faction newFaction) {
            base.SetFaction(newFaction);
            if (inhabitantFaction != null) {
                inhabitantFaction = newFaction;
            }
            isInhabitantFactionDefined = true;
        }

        // public override void PostMake() {
        //     base.PostMake();
        //     if (Abandoned) {
        //         trader = null;
        //     }
        // }

        public override void Tick() {
            base.Tick();

            if (abandonCt > 100) {
                abandonCt = 0;
                if (Abandoned) {
                    trader = null;
                }
            }
        }

        public override void PostMake() {
            base.PostMake();
            if (Faction == null) {
                base.SetFaction(Find.FactionManager?.AllFactions
                    .Where(f => f.def.humanlikeFaction && !f.def.techLevel.IsNeolithicOrWorse())
                    .RandomElementWithFallback());
            }
            isInhabitantFactionDefined = false;
        }

        public override void PostAdd() {
            base.PostAdd();
            if (!isInhabitantFactionDefined || inhabitantFaction == null) {
                inhabitantFaction = Faction;
            }
        }

        public override void PostMapGenerate() {
            base.PostMapGenerate();
            if (Abandoned) {
                SetFaction(Faction.OfPlayer);
            }
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan) {
            if (Visitable) {
                var action = new Command_Action {
                    icon = SettleUtility.SettleCommandTex,
                    defaultLabel = "EnterCity".Translate(),
                    defaultDesc = "EnterCityDesc".Translate(),
                    action = () => {
                        LongEventHandler.QueueLongEvent(() => {
                            var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(Tile, null);
                            CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge,
                                CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
                        }, "GeneratingMapForNewEncounter", false, null);
                    },
                };
                if (this.EnterCooldownBlocksEntering()) {
                    action.disabled = true;
                    action.disabledReason =
                        "MessageEnterCooldownBlocksEntering".Translate(this.EnterCooldownDaysLeft().ToString("0.#"));
                }

                yield return action;

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
            Scribe_References.Look(ref inhabitantFaction, "inhabitantFaction");
            base.ExposeData();
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
            if (Config_Cities.Instance.limitCitySize) {
                mapSize.x = Mathf.Min(mapSize.x, 200);
                mapSize.z = Mathf.Min(mapSize.z, 200);
            }
            return mapSize;
        }
    }
}