using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Cities {
    public class Quest_Defend : Quest {
        const int MaxStageCount = 8;
        const int CheckInterval = 1000;
        static readonly IntRange StageInterval = new IntRange(5_000, 10_000);

        Faction enemyFaction;
        City city;
        int stage;
        int ticksTillNextStage = Rand.RangeInclusive(3_000, 5_000);

        public override int MinCapableColonists => 5;

        public override LookTargets Targets => city;

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {enemyFaction.Name, city.Faction.Name, city.Name};

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref enemyFaction, "enemyFaction");
            Scribe_References.Look(ref city, "city");
            Scribe_Values.Look(ref stage, "stage");
            Scribe_Values.Look(ref ticksTillNextStage, "ticksTillNextStage");
        }

        public override void ChooseParts() {
            base.ChooseParts();
            enemyFaction = Find.FactionManager.RandomEnemyFaction();
            city = Find.WorldObjects.Settlements
                .OfType<City>()
                .Where(s => s.Visitable && !s.Abandoned && !s.HasMap)
                .RandomByDistance(HomeMap?.Parent,50) ;
        }

        public override bool AllPartsValid() {
            return base.AllPartsValid() && enemyFaction != null && city != null;
        }

        public override void OnMapGenerated(Map map) {
            if (map.Parent == city) {
                map.GetComponent<MapComponent_City>().cityOwnedThings.Clear();
                var playerFaction = Faction.OfPlayer;
                foreach (var thing in map.listerThings.AllThings) {
                    if (!(thing is Pawn) && thing.def.CanHaveFaction) {
                        thing.SetFactionDirect(playerFaction);
                    }
                }
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == city) {
                Cancel();
            }
        }

        public override void OnTick() {
            if (AtInterval(CheckInterval)) {
                var map = city.Map;
                if (map != null) {
                    ticksTillNextStage -= CheckInterval;
                    if (ticksTillNextStage <= 0) {
                        if (stage < MaxStageCount) {
                            stage++;
                            ticksTillNextStage += StageInterval.RandomInRange;
                            city.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 100);
                            var storyComp = Find.Storyteller.storytellerComps.First(x =>
                                x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                            var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
                            parms.faction = enemyFaction;
                            parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
                            parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetRandom();
                            parms.points += Mathf.RoundToInt((5 + stage) * 1000);
                            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);

                            Messages.Message("QuestDefendWave".Translate().Formatted(stage, MaxStageCount),
                                MessageTypeDefOf.ThreatBig);
                        }
                        else {
                            //var hasEnemy = false;
                            //var playerFaction = Faction.OfPlayer;
                            //foreach(var pawn in map.mapPawns.AllPawnsSpawned) {
                            //	if(!pawn.IsPrisoner && pawn.Faction.HostileTo(playerFaction)) {
                            //		hasEnemy = true;
                            //		break;
                            //	}
                            //}
                            //if(!hasEnemy) {
                            Complete();
                            //}
                        }
                    }
                }
            }
        }

        public override void OnComplete() {
            var map = city.Map;
            if (map == null) {
                return;
            }

            var faction = map.ParentFaction;
            var playerFaction = Faction.OfPlayer;
            //map.Parent.SetFaction(playerFaction);
            faction.TryAffectGoodwillWith(playerFaction, 200);
            /*foreach(var pawn in map.mapPawns.AllPawns) {
                if(pawn.Faction == faction) {
                    pawn.SetFactionDirect(playerFaction);
                }
            }*/
        }

        public override void OnCancel() {
            OnExpire();
        }

        public override void OnExpire() {
            city.SetFaction(enemyFaction);
        }
    }

    public class Quest_PrisonBreak : Quest {
        //const int MaxPrisoners = 5;

        City city;

        public override int MinCapableColonists => 1;

        public override LookTargets Targets => city;

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {city.Faction.Name, city.Name};

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref city, "city");
        }

        public override void ChooseParts() {
            base.ChooseParts();
            city = Find.WorldObjects.Settlements
                .Where(s => s is City city && city.Visitable && !city.Abandoned
                            && QuestUtility.Reachable(HomeMap?.Parent, s, 50)
                            && !s.HasMap)
                .RandomElementWithFallback() as City;
        }

        public override bool AllPartsValid() {
            return base.AllPartsValid() && city != null;
        }

        public override void OnMapGenerated(Map map) {
            if (map.Parent == city) {
                var playerFaction = Faction.OfPlayer;
                var count = 0;
                foreach (var pawn in map.mapPawns.AllPawnsSpawned) {
                    if (pawn.IsPrisoner) {
                        count++;
                        pawn.mindState.WillJoinColonyIfRescued = true;
                        //pawn.guest = null;
                        //if(count < MaxPrisoners) {
                        //	pawn.SetFactionDirect(playerFaction);
                        //}
                    }
                }

                if (count > 0) {
                    Complete();
                }
                else {
                    Cancel();
                }
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == city) {
                Cancel();
            }
        }

        public override void OnComplete() {
            city.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -200);
        }
    }
}