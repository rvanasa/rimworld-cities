using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
    public static class QueryUtility {
        public static T RandomByDistance<T>(this IEnumerable<T> source, WorldObject start, int max)
            where T : WorldObject {
            
            var collection = source
                .Where(s => QuestUtility.Reachable(start, s))
                .ToArray();
            
            var filtered = collection
                .Where(s => Find.WorldGrid.ApproxDistanceInTiles(start.Tile, s.Tile) <= max)
                .ToArray();
            
            return filtered.RandomElementWithFallback()
                   ?? (collection.Length == 0
                       ? null
                       : collection.MinBy(s => Find.WorldGrid.ApproxDistanceInTiles(start.Tile, s.Tile)));
        }
    }
}