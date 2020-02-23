using Cities;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowCheckpoints : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange tierRange = new IntRange(1, 3);
        public IntRange sentryRange = new IntRange(5, 10);
        public IntRange ammoRange = new IntRange(4, 10);

        protected override void ScatterAt(IntVec3 pos, Map map, GenStepParams parms, int count) {
            var tier = tierRange.RandomInRange;

            var stuff = GenCity.RandomWallStuff(map);

            var s = new Stencil(map).MoveTo(pos);
            s.Bound(0, 0, 0, tier - 1).Fill(ThingDefOf.Door, stuff, IsValidTile);
            s = s.Bound(s.MinX, 0, s.MaxX, 0);

            s.Expand(0, 0, 0, tier - 1)
                .Fill(ThingDefOf.Wall, stuff, IsValidWallTile);

            s.Expand(0, -1, 0, -3)
                .ClearThingsInBounds();

            s.MoveWithBounds(0, -3)
                .Fill(ThingDefOf.Barricade, GenCity.RandomStuff(ThingDefOf.Barricade, map), mask: IsValidTile);

            var sentries = sentryRange.RandomInRange;
            for (var i = 0; i < sentries; i++) {
                var point = s.MoveRand().pos + IntVec3.South;
                GenCity.SpawnInhabitant(point, map, new LordJob_DefendBase(map.ParentFaction, point));
            }

            var mortars = tierRange.RandomInRange * 2;
            for (var i = 0; i < mortars; i++) {
                var attempts = 10;
                do {
                    var point = s.MoveRand().pos + IntVec3.North * (tier + 2);
                    var sMortar = s.MoveTo(point).ExpandRegion(IsValidEmplacementTile, 25).Center();
                    if (s.Area >= 9) {
                        var mortar = sMortar.ClearThingsInBounds()
                            .FillTerrain(TerrainDefOf.Concrete)
                            .Back()
                            .Spawn(ThingDefOf.Turret_Mortar, GenCity.RandomStuff(ThingDefOf.Turret_Mortar, map));
                        mortar.SetFactionDirect(map.ParentFaction);

                        var ammoPoint = point + IntVec3.North * 2;
                        var ammo = s.MoveTo(ammoPoint).Spawn(0, 0, ThingDefOf.Shell_HighExplosive);
                        ammo.stackCount = ammoRange.RandomInRange;
                        GenCity.SpawnInhabitant(ammoPoint, map, new LordJob_ManTurrets());

                        break;
                    }
                } while (attempts-- > 0);
            }

            s.Bound(s.MinX, 0, s.MaxX, 0)
                .Expand(3).FillTerrain(TerrainDefOf.Concrete, IsValidTile);

            // s.Bound(s.MinX, 0, s.MaxX, -3)
            //     .FillRoof(RoofDefOf.RoofConstructed, IsValidTile);
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNatural(pos.GetTerrain(map));
        }

        bool IsValidWallTile(Map map, IntVec3 pos) {
            return pos.GetThingList(map).Count == 0;
        }

        bool IsValidEmplacementTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }
    }
}