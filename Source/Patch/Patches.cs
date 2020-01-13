using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
	[StaticConstructorOnStartup]
	public static class Patches_RimCities {
		static Patches_RimCities() {
			var harmony = HarmonyInstance.Create("cabbage.rimcities");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(MapGenerator))]
	[HarmonyPatch(nameof(MapGenerator.GenerateMap))]
	static class MapGenerator_GenerateMap {
		static void Prefix(ref IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs, ref System.Action<Map> extraInitBeforeContentGen) {
			if(parent is City city) {
				var shouldLimitMapSize = !city.Abandoned;
				if(Config_Cities.Instance.limitCitySize) {
					mapSize.x = Mathf.Min(mapSize.x, 200);
					mapSize.z = Mathf.Min(mapSize.z, 200);
				}
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
	static class SettlementDefeatUtility_CheckDefeated {
		static bool Prefix(Settlement factionBase) {
			if(factionBase is City city && !factionBase.Faction.HostileTo(Faction.OfPlayer)) {
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CaravanArrivalAction_OfferGifts))]
	[HarmonyPatch(nameof(CaravanArrivalAction_OfferGifts.CanOfferGiftsTo))]
	static class CaravanArrivalAction_OfferGifts_CanOfferGiftsTo {
		static bool Prefix(ref FloatMenuAcceptanceReport __result, Caravan caravan, SettlementBase settlement) {
			if(settlement is City city && city.Abandoned) {
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(IncidentWorker_QuestTradeRequest))]
	[HarmonyPatch(nameof(IncidentWorker_QuestTradeRequest.RandomNearbyTradeableSettlement))]
	static class IncidentWorker_QuestTradeRequest_RandomNearbyTradeableSettlement {
		static void Postfix(ref SettlementBase __result, int originTile) {
			if(__result is City city && (city.Abandoned || city.Faction.HostileTo(Faction.OfPlayer))) {
				__result = null;
			}
		}
	}

	[HarmonyPatch(typeof(ThingOwner<Thing>))]
	[HarmonyPatch(nameof(ThingOwner<Thing>.TryAdd))]
	[HarmonyPatch(new[] { typeof(Thing), typeof(bool) })]
	static class ThingOwner_TryAdd {
		static void Prefix(ref ThingOwner<Thing> __instance, Thing item) {
			var pawn =
				(__instance.Owner as Pawn_InventoryTracker)?.pawn ??
				(__instance.Owner as Pawn_ApparelTracker)?.pawn ??
				(__instance.Owner as Pawn_EquipmentTracker)?.pawn;

			if(pawn != null && pawn.IsColonist && item.IsOwnedByCity(pawn.Map)) {
				if(pawn.Map.Parent is City city && !city.Abandoned && city.Faction != pawn.Faction) {
					city.Faction.TryAffectGoodwillWith(pawn.Faction, -Mathf.RoundToInt(Mathf.Sqrt(item.MarketValue)) - 2);
				}
			}
		}
	}

	[HarmonyPatch(typeof(CompForbiddable))]
	[HarmonyPatch(nameof(CompForbiddable.CompGetGizmosExtra))]
	static class CompForbiddable_CompGetGizmosExtra {
		static bool Prefix(ref CompForbiddable __instance, ref IEnumerable<Gizmo> __result) {
			if(__instance.Forbidden && __instance.parent.IsOwnedByCity()) {
				__result = Enumerable.Empty<Gizmo>();
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.SplitOff))]
	static class Thing_SplitOff {
		static void Postfix(ref Thing __instance, ref Thing __result) {
			if(__instance.IsOwnedByCity()) {
				__result?.SetOwnedByCity(true, __instance.Map);
			}
		}
	}

	[HarmonyPatch(typeof(FogGrid))]
	[HarmonyPatch("FloodUnfogAdjacent")]
	static class FogGrid_FloodUnfogAdjacent {
		static bool Prefix(ref FogGrid __instance, ref Map ___map, IntVec3 c) {
			if(!(___map.Parent is City)) {
				return true;
			}
			__instance.Unfog(c);
			FloodUnfogResult floodUnfogResult = default(FloodUnfogResult);
			for(int i = 0; i < 4; i++) {
				IntVec3 intVec = c + GenAdj.CardinalDirections[i];
				if(intVec.InBounds(___map) && intVec.Fogged(___map)) {
					Building edifice = intVec.GetEdifice(___map);
					if(edifice == null || !edifice.def.MakeFog) {
						floodUnfogResult = FloodFillerFog.FloodUnfog(intVec, ___map);
					}
					else {
						__instance.Unfog(intVec);
					}
				}
			}
			for(int j = 0; j < 8; j++) {
				IntVec3 c2 = c + GenAdj.AdjacentCells[j];
				if(c2.InBounds(___map)) {
					Building edifice2 = c2.GetEdifice(___map);
					if(edifice2 != null && edifice2.def.MakeFog) {
						__instance.Unfog(c2);
					}
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(WorldObject))]
	[HarmonyPatch(nameof(WorldObject.GetGizmos))]
	static class WorldObject_GetGizmos {
		static void Postfix(ref WorldObject __instance, ref IEnumerable<Gizmo> __result) {
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				__result = __result.Concat(quest.GetGizmos(__instance));
				if(quest.Targets.targets.Contains(__instance)) {
					if(quest.def.Worker is IncidentWorker_Quest worker) {
						var gizmo = new Command_Action {
							icon = QuestIcons.InfoIcon,
							defaultLabel = quest.Name,
							defaultDesc = worker.LetterText,
							action = () => worker.MakeLetter().OpenLetter(),
						};
						__result = __result.Add(gizmo);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.GetGizmos))]
	static class Thing_GetGizmos {
		static void Postfix(ref Thing __instance, ref IEnumerable<Gizmo> __result) {
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				__result = __result.Concat(quest.GetGizmos(__instance));
			}
		}
	}

	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.GetFloatMenuOptions))]
	static class Thing_GetFloatMenuOptions {
		static void Postfix(ref Thing __instance, ref IEnumerable<FloatMenuOption> __result, Pawn selPawn) {
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				__result = __result.Concat(quest.GetFloatMenuOptions(__instance, selPawn));
			}
		}
	}
}