using RimWorld;
using Verse;

namespace Cities {

    public class GenStep_NarrowGarages : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange heightRange = new IntRange(10, 30);
        public IntRange spacingRange = new IntRange(5, 8);

        protected override void ScatterAt(IntVec3 pos, Map map, GenStepParams parms, int count) {
            var height = heightRange.RandomInRange;
            var spacing = spacingRange.RandomInRange;

            var s = new Stencil(map).MoveTo(pos);
            s = s.Bound(s.MinX, 0, s.MaxX, height)
                // .Border(ThingDefOf.Wall, GenCity.RandomWallStuff(map))
                .FillRoof(RoofDefOf.RoofConstructed);

            // s.Expand(1)
            //     .FillTerrain(GenCity.RandomFloor(map), IsValidTile);

            for (var i = s.MinX + spacingRange.RandomInRange; i <= s.MaxX; i += spacing) {
                for (var j = s.MinZ + spacingRange.RandomInRange; j <= s.MaxZ; j += spacing) {
                    var point = s.pos + new IntVec3(i, 0, j);
                    if (IsValidTile(map, point)) {
                        s.MoveTo(point)
                            .Spawn(ThingDefOf.Column, GenCity.RandomStuff(ThingDefOf.Column, map));
                    }
                }
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }
    }
}