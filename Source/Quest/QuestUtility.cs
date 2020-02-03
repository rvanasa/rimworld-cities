using System.Text;
using RimWorld.Planet;
using Verse;

namespace Cities {
    public static class QuestUtility {
        public static bool Reachable(WorldObject from, WorldObject to, int minDist = -1) {
            if (from == null || to == null) {
                return false;
            }

            return Reachable(from.Tile, to.Tile, minDist);
        }

        public static bool Reachable(int from, int to, int minDist = -1) {
            if (minDist >= 0 && Find.WorldGrid.ApproxDistanceInTiles(from, to) > minDist) {
                return false;
            }

            return Find.WorldReachability.CanReach(from, to);
        }

        public static IntVec3 FindDropSpot(Map map, int dropArea = 10) {
            var attempts = 20;
            Stencil s;
            do {
                s = new Stencil(map).MoveRand()
                    .ExpandRegion((m, p) => p.GetFirstThing<Thing>(m) == null, dropArea)
                    .Center();
                if (s.Area >= dropArea) {
                    break;
                }
            } while (--attempts >= 0);

            return s.pos;
        }
    }
}