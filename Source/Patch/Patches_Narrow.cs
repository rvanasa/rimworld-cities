using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

    [HarmonyPatch(typeof(GenStep_FindPlayerStartSpot))]
    [HarmonyPatch(nameof(GenStep_FindPlayerStartSpot.Generate))]
    internal static class GenStep_FindPlayerStartSpot_Generate {
        static void Postfix(Map map) {
            if (map.Parent is Citadel) {
                const int border = 10;
                MapGenerator.PlayerStartSpot = GenCity.FindPawnSpot(new IntVec3(
                    Rand.RangeInclusive(border, map.Size.x - border),
                    0,
                    Rand.RangeInclusive(border, map.Size.x - border)), map);
            }
        }
    }

    [HarmonyPatch(typeof(CaravanEnterMapUtility))]
    [HarmonyPatch("GetEnterCell")]
    internal static class CaravanEnterMapUtility_GetEnterCell {
        static void Postfix(Map map, ref IntVec3 __result) {
            if (map.Parent is Citadel) {
                __result.z = 0;
            }
            __result = GenCity.FindPawnSpot(__result, map);
        }
    }
}