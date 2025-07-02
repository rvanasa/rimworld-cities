using System.Linq;
using Cities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowMinefields : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange spacingRange = new IntRange(3, 10);
        public IntRange jitterRange = new IntRange(0, 2);

        protected override void ScatterAt(IntVec3 pos, Map map, GenStepParams parms, int count) {
            // Top half of map
            pos.z += (map.Size.z - pos.z) / 2;

            var s = new Stencil(map).MoveTo(pos);

            s = s.Bound(s.MinX, 0, s.MaxX, 0);

            var spacing = spacingRange.RandomInRange;
            var cityFaction = s.map.GetCityFaction();
            for (var i = s.MinX + spacingRange.RandomInRange; i <= s.MaxX; i += spacing) {
                var point = s.MoveRand().pos + IntVec3.North * 3;
                if (IsValidTile(map, point)) {
                    var trap = s.MoveTo(point)
                        .Spawn(jitterRange.RandomInRange * Rand.Sign, jitterRange.RandomInRange * Rand.Sign,
                            DefDatabase<ThingDef>.GetNamed("TrapIED_HighExplosive"));
                    trap.SetFactionDirect(cityFaction);
                }
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }
    }
}