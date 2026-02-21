using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Cities {
    public class GenStep_Fields : GenStep_RectScatterer {
        public float density = 0.9F;
        public AltitudeLayer altitudeLayer = AltitudeLayer.LowPlant;
        public List<string> excludePlants = new List<string>();

        public override void GenerateRect(Stencil s) {
            var plantDef = DefDatabase<ThingDef>.AllDefs
                .Where(t => t.category == ThingCategory.Plant && t.altitudeLayer == altitudeLayer && !t.plant.cavePlant && !t.plant.isStump && !excludePlants.Contains(t.defName))
                .RandomElement();

            // Growth and age follow the rules from `RimWorld.BaseGen.SymbolResolver_CultivatedPlants.Resolve`.
            float growth = Rand.Range(0.2f, 1f);
            int age = plantDef.plant.LimitedLifespan ? Rand.Range(0, Mathf.Max(plantDef.plant.LifespanTicks - 2500, 0)) : -1;

            foreach (var pos in s.bounds.Cells) {
                if (s.Chance(density) && pos.GetFirstThing<Thing>(s.map) == null && GenSpawn.Spawn(plantDef, pos, s.map) is Plant plant) {
                    plant.Growth = growth;

                    if (age >= 0) {
                        plant.Age = age;
                    }
                }
            }
        }

        protected override bool IsValidTerrain(Map map, TerrainDef terrain) {
            return base.IsValidTerrain(map, terrain) && terrain != TerrainDefOf.Ice;
        }
    }
}