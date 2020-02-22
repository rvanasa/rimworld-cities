using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Cities {

    [HarmonyPatch(typeof(MapGenerator))]
    [HarmonyPatch(nameof(MapGenerator.GenerateMap))]
    internal static class MapGenerator_GenerateMap {
        static void Prefix(ref IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs, ref System.Action<Map> extraInitBeforeContentGen) {
            // if (Find.GameInitData.QuickStarted && Config_QuickStart.MapGenerator != null) {
            //     mapGenerator = DefDatabase<MapGeneratorDef>.GetNamed(Config_QuickStart.MapGenerator);
            // }

            if (parent is City city) {
                mapSize = city.ChooseMapSize(mapSize);
                var prevInitAction = extraInitBeforeContentGen;
                extraInitBeforeContentGen = map => {
                    city.PreMapGenerate(map);
                    prevInitAction?.Invoke(map);
                };
            }
        }
    }

    [HarmonyPatch(typeof(SettlementDefeatUtility))]
    [HarmonyPatch(nameof(SettlementDefeatUtility.CheckDefeated))]
    internal static class SettlementDefeatUtility_CheckDefeated {
        static bool Prefix(Settlement factionBase) {
            if (factionBase is City city && !factionBase.Faction.HostileTo(Faction.OfPlayer)) {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CaravanArrivalAction_OfferGifts))]
    [HarmonyPatch(nameof(CaravanArrivalAction_OfferGifts.CanOfferGiftsTo))]
    internal static class CaravanArrivalAction_OfferGifts_CanOfferGiftsTo {
        static bool Prefix(ref FloatMenuAcceptanceReport __result, Caravan caravan, Settlement settlement) {
            if (settlement is City city && city.Abandoned) {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(QuestNode_GetNearbySettlement))]
    [HarmonyPatch("RandomNearbyTradeableSettlement")]
    internal static class QuestNode_GetNearbySettlement_RandomNearbyTradeableSettlement {
        static void Postfix(ref MapParent __result, int originTile, Slate slate) {
            if (__result is City city && (city.Abandoned || city.Faction.HostileTo(Faction.OfPlayer))) {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(ThingOwner<Thing>))]
    [HarmonyPatch(nameof(ThingOwner<Thing>.TryAdd))]
    [HarmonyPatch(new[] {typeof(Thing), typeof(bool)})]
    internal static class ThingOwner_TryAdd {
        static void Prefix(ref ThingOwner<Thing> __instance, Thing item) {
            var pawn =
                (__instance.Owner as Pawn_InventoryTracker)?.pawn ??
                (__instance.Owner as Pawn_ApparelTracker)?.pawn ??
                (__instance.Owner as Pawn_EquipmentTracker)?.pawn;

            if (pawn != null && pawn.IsColonist && item.IsOwnedByCity(pawn.Map)) {
                if (pawn.Map.Parent is City city && !city.Abandoned && city.Faction != pawn.Faction) {
                    city.Faction.TryAffectGoodwillWith(pawn.Faction,
                        -Mathf.RoundToInt(Mathf.Sqrt(item.MarketValue)) - 2);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CompForbiddable))]
    [HarmonyPatch(nameof(CompForbiddable.CompGetGizmosExtra))]
    internal static class CompForbiddable_CompGetGizmosExtra {
        static bool Prefix(ref CompForbiddable __instance, ref IEnumerable<Gizmo> __result) {
            if (__instance.Forbidden && __instance.parent.IsOwnedByCity()) {
                __result = Enumerable.Empty<Gizmo>();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing))]
    [HarmonyPatch(nameof(Thing.SplitOff))]
    internal static class Thing_SplitOff {
        static void Postfix(ref Thing __instance, ref Thing __result) {
            if (__instance.IsOwnedByCity()) {
                __result?.SetOwnedByCity(true, __instance.Map);
            }
        }
    }

    [HarmonyPatch(typeof(FogGrid))]
    [HarmonyPatch("FloodUnfogAdjacent")]
    internal static class FogGrid_FloodUnfogAdjacent {
        static bool Prefix(ref FogGrid __instance, ref Map ___map, IntVec3 c) {
            if (!(___map.Parent is City)) {
                return true;
            }

            __instance.Unfog(c);
            for (var i = 0; i < 4; i++) {
                var intVec = c + GenAdj.CardinalDirections[i];
                if (intVec.InBounds(___map) && intVec.Fogged(___map)) {
                    var edifice = intVec.GetEdifice(___map);
                    if (edifice == null || !edifice.def.MakeFog) {
                        FloodFillerFog.FloodUnfog(intVec, ___map);
                    }
                    else {
                        __instance.Unfog(intVec);
                    }
                }
            }

            for (var j = 0; j < 8; j++) {
                var c2 = c + GenAdj.AdjacentCells[j];
                if (c2.InBounds(___map)) {
                    var edifice2 = c2.GetEdifice(___map);
                    if (edifice2 != null && edifice2.def.MakeFog) {
                        __instance.Unfog(c2);
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Settlement_TraderTracker))]
    [HarmonyPatch(nameof(Settlement_TraderTracker.TraderKind), MethodType.Getter)]
    internal static class Settlement_TraderTracker_TraderKind {
        static bool Prefix(ref Settlement_TraderTracker __instance, ref TraderKindDef __result) {
            if (__instance.ParentHolder is City city) {
                __result = city.WorldTraderKind;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.DropAndForbidEverything))]
    internal static class Pawn_DropAndForbidEverything {
        static bool Prefix(ref Pawn __instance) {
            if (__instance.Map.Parent is City && __instance.TraderKind != null) {
                __instance.inventory.DestroyAll();
            }
            return true;
        }
    }

    // bug hotfix for 1.1
    [HarmonyPatch(typeof(Room))]
    [HarmonyPatch(nameof(Room.Notify_ContainedThingSpawnedOrDespawned))]
    internal static class Room_Notify_ContainedThingSpawnedOrDespawned {
        static bool Prefix(Room __instance, ref Thing th) {
            try {
                if (th.def.category == ThingCategory.Mote || th.def.category == ThingCategory.Projectile || (th.def.category == ThingCategory.Ethereal || th.def.category == ThingCategory.Pawn))
                    return false;
                if (__instance.IsDoorway) {
                    foreach (var t in __instance.Regions[0].links) {
                        Region otherRegion = t.GetOtherRegion(__instance.Regions[0]);
                        if (!otherRegion.IsDoorway)
                            otherRegion.Room.Notify_ContainedThingSpawnedOrDespawned(th);
                    }
                }
                typeof(Room).GetField("statsAndRoleDirty", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField)
                    .SetValue(__instance, true);
            }
            catch (System.Exception e) {
                Debug.LogWarning(e);
            }

            return false;
        }
    }
}