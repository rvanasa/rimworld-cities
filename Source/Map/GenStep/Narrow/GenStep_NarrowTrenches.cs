using System.Linq;
using Cities;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowTrenches : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange sentryRange = new IntRange(10, 16);

        protected override void ScatterAt(IntVec3 pos, Map map, GenStepParams parms, int count) {
            var s = new Stencil(map).MoveTo(pos);

            s = s.Bound(s.MinX, 0, s.MaxX, 0)
                .ClearThingsInBounds();

            s.Expand(1)
                .FillTerrain(GenCity.RandomFloor(map), IsValidTile);

            var sandbagStuff = GenCity.RandomStuff(ThingDefOf.Sandbags, map);
            s.MoveWithBounds(0, -2)
                .Border(ThingDefOf.Sandbags, sandbagStuff, mask: IsValidTile);
            s.MoveWithBounds(0, 2)
                .Border(ThingDefOf.Sandbags, sandbagStuff, mask: IsValidTile);

            var sentries = sentryRange.RandomInRange;
            for (var i = 0; i < sentries; i++) {
                var point = s.MoveRand().pos + IntVec3.North * 3;
                GenCity.SpawnInhabitant(point, map, new LordJob_DefendBase(map.ParentFaction, point));
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNatural(pos.GetTerrain(map)) && pos.GetFirstThing<Building>(map) == null;
            // return pos.GetFirstThing<Building>(map) == null;
        }
    }
}