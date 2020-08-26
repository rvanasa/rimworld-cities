using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace Cities {
    public class GenStep_Post : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void Generate(Map map, GenStepParams parms) {
            foreach (var thing in map.spawnedThings) {
                if (thing.def == ThingDefOf.Silver) {
                    thing.Destroy();
                }
                else if (!thing.def.canBeUsedUnderRoof) {
                    new Stencil(map)
                        .BoundTo(GenAdj.OccupiedRect(thing.Position, thing.Rotation, thing.def.size))
                        .ClearRoof();
                }
            }
        }
    }
}