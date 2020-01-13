using RimWorld.Planet;
using Verse;

namespace Cities
{
	public static class QuestUtility {
		public static bool Reachable(WorldObject from, WorldObject to, int minDist = int.MaxValue) {
			if(from == null || to == null) {
				return false;
			}
			return Reachable(from.Tile, to.Tile, minDist);
		}

		public static bool Reachable(int from, int to, int minDist = int.MaxValue) {
			return Find.WorldGrid.ApproxDistanceInTiles(from, to) < minDist && Find.WorldReachability.CanReach(from, to);
		}

		public static IntVec3 FindDropSpot(Map map, int dropArea = 10) {
			var attempts = 20;
			Stencil s;
			do {
				s = new Stencil(map).MoveRand()
					.ExpandRegion(p => p.GetFirstThing<Thing>(map) == null, dropArea)
					.Center();
				if(s.Area >= dropArea) {
					break;
				}
			}
			while(--attempts >= 0);
			return s.pos;
		}
	}
}