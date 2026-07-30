using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Cities {
    public class GenStep_ClearChunks : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void Generate(Map map, GenStepParams parms) {
            List<Thing> thingsToDestroy = map.spawnedThings.Where(thing => !map.terrainGrid.TerrainAt(thing.Position).natural
                && (thing.HasThingCategory(ThingCategoryDefOf.StoneChunks) || thing is Filth)
                && thing.def.destroyable
                && !thing.Destroyed).ToList();

            foreach (Thing thing in thingsToDestroy) {
                thing.Destroy();
            }
        }
    }
}
