using RimWorld;
using UnityEngine;
using Verse;

namespace Cities {
    public class BuildingDecorator_Patio : BuildingDecorator {
        public IntRange distanceRange = new IntRange(2, 3);
        public IntRange columnSpacingRange = new IntRange(3, 8);

        public override void Decorate(Stencil s) {
            var floorStuff = GenCity.RandomFloor(s.map);
            var columnStuff = GenCity.RandomStuff(ThingDefOf.Column, s.map);
            var distance = distanceRange.RandomInRange;
            for (var dir = 0; dir < 4; dir++) {
                var sDir = s.Rotate(dir);
                sDir = sDir.Bound(sDir.MinX, sDir.MaxZ + 1, sDir.MaxX, sDir.MaxZ + distance);
                if (dir % 2 == 0) {
                    sDir = sDir.Expand(distance, 0, distance, 0);
                }

                var spacing = columnSpacingRange.RandomInRange;
                for (var x = sDir.MinX + Rand.Range(0, spacing); x <= sDir.MaxX; x += spacing) {
                    var sCol = sDir.Move(x, sDir.MaxZ);
                    if (sCol.IsInMap() && IsValidTile(s.map, sCol.pos)) {
                        sCol.Spawn(ThingDefOf.Column, columnStuff);
                    }
                }

                sDir.FillRoof(RoofDefOf.RoofConstructed)
                    .FillTerrain(floorStuff, IsValidTile);
            }
        }

        bool IsValidTile(Map map, IntVec3 point) {
            return TerrainUtility.IsNatural(point.GetTerrain(map));
        }
    }
}