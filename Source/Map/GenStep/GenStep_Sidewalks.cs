using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public class GenStep_Sidewalks : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public List<TerrainDef> sidewalkTerrains = new List<TerrainDef>();

        protected override void ScatterAt(IntVec3 pos, Map map, int count) {
            var terrain = sidewalkTerrains.RandomElement();

            var s = new Stencil(map).MoveTo(pos).RotateRand();
            if (IsValidTile(map, s.pos)) {
                s.SetTerrain(terrain);
            }

            var s1 = s.Move(0, 1);
            while (s1.Check(s1.pos, IsValidTile)) {
                s1.SetTerrain(terrain);
                s1 = s1.Move(0, 1);
            }

            var s2 = s.Move(0, -1);
            while (s2.Check(s2.pos, IsValidTile)) {
                s2.SetTerrain(terrain);
                s2 = s2.Move(0, -1);
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }
    }
}