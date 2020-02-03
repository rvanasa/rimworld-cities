using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

    [HarmonyPatch(typeof(GenStep_FindPlayerStartSpot))]
    [HarmonyPatch(nameof(GenStep_FindPlayerStartSpot.Generate))]
    internal static class GenStep_FindPlayerStartSpot_Generate {
        static void Postfix(ref Map map) {
            if (map.Parent is Citadel) {
                const int border = 10;
                MapGenerator.PlayerStartSpot = new IntVec3(
                    Rand.RangeInclusive(border, map.Size.x - border),
                    0,
                    Rand.RangeInclusive(border, map.Size.x - border));
            }
        }
    }

    [HarmonyPatch(typeof(CaravanEnterMapUtility))]
    [HarmonyPatch("GetEnterCell")]
    internal static class CaravanEnterMapUtility_GetEnterCell {
        static void Postfix(ref Map map, ref IntVec3 __result) {
            if (map.Parent is Citadel) {
                __result.z = 0;
            }
        }
    }
}