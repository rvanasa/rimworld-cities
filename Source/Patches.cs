using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
	[StaticConstructorOnStartup]
	public class RimCities_Patches {
		public static void Setup() {
			var harmony = HarmonyInstance.Create("cabbage.rimcities");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(SettlementUtility))]
	[HarmonyPatch("AttackNow")]
	[HarmonyPatch(new[] { typeof(Caravan), typeof(SettlementBase) })]
	static class SettlementUtility_AttackNow_Patch {
		static bool Prefix(Caravan caravan, SettlementBase settlement, ref IntVec3 __state) {
			if(settlement is City city) {
				__state = Find.World.info.initialMapSize;
				city.PrepareMapSize(ref Find.World.info.initialMapSize);
			}
			return true;
		}

		static void Postfix(Caravan caravan, SettlementBase settlement, ref IntVec3 __state) {
			if(settlement is City) {
				Find.World.info.initialMapSize = __state;
			}
		}
	}
}