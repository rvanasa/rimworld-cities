using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

    internal static class NarrowUtil {
        public static IntVec3 FindStartSpot(Map map) {
            const int border = 10;
            return new IntVec3(Rand.RangeInclusive(border, map.Size.x - border), 0, 3);
        }
    }

    [HarmonyPatch(typeof(GenStep_FindPlayerStartSpot))]
    [HarmonyPatch(nameof(GenStep_FindPlayerStartSpot.Generate))]
    internal static class GenStep_FindPlayerStartSpot_Generate {
        static void Postfix(Map map) {
            if (map.Parent is Citadel) {
                MapGenerator.PlayerStartSpot = NarrowUtil.FindStartSpot(map);
            }
        }
    }

    [HarmonyPatch(typeof(CaravanEnterMapUtility))]
    [HarmonyPatch("GetEnterCell")]
    internal static class CaravanEnterMapUtility_GetEnterCell {
        static bool Prefix(Map map, ref IntVec3 __result) {
            if (map.Parent is Citadel) {
                __result = NarrowUtil.FindStartSpot(map);

                FloodFillerFog.FloodUnfog(__result, map);

                return false;
            }
            return true;
        }
    }

    // [HarmonyPatch(typeof(RegionAndRoomUpdater))]
    // [HarmonyPatch(nameof(RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms))]
    // internal static class RegionAndRoomUpdater_TryRebuildDirtyRegionsAndRooms {
    //     static bool Prefix(RegionAndRoomUpdater __instance, Map ___map) {
    //         if (___map.Parent is Citadel) {
    //             return false;
    //         }
    //         return true;
    //     }
    // }

    // [HarmonyPatch(typeof(RegionGrid))]
    // [HarmonyPatch(nameof(RegionGrid.GetValidRegionAt))]
    // internal static class RegionGrid_GetValidRegionAt {
    //     static bool Prefix(RegionGrid __instance, IntVec3 c, ref Region __result, Map ___map) {
    //         if (___map.Parent is Citadel) {
    //             __result = __instance.GetValidRegionAt_NoRebuild(c);
    //             // if (__result == null) {
    //             //     var region = Region.MakeNewUnfilled();
    //             // }
    //             return false;
    //         }
    //         return true;
    //     }
    // }
}