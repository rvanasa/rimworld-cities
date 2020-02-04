using System.Linq;
using Cities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_NarrowMinefields : GenStep_Scatterer {
        public override int SeedPart => GetType().Name.GetHashCode();

        public IntRange spacingRange = new IntRange(3, 6);
        public IntRange jitterRange = new IntRange(0, 2);

        protected override void ScatterAt(IntVec3 pos, Map map, int count) {
            var s = new Stencil(map).MoveTo(pos);

            s = s.Bound(s.MinX, 0, s.MaxX, 0);

            var spacing = spacingRange.RandomInRange;
            for (var i = s.MinX + spacingRange.RandomInRange; i <= s.MaxX; i += spacing) {
                var point = s.MoveRand().pos + IntVec3.North * 3;

                var trap = s.MoveTo(point)
                    .Spawn(jitterRange.RandomInRange * Rand.Sign, jitterRange.RandomInRange * Rand.Sign,
                        DefDatabase<ThingDef>.GetNamed("TrapIED_HighExplosive"));
                trap.SetFactionDirect(map.ParentFaction);
            }
        }

        bool IsValidTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNatural(pos.GetTerrain(map));
        }
    }
}