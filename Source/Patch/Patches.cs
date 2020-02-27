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
using Verse.AI;

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
            if (___map.Parent is City) {
                var map = ___map;
                __instance.Unfog(c);
                for (var index = 0; index < 4; ++index) {
                    var intVec3 = c + GenAdj.CardinalDirections[index];
                    if (intVec3.InBounds(map) && intVec3.Fogged(map)) {
                        var edifice = intVec3.GetEdifice(map);
                        if (edifice == null || !edifice.def.MakeFog) {
                            FloodFillerFog.FloodUnfog(intVec3, map);
                        }
                        else {
                            __instance.Unfog(intVec3);
                        }
                    }
                }
                for (var index = 0; index < 8; ++index) {
                    var c1 = c + GenAdj.AdjacentCells[index];
                    if (c1.InBounds(map)) {
                        var edifice = c1.GetEdifice(map);
                        if (edifice != null && edifice.def.MakeFog) {
                            __instance.Unfog(c1);
                        }
                    }
                }
                return false;
            }
            return true;
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
            if (__instance.Map?.Parent is City && __instance.TraderKind != null) {
                __instance.inventory?.DestroyAll();
            }
            return true;
        }
    }

    // bug hotfix for 1.1
    [HarmonyPatch(typeof(Room))]
    [HarmonyPatch(nameof(Room.Notify_ContainedThingSpawnedOrDespawned))]
    internal static class Room_Notify_ContainedThingSpawnedOrDespawned {
        static bool Prefix(Room __instance, ref Thing th, ref bool ___statsAndRoleDirty) {
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
                ___statsAndRoleDirty = true;
            }
            catch (System.Exception e) {
                Debug.LogWarning(e);
            }

            return false;
        }
    }

    // pathing error message hotfix
    [HarmonyPatch(typeof(Pawn_PathFollower))]
    [HarmonyPatch(nameof(Pawn_PathFollower.StartPath))]
    internal static class Pawn_PathFollower_StartPath {
        static bool Prefix(ref Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode, Pawn ___pawn) {
            if (___pawn.Map?.Parent is City) {
                dest = (LocalTargetInfo) GenPath.ResolvePathMode(___pawn, dest.ToTargetInfo(___pawn.Map), ref peMode);
                if (dest.HasThing && dest.ThingDestroyed) {
                    Log.Warning(___pawn + " pathing to destroyed thing " + dest.Thing);
                    // ReSharper disable once PossibleNullReferenceException
                    typeof(Pawn_PathFollower).GetMethod("PatherFailed", BindingFlags.NonPublic).Invoke(__instance, new object[] { });
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch(nameof(PawnGenerator.GeneratePawn), new[] {typeof(PawnGenerationRequest)})]
    internal static class PawnGenerator_GeneratePawn {
        static void Postfix(ref Pawn __result) {
            if (__result.RaceProps.IsMechanoid && Rand.Chance(.005F)) {
                var names = ((GenStep_NarrowKeep) DefDatabase<GenStepDef>.GetNamed("Narrow_Keep").genStep).mechanoidNames;
                __result.Name = new NameSingle(names[Rand.Range(0, names.Count)]);
            }
        }
    }
}