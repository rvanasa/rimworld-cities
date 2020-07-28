using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public class Quest_Hostages : Quest {
        City city;
        List<Pawn> hostages;
        List<Pawn> captors;

        bool _enteredCity;

        public override LookTargets Targets => new LookTargets(city, hostages[0]);

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {city.Name, hostages.Count};

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref city, "city");
            Scribe_Collections.Look(ref hostages, "hostages", LookMode.Reference);
            Scribe_Collections.Look(ref captors, "captors", LookMode.Reference);
        }

        public override void ChooseParts() {
            base.ChooseParts();
            city = Find.WorldObjects.Settlements
                .OfType<City>()
                .Where(s => s.Visitable && !s.Abandoned && !s.HasMap && !(s is Citadel))
                .RandomByDistance(HomeMap?.Parent, Rand.RangeInclusive(20, 100));
            if (city == null) {
                return;
            }

            var faction = city.Faction;
            var hostageCt = Rand.RangeInclusive(2, 4);
            hostages = new List<Pawn>();
            for (var i = 0; i < hostageCt; i++) {
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Slave, faction, PawnGenerationContext.NonPlayer, city.Tile));
                pawn.equipment?.DestroyAllEquipment();
                hostages.Add(pawn);
            }

            var captorFaction = GenCity.RandomCityFaction(f => !faction.def.CanEverBeNonHostile)
                                ?? GenCity.RandomCityFaction(f => f.HostileTo(Faction.OfPlayer));
            var captorCt = hostageCt + 3;
            captors = new List<Pawn>();
            for (var i = 0; i < captorCt; i++) {
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(faction.RandomPawnKind(), captorFaction, PawnGenerationContext.NonPlayer, city.Tile));
                captors.Add(pawn);
            }
        }

        public override bool AllPartsValid() {
            return base.AllPartsValid() && city != null && hostages != null && captors != null;
        }

        protected override void OnSetupHandle(RimWorld.Quest handle) {
            handle.AddPart(new QuestPart_CityQuest {
                targets = new GlobalTargetInfo[] {city},
            });
        }

        public override void OnMapGenerated(Map map) {
            if (map.Parent == city) {
                var bed = map.listerThings.AllThings.OfType<Building_Bed>()
                    .Where(b => !b.ForPrisoners && !b.Medical)
                    .RandomElementWithFallback();
                if (bed == null) {
                    Cancel();
                    return;
                }

                foreach (var pawn in hostages) {
                    if (!pawn.Spawned) {
                        GenSpawn.Spawn(pawn, bed.Position, map);
                    }
                    if (pawn.GetLord() == null) {
                        var lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_Hostage(), map);
                        lord.AddPawn(pawn);
                    }
                }

                foreach (var pawn in captors) {
                    if (!pawn.Spawned) {
                        GenSpawn.Spawn(pawn, bed.Position, map);
                    }
                    if (pawn.GetLord() == null) {
                        var lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_Captor(hostages.RandomElement()), map);
                        lord.AddPawn(pawn);
                    }
                }

                var captorFaction = captors[0].Faction;
                foreach (var pos in CellRect.CenteredOn(bed.Position, 10)) {
                    if (pos.InBounds(map)) {
                        foreach (var thing in pos.GetThingList(map)) {
                            if (thing is Building_Door) {
                                thing.SetFactionDirect(captorFaction);
                            }
                        }
                    }
                }

                bed.Destroy();
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == city) {
                Cancel();
            }
        }

        public override void OnStart() {
            base.OnStart();

            foreach (var pawn in hostages.Concat(captors)) {
                Find.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
        }

        public override void OnComplete() {
            city.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 20);
            foreach (var pawn in hostages) {
                pawn.SetFactionDirect(Faction.OfPlayer);
            }
        }

        public override void OnTick() {
            if (AtInterval(100)) {
                if (city.HasMap) {
                    if (!_enteredCity) {
                        _enteredCity = true;
                        Find.LetterStack.ReceiveLetter("QuestTargetLocated".Translate(), "QuestTargetLocatedMessage".Translate(), LetterDefOf.NegativeEvent, hostages[0]);
                    }

                    if (hostages.All(p => !p.Spawned || p.Dead)) {
                        Cancel();
                    }
                    else if (captors.All(p => !p.Spawned || p.Downed)) {
                        Complete();
                    }
                }
            }
        }

        public override void OnEnd() {
            foreach (var pawn in hostages.Concat(captors)) {
                Find.World.worldPawns.ForcefullyKeptPawns.Remove(pawn);
            }
        }
    }
}