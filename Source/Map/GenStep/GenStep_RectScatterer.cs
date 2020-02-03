using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public abstract class GenStep_RectScatterer : GenStep_Scatterer {
        public IntRange areaConstraints = new IntRange(100, 1000);
        public float maxRatio = 3;

        [Unsaved] IntVec3? cachedPos;

        [Unsaved] Stencil? cachedStencil;

        public int MaxAttempts => 100;

        public override int SeedPart => GetType().Name.GetHashCode();

        protected override void ScatterAt(IntVec3 pos, Map map, int count) {
            var s = GetStencil(map, pos);
            if (s.HasValue) {
                GenerateRect(s.Value.Center().RotateRand());
            }
        }

        protected override bool TryFindScatterCell(Map map, out IntVec3 result) {
            var attempts = MaxAttempts;
            do {
                if (base.TryFindScatterCell(map, out result)) {
                    if (GetStencil(map, result).HasValue) {
                        return true;
                    }
                }
            } while (--attempts > 0);

            return false;
        }

        Stencil? GetStencil(Map map, IntVec3 pos) {
            if (cachedPos != null && pos == cachedPos) {
                return cachedStencil;
            }

            cachedPos = pos;
            var s = new Stencil(map)
                .MoveTo(pos)
                .ExpandRegion(IsValidTile, areaConstraints.max);

            var ratio = (float) s.Width / s.Height;
            if (s.Area < areaConstraints.min || ratio > maxRatio || 1 / ratio > maxRatio) {
                return cachedStencil = null;
            }

            return cachedStencil = s;
        }

        public abstract void GenerateRect(Stencil s);

        protected virtual bool IsValidTile(Map map, IntVec3 pos) {
            return IsValidTerrain(pos.GetTerrain(map));
        }

        protected virtual bool IsValidTerrain(TerrainDef terrain) {
            return TerrainUtility.IsNaturalExcludingRock(terrain);
        }
    }
}