using RimWorld;
using Verse;

namespace Cities {
    public class BuildingDecorator_Sandbags : BuildingDecorator {
        public int distance = 2;

        public override void Decorate(Stencil s) {
            var stuff = GenCity.RandomStuff(ThingDefOf.Sandbags, s.map);
            for (var dir = 0; dir < 4; dir++) {
                var sDir = s.Rotate(dir);
                sDir.Bound(sDir.MinX - distance, sDir.MaxZ + distance, sDir.MinX / 2, sDir.MaxZ + distance)
                    .Fill(ThingDefOf.Sandbags, stuff, mask: IsValidTile);
                sDir.Bound(sDir.MaxX / 2, sDir.MaxZ + distance, sDir.MaxX + distance, sDir.MaxZ + distance)
                    .Fill(ThingDefOf.Sandbags, stuff, mask: IsValidTile);
            }
        }

        bool IsValidTile(Map map, IntVec3 point) {
            return TerrainUtility.IsNatural(point.GetTerrain(map));
        }
    }
}