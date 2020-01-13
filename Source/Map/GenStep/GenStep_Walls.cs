using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;

namespace Cities {
    public class GenStep_Walls : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        protected override void ScatterAt(IntVec3 pos, Map map, int count) {
            var s = new Stencil(map);
            s = s.BoundTo(CellRect.FromLimits(pos, s.bounds.RandomCell));
            var stuff = GenCity.RandomWallStuff(map);
            for (var dir = 0; dir < 4; dir++) {
                var sDir = s.Rotate(dir);
                sDir.Fill(sDir.MinX, sDir.MaxZ, sDir.MaxX, sDir.MaxZ, ThingDefOf.Wall, stuff, p => IsValidTile(map, p));
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNatural(pos.GetTerrain(map));
        }
    }
}