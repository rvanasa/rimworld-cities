using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_Fields : GenStep_RectScatterer {
        public float density = 0.9F;
        public AltitudeLayer altitudeLayer = AltitudeLayer.LowPlant;

        public override void GenerateRect(Stencil s) {
            var plant = DefDatabase<ThingDef>.AllDefs
                .Where(t => t.category == ThingCategory.Plant && t.altitudeLayer == altitudeLayer && !t.plant.cavePlant)
                .RandomElement();
            foreach (var pos in s.bounds.Cells) {
                if (s.Chance(density) && pos.GetFirstThing<Thing>(s.map) == null) {
                    GenSpawn.Spawn(plant, pos, s.map);
                }
            }
        }

        protected override bool IsValidTerrain(Map map, TerrainDef terrain) {
            return base.IsValidTerrain(map, terrain) && terrain != TerrainDefOf.Ice;
        }
    }
}