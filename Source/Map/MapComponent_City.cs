using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Cities {

    public class MapComponent_City : MapComponent {
        const int EventTimeCycle = 40_000;

        public HashSet<Thing> cityOwnedThings = new HashSet<Thing>();

        public MapComponent_City(Map map) : base(map) {
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Collections.Look(ref cityOwnedThings, "cityOwnedThings", LookMode.Reference);
        }

        public override void MapComponentTick() {
            if ((Find.TickManager.TicksGame + map.Parent.ID) % EventTimeCycle == 0) {
                if (!Config_Cities.Instance.enableEvents) {
                    return;
                }

                if (map.Parent is City city) {
                    if (!city.Abandoned) {
                        if (city.Faction.HostileTo(Faction.OfPlayer)) {
                            var storyComp = Find.Storyteller.storytellerComps
                                .First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                            var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
                            parms.faction = city.Faction;
                            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                            parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetRandom();
                            parms.raidArrivalModeForQuickMilitaryAid = true;
                            parms.points += city.RaidPointIncrease;
                            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
                        }

                        if (city.inhabitantFaction != null && city.inhabitantFaction != Faction.OfPlayer) {
                            foreach (var pawn in map.mapPawns.SpawnedPawnsInFaction(city.inhabitantFaction)) {
                                if (!pawn.inventory.innerContainer.Any(thing => pawn.WillEat(thing))) {
                                    for (var i = 0; i < 2; i++) {
                                        var meal = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                                        pawn.inventory.innerContainer.TryAdd(meal);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}