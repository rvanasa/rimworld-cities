using Cities;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowCheckpoints : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange tierRange = new IntRange(1, 3);
        public IntRange sentryRange = new IntRange(6, 12);
        public IntRange ammoRange = new IntRange(4, 10);

        public float mapDensity = .4F;

        protected override void ScatterAt(IntVec3 pos, Map map, GenStepParams parms, int count) {
            var tier = tierRange.RandomInRange;

            pos.z = (int) (map.Size.z * (1 - Rand.Value * mapDensity)); /////////

            var s = new Stencil(map).MoveTo(pos);
            // if (IsValidWallTile(map, s.pos)) {
            //     s.Spawn(ThingDefOf.Door, wallStuff);
            // }
            s = s.Bound(s.MinX, 0, s.MaxX, 0);

            // s.Fill(ThingDefOf.Wall, wallStuff, IsValidWallTile);

            s.Expand(0, -1, 0, -3)
                .ClearThingsInBounds();

            s.MoveWithBounds(0, -3)
                .Fill(ThingDefOf.Barricade, GenCity.RandomStuff(ThingDefOf.Barricade, map), IsValidTile);

            var sentries = sentryRange.RandomInRange;
            for (var i = 0; i < sentries; i++) {
                var point = s.MoveRand().pos + IntVec3.South;
                GenCity.SpawnInhabitant(point, map, new LordJob_DefendBase(map.ParentFaction, point));
            }

            if (Config_Cities.Instance.enableMortars) {
                var mortars = tierRange.RandomInRange * 1; /**/
                for (var i = 0; i < mortars; i++) {
                    var attempts = 10;
                    do {
                        var point = s.Expand(-2, 0, -2, 0).MoveRand().pos + IntVec3.North * (tier + 2);
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
                            ammo.SetOwnedByCity(true, s.map);
                            GenCity.SpawnInhabitant(ammoPoint, map, new LordJob_ManTurrets());

                            break;
                        }
                    } while (attempts-- > 0);
                }
            }

            s.Bound(s.MinX, 0, s.MaxX, 0)
                .Expand(3).FillTerrain(TerrainDefOf.Concrete, IsValidTile);

            // s.Bound(s.MinX, 0, s.MaxX, -3)
            //     .FillRoof(RoofDefOf.RoofConstructed, IsValidTile);
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return pos.GetFirstThing<Building>(map) == null;
            // return TerrainUtility.IsNatural(pos.GetTerrain(map));
        }

        bool IsValidWallTile(Map map, IntVec3 pos) {
            return pos.GetFirstThing<Building>(map) == null;
            // return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }

        bool IsValidEmplacementTile(Map map, IntVec3 pos) {
            return pos.GetFirstThing<Building>(map) == null;
            // return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }
    }
}