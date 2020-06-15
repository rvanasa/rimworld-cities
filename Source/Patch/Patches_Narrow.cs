using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

    internal static class NarrowUtil {
        public static IntVec3 FindStartSpot(Map map) {
            const int border = 10;
            return new IntVec3(Rand.RangeInclusive(border, map.Size.x - border), 0, 4);
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

    [HarmonyPatch(typeof(RCellFinder))]
    [HarmonyPatch(nameof(RCellFinder.TryFindRandomPawnEntryCell))]
    internal static class RCellFinder_TryFindRandomPawnEntryCell {
        static bool Prefix(Map map, ref IntVec3 result, ref bool __result) {
            if (map.Parent is Citadel) {
                result = NarrowUtil.FindStartSpot(map);
                __result = true;

                return false;
            }
            return true;
        }
    }
}