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
	public static class RimCities_Patches {
		static RimCities_Patches() {
			var harmony = HarmonyInstance.Create("cabbage.rimcities");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(MapGenerator))]
	[HarmonyPatch(nameof(MapGenerator.GenerateMap))]
	static class MapGenerator_GenerateMap {
		static void Prefix(ref IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs, ref System.Action<Map> extraInitBeforeContentGen) {
			if(mapGenerator.defName.StartsWith("City_")) {
				if(LoadedModManager.GetMod<Mod_Cities>().GetSettings<ModSettings_Cities>().limitCitySize) {
					mapSize.x = Mathf.Min(mapSize.x, 200);
					mapSize.z = Mathf.Min(mapSize.z, 200);
				}
			}

			var prevInitAction = extraInitBeforeContentGen;
			extraInitBeforeContentGen = map => {
				if(map.Parent is City city) {
					city.PreMapGenerate(map);
				}
				prevInitAction?.Invoke(map);
			};
		}
	}

	[HarmonyPatch(typeof(SettlementDefeatUtility))]
	[HarmonyPatch(nameof(SettlementDefeatUtility.CheckDefeated))]
	static class SettlementDefeatUtility_CheckDefeated {
		static bool Prefix(Settlement factionBase) {
			var playerFaction = Faction.OfPlayer;
			if(factionBase is City city && !factionBase.Faction.HostileTo(playerFaction)) {
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

	[HarmonyPatch(typeof(ThingOwner<Thing>))]
	[HarmonyPatch(nameof(ThingOwner<Thing>.TryAdd))]
	[HarmonyPatch(new[] { typeof(Thing), typeof(bool) })]
	static class ThingOwner_TryAdd {
		static void Prefix(ref ThingOwner __instance, Thing item) {
			var pawn =
				(__instance.Owner as Pawn_InventoryTracker)?.pawn ??
				(__instance.Owner as Pawn_EquipmentTracker)?.pawn;

			if(pawn != null && pawn.IsColonistPlayerControlled) {
				if(pawn.Map.Parent is City city && city.Faction != pawn.Faction && !city.Faction.HostileTo(pawn.Faction)) {
					city.Faction.TryAffectGoodwillWith(pawn.Faction, -Mathf.RoundToInt(Mathf.Sqrt(item.MarketValue)) - 2);
				}
			}
		}
	}
}