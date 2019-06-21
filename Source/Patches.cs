using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
	public class RimCities_Patches {
		public static void Setup() {
			var harmony = HarmonyInstance.Create("cabbage.rimcities");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	//public static Map GenerateMap(IntVec3 mapSize, MapParent parent, MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs = null, Action<Map> extraInitBeforeContentGen = null) {

	[HarmonyPatch(typeof(MapGenerator))]
	[HarmonyPatch(nameof(MapGenerator.GenerateMap))]
	static class Patch_MapGenerator_GenerateMap {
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
	static class Patch_SettlementDefeatUtility_CheckDefeated {
		static bool Prefix(Settlement factionBase) {
			var playerFaction = Faction.OfPlayer;
			if(factionBase is City city && !factionBase.Faction.HostileTo(playerFaction)) {
				return false;
			}
			return true;
		}
	}

	/*[HarmonyPatch(typeof(CaravanArrivalAction_Enter))]
	[HarmonyPatch(nameof(CaravanArrivalAction_Enter.CanEnter))]
	static class Patch_CaravanArrivalAction_Enter_CanEnter {
		static bool Prefix(ref FloatMenuAcceptanceReport __result, Caravan caravan, MapParent mapParent) {
			if(mapParent is City city) {
				__result = true;
				return false;
			}
			return true;
		}
	}*/

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
}