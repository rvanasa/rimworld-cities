using System.Linq;
using Verse;

namespace Cities
{
	public static class TerrainUtility {
		public static TerrainDef Bridge = DefDatabase<TerrainDef>.GetNamed("Bridge");
		public static TerrainAffordanceDef Bridgeable = DefDatabase<TerrainAffordanceDef>.GetNamed("Bridgeable");

		private static readonly TerrainDef[] RockTerrains = (
			from def in DefDatabase<ThingDef>.AllDefs
			where def.building != null && def.building.isNaturalRock && !def.building.isResourceRock
			select def.building.naturalTerrain).ToArray();

		public static bool IsNaturalRock(TerrainDef terrain) {
			return RockTerrains.Contains(terrain);
		}

		public static bool IsNaturalExcludingRock(TerrainDef terrain) {
			return terrain.fertility > 0;
		}

		public static bool IsNatural(TerrainDef terrain) {
			return IsNaturalExcludingRock(terrain) || IsNaturalRock(terrain);
		}
	}
}