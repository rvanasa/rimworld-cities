using System;
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

    [HarmonyPatch(typeof(HistoryAutoRecorder))]
    [HarmonyPatch(nameof(HistoryAutoRecorder.Tick))]
    internal static class HistoryAutoRecorder_Tick {
        static bool Prefix(ref HistoryAutoRecorder __instance) {
            try {
                if (Find.TickManager.TicksGame % __instance.def.recordTicksFrequency == 0 || !__instance.records.Any()) {
                    if (Find.AnyPlayerHomeMap == null && !__instance.records.Empty()) {
                        __instance.records.Add(__instance.records.Last());
                        return false;
                    }
                    float item = __instance.def.Worker.PullRecord();
                    __instance.records.Add(item);
                }
            } catch (Exception e) {
                // Warning instead of error
                Log.Warning(e.ToString());
            }
            return false;
        }
    }

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

            // if (Config_Cities.Instance.customMapSizes) {
            //     mapSize.x = Config_Cities.Instance.customMapX;
            //     mapSize.z = Config_Cities.Instance.customMapZ;
            // }
        }
    }

    [HarmonyPatch(typeof(SettlementDefeatUtility))]
    [HarmonyPatch(nameof(SettlementDefeatUtility.CheckDefeated))]
    internal static class SettlementDefeatUtility_CheckDefeated {
        static bool Prefix(Settlement factionBase) {
            if (factionBase is City && !factionBase.Faction.HostileTo(Faction.OfPlayer)) {
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
        static void Postfix(ref MapParent __result, PlanetTile originTile, Slate slate) {
            if (__result is City city && (city.Abandoned || city.Faction.HostileTo(Faction.OfPlayer))) {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(ThingOwner<Thing>))]
    [HarmonyPatch(nameof(ThingOwner<Thing>.TryAdd))]
    [HarmonyPatch(new[] { typeof(Thing), typeof(bool) })]
    internal static class ThingOwner_TryAdd {
        static void Prefix(ref ThingOwner<Thing> __instance, Thing item) {
            if (!Config_Cities.Instance.enableLooting) {
                var pawn =
                    (__instance.Owner as Pawn_InventoryTracker)?.pawn ??
                    (__instance.Owner as Pawn_ApparelTracker)?.pawn ??
                    (__instance.Owner as Pawn_EquipmentTracker)?.pawn;

                if (pawn != null && pawn.IsColonistPlayerControlled && item.IsOwnedByCity(pawn.Map)) {
                    if (pawn.Map.Parent is City city && !city.Abandoned && city.Faction != pawn.Faction) {
                        city.Faction.TryAffectGoodwillWith(pawn.Faction,
                            -Mathf.RoundToInt(Mathf.Sqrt(item.stackCount * item.MarketValue) * Rand.Range(1.5F, 2)) - 2);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(TransporterUtility))]
    [HarmonyPatch(nameof(TransporterUtility.AllSendableItems))]
    internal static class TransporterUtility_AllSendableItems {
        static void Postfix(ref IEnumerable<Thing> __result, Map map) {
            if (!Config_Cities.Instance.enableLooting && map.Parent is City) {
                __result = __result.Where(t => !(t as ThingWithComps)?.GetComp<CompForbiddable>()?.Forbidden ?? !t.IsOwnedByCity(map));

                // __result = __result.ToList();
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

    [HarmonyPatch(typeof(SettlementUtility))]
    [HarmonyPatch(nameof(SettlementUtility.Attack))]
    internal static class SettlementUtility_Attack {
        static bool Prefix(Caravan caravan, Settlement settlement) {
            if (settlement is Citadel && !settlement.HasMap) {
                var method = typeof(SettlementUtility).GetMethod("AttackNow", BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null) {
                    LongEventHandler.QueueLongEvent(() => method.Invoke(null, new object[] { caravan, settlement }),
                        "GeneratingCitadel", false, null);
                    return false;
                }
                Log.Error("SettlementUtility.AttackNow(..) no longer exists");
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FogGrid))]
    [HarmonyPatch("NotifyAreaRevealed")]
    internal static class FogGrid_NotifyAreaRevealed {
        static bool Prefix(ref Map ___map) {
            return !(___map.Parent is City);
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

    [HarmonyPatch(typeof(Pawn_TraderTracker))]
    [HarmonyPatch(nameof(Pawn_TraderTracker.ColonyThingsWillingToBuy))]
    internal static class Pawn_TraderTracker_ColonyThingsWillingToBuy {
        static void Postfix(Pawn playerNegotiator, ref IEnumerable<Thing> __result) {
            if (playerNegotiator.Map?.Parent is City) {
                __result = __result.Concat(playerNegotiator.Map.mapPawns.SpawnedPawnsInFaction(playerNegotiator.Faction)
                    .SelectMany(p => p.inventory.innerContainer.InnerListForReading));
            }
        }
    }

    [HarmonyPatch(typeof(TradeDeal))]
    [HarmonyPatch("InSellablePosition")]
    internal static class TradeDeal_InSellablePosition {
        static bool Prefix(Thing t, ref bool __result) {
            if (Find.CurrentMap?.Parent is City && !t.Spawned) {
                __result = true;
                return false;
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
            } catch (System.Exception e) {
                Log.Warning(e.ToString());
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
                dest = (LocalTargetInfo)GenPath.ResolvePathMode(___pawn, dest.ToTargetInfo(___pawn.Map), ref peMode);
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
    [HarmonyPatch(nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    internal static class PawnGenerator_GeneratePawn {
        static void Postfix(ref Pawn __result) {
            if (__result.RaceProps.IsMechanoid && Rand.Chance(.005F)) {
                var names = ((GenStep_NarrowKeep)DefDatabase<GenStepDef>.GetNamed("Narrow_Keep").genStep).mechanoidNames;
                __result.Name = new NameSingle(names[Rand.Range(0, names.Count)]);
            }
        }
    }


}