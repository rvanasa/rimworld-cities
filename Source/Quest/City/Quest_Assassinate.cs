using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public class Quest_Assassinate : Quest {
        City city;
        Pawn pawn;

        public override LookTargets Targets => city;

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {pawn.Name.ToStringFull, pawn.Name.ToStringShort, city.Faction.Name, city.Name};

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref city, "city");
            Scribe_References.Look(ref pawn, "pawn");
        }

        public override void ChooseParts() {
            base.ChooseParts();
            city = Find.WorldObjects.Settlements
                .OfType<City>()
                .Where(s => s.Visitable && !s.Abandoned && !s.HasMap)
                .RandomByDistance(HomeMap?.Parent, 50);
            if (city == null) {
                return;
            }

            var faction = city.Faction;
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(faction.RandomPawnKind(), faction,
                PawnGenerationContext.NonPlayer, city.Tile));
        }
        
        public override bool AllPartsValid() {
            return base.AllPartsValid() && city != null && pawn != null;
        }

        public override void OnMapGenerated(Map map) {
            if (map.Parent == city) {
                if (!pawn.Spawned) {
                    GenSpawn.Spawn(pawn, CellRect.WholeMap(map).ExpandedBy(-20).RandomCell, map);
                }
                if (pawn.GetLord() == null) {
                    var lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_LiveInCity(), map);
                    lord.AddPawn(pawn);
                }
                var bed = map.listerThings.AllThings.OfType<Building_Bed>().Where(b => !b.ForPrisoners && !b.Medical)
                    .RandomElementWithFallback();
                if (bed != null) {
                    bed.SetFactionDirect(pawn.Faction);
                    bed.TryAssignPawn(pawn);
                }
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == city) {
                Cancel();
            }
        }

        public override void OnStart() {
            base.OnStart();
            
            Find.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        }

        public override void OnTick() {
            if (AtInterval(100)) {
                if (pawn != null && pawn.Dead) {
                    Complete();
                }
                else if (city.HasMap && (pawn == null || !pawn.SpawnedOrAnyParentSpawned)) {
                    Cancel();
                }
            }
        }

        public override void OnEnd() {
            Find.World.worldPawns.ForcefullyKeptPawns.Remove(pawn);
        }
    }
}