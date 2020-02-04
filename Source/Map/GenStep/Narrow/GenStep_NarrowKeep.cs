using Cities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowKeep : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public float heightRatio = 2;

        public override void Generate(Map map, GenStepParams parms) {
            var s = new Stencil(map);
            s = s.Bound(s.MinX, s.MaxZ - Mathf.RoundToInt(map.Size.x * heightRatio), s.MaxX, s.MaxZ)
                .ClearThingsInBounds()
                .Center();

            s.FillTerrain(GenCity.RandomFloor(map));
            s.Fill(s.MinX, s.MinZ, s.MaxX, s.MinZ, ThingDefOf.Wall, GenCity.RandomWallStuff(map, true));
        }

        // bool IsValidTile(Map map, IntVec3 pos) {
        //     return TerrainUtility.IsNatural(pos.GetTerrain(map));
        // }
    }
}