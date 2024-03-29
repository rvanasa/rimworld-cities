using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
    public class Quest_Sabotage : Quest {
        City target;

        public override int MinCapableColonists => 3;

        public override LookTargets Targets => target;

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {target.Faction.Name, target.Name};

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref target, "target");
        }

        public override void ChooseParts() {
            base.ChooseParts();
            target = Find.WorldObjects.Settlements
                .OfType<City>()
                .Where(s => s.Visitable && s.inhabitantFaction != null && s.inhabitantFaction.PlayerGoodwill < 50 && !s.HasMap && !(s is Citadel) && s.FindQuests().Count == 0)
                .RandomByDistance(HomeMap?.Parent, 80);
        }

        public override bool AllPartsValid() {
            return base.AllPartsValid() && target != null && Config_Cities.Instance.enableTurrets;
        }

        protected override void OnSetupHandle(RimWorld.Quest handle) {
            handle.AddPart(new QuestPart_CityQuest {
                targets = new GlobalTargetInfo[] {target},
            });
        }

        public override void OnMapGenerated(Map map) {
            if (map.Parent == target) {
                var playerFaction = Faction.OfPlayer;
                foreach (var thing in map.listerThings.AllThings) {
                    if (IsDefense(thing)) {
                        thing.SetFactionDirect(playerFaction);
                        //map.designationManager.AddDesignation(new Designation(thing, DesignationDefOf.Deconstruct));
                    }
                }
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == target) {
                Cancel();
            }
        }

        public override void OnTick() {
            if (AtInterval(200)) {
                var map = target.Map;
                if (map == null) {
                    return;
                }
                var hasDefenses = false;
                foreach (var thing in map.listerThings.AllThings) {
                    if (IsDefense(thing)) {
                        hasDefenses = true;
                        Log.Warning("Building remaining: " + thing + " :: " + thing.Position);
                        break;
                    }
                }
                if (!hasDefenses) {
                    Complete();
                }
            }
        }

        public override void OnComplete() {
            var faction = Find.FactionManager.RandomNonHostileFaction(minTechLevel: TechLevel.Industrial)
                          ?? FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms() {
                              factionDef = FactionDefOf.Empire,
                          });

            faction.TryAffectGoodwillWith(Faction.OfPlayer, 50);
            target.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -50);
            faction.TryAffectGoodwillWith(target.Faction, -200);
            target.SetFaction(faction);

            var map = target.Map;
            if (map == null) {
                return;
            }
            var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.raidArrivalModeForQuickMilitaryAid = true;
            parms.points = 4000;
            IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
        }

        bool IsDefense(Thing thing) {
            return thing is Building_Turret && thing.def.CanHaveFaction;
        }
    }
}