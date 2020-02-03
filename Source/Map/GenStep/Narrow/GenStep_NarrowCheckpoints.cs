using Cities;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowCheckpoints : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange tierRange = new IntRange(1, 3);
        public IntRange sentryRange = new IntRange(2, 5);

        protected override void ScatterAt(IntVec3 pos, Map map, int count) {
            var tier = tierRange.RandomInRange;

            var stuff = GenCity.RandomWallStuff(map);

            var s = new Stencil(map).MoveTo(pos);
            s.Bound(0, 0, 0, tier - 1).Fill(ThingDefOf.Door, stuff, IsValidTile);

            s.Bound(s.MinX, 0, s.MaxX, tier - 1)
                .Fill(ThingDefOf.Wall, stuff, IsValidTile);

            s.Bound(s.MinX, -3, s.MaxX, -3)
                .Fill(ThingDefOf.Sandbags, mask: IsValidTile);
            s.Bound(s.MinX, 0, s.MaxX, 0)
                .Expand(3).FillTerrain(TerrainDefOf.Concrete, IsValidTile);

            s = s.Bound(s.MinX, -1, s.MaxX, -1);
            
            var sentries = sentryRange.RandomInRange;
            for (var i = 0; i < sentries; i++) {
                var point = s.MoveRand().pos;
                GenCity.SpawnInhabitant(point, map, new LordJob_DefendBase(map.ParentFaction, point));
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNatural(pos.GetTerrain(map));
        }
    }
}