using RimWorld;
using Verse;

namespace Cities
{
    public class RoomDecorator_Bedroom : RoomDecorator {
        public float headTableChance = 0.9F;
        public float dresserChance = 0.9F;
        public float pawnInBedroomChance = 0.2F;

        public RoomDecorator_Bedroom() {
            lightChance = 0;
        }

        public override void Decorate(Stencil s) {
            var stuff = GenCity.RandomStuff(ThingDefOf.Bed, s.map);
            var sDresser = s.Rotate(s.RandInclusive(0, 1) * 2);
            sDresser.Spawn(s.RandInclusive(sDresser.MinX + 1, sDresser.MaxX - 1), sDresser.MinZ + 1, DefDatabase<ThingDef>.GetNamed("Dresser"), stuff);
            var bedX = s.RandX;
            s.Spawn(bedX, s.MinZ, DefDatabase<ThingDef>.GetNamed("EndTable"), stuff);
            var bed = (Building_Bed)s.Spawn(bedX, s.MinZ + 1, ThingDefOf.Bed, stuff);
            bed.SetFactionDirect(s.map.ParentFaction);
            var pawn = GenCity.SpawnInhabitant(s.Chance(pawnInBedroomChance) ? s.pos : s.MapBounds.RandomCell, s.map, null, randomWorkSpot: true);
            bed.TryAssignPawn(pawn);
        }
    }
}