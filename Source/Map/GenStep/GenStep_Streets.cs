using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public class GenStep_Streets : GenStep {
        public IntRange centerRadius = new IntRange(5, 15);
        public IntRange roadSpacing = new IntRange(30, 50);
        public float roadChance = 0.5F;
        public List<TerrainDef> roadTerrains = new List<TerrainDef>();
        public List<TerrainDef> divTerrains = new List<TerrainDef>();
        public List<TerrainDef> sidewalkTerrains = new List<TerrainDef>();

        public override int SeedPart => GetType().Name.GetHashCode();

        public override void Generate(Map map, GenStepParams parms) {
            var roadTerrain = roadTerrains.RandomElement();
            var divTerrain = divTerrains.RandomElement();
            var sidewalkTerrain = sidewalkTerrains.RandomElement();

            var radius = centerRadius.RandomInRange;

            var s = new Stencil(map);
            s = s.MoveTo(s.Expand(-radius - 2).bounds.RandomCell);

            s.Bound(-radius, -radius, radius, radius)
                .ClearThingsInBounds()
                .FillTerrain(sidewalkTerrains.RandomElement());

            s.FillTerrain(-2, -2, 2, 2, roadTerrain);

            for (var dir = 0; dir < 4; dir++) {
                GenMainRoad(s.Rotate(dir).Move(0, 3), roadTerrain, divTerrain, sidewalkTerrain);
            }
        }

        void GenMainRoad(Stencil s, TerrainDef roadTerrain, TerrainDef divTerrain, TerrainDef sidewalkTerrain) {
            s.Bound(-3, 0, 3, s.MaxZ)
                .BorderTerrain(sidewalkTerrain, (m, p) => IsValidSidewalkTerrain(p.GetTerrain(m)));
            s.Bound(-2, 0, 2, s.MaxZ)
                .ClearThingsInBounds()
                .FillTerrain(roadTerrain);
            var s1 = s;
            s.Bound(-4, 0, 4, s.MaxZ)
                .BorderTerrain(TerrainUtility.Bridge, (m, p) => p.GetTerrain(m).IsWater);
            s.FillTerrain(0, 0, 0, s.MaxZ, divTerrain);
            s = s.Move(0, roadSpacing.RandomInRange);
            while (s.Expand(-2).IsInBounds()) {
                if (s.Chance(roadChance)) {
                    GenSideRoad(s.Left().Move(0, 3), roadTerrain, sidewalkTerrain);
                }

                if (s.Chance(roadChance)) {
                    GenSideRoad(s.Right().Move(0, 3), roadTerrain, sidewalkTerrain);
                }

                s = s.Move(0, roadSpacing.RandomInRange);
            }
        }

        void GenSideRoad(Stencil s, TerrainDef roadTerrain, TerrainDef sidewalkTerrain) {
            s.Bound(-2, 0, 2, s.MaxZ)
                .BorderTerrain(sidewalkTerrain, (m, p) => IsValidSidewalkTerrain(p.GetTerrain(m)));
            s.Bound(-1, 0, 1, s.MaxZ)
                .ClearThingsInBounds()
                .FillTerrain(roadTerrain);
            s.Bound(-3, 0, 3, s.MaxZ)
                .BorderTerrain(TerrainUtility.Bridge, (m, p) => p.GetTerrain(m).IsWater);
        }

        bool IsValidSidewalkTerrain(TerrainDef terrain) {
            return TerrainUtility.IsNatural(terrain) || terrain == TerrainUtility.Bridge || terrain.IsWater;
        }
    }
}