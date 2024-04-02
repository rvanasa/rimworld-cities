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

    [HarmonyPatch(typeof(GenStep_FindPlayerStartSpot), nameof(GenStep_FindPlayerStartSpot.Generate))]
    internal static class GenStep_FindPlayerStartSpot_Generate {
        static void Postfix(Map map) {
            if (map.Parent is Citadel) {
                MapGenerator.PlayerStartSpot = NarrowUtil.FindStartSpot(map);
            }
        }
    }

    [HarmonyPatch(typeof(CaravanEnterMapUtility), "GetEnterCell")]
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

    [HarmonyPatch(typeof(RCellFinder), nameof(RCellFinder.TryFindRandomPawnEntryCell))]
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

    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
    internal static class GenSpawn_Spawn {
        static void Postfix(Thing newThing, Map map) {
            if (newThing is Pawn pawn && map?.Parent is Citadel) {
                if (!pawn.Faction?.IsPlayer ?? false) {
                    foreach (var item in new System.Collections.Generic.List<ThingWithComps>(pawn.equipment.AllEquipmentListForReading)) {
                        if (item.def.weaponTags.Contains("GunSingleUse")) {
                            pawn.equipment.DestroyEquipment(item);
                        }
                    }
                }
            }
        }
    }
}