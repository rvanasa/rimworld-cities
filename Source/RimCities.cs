using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
	public class Mod_Cities : Mod {
		ModSettings_Cities settings;

		public Mod_Cities(ModContentPack content) : base(content) {
			settings = GetSettings<ModSettings_Cities>();
		}

		public override void DoSettingsWindowContents(Rect inRect) {
			var listing = new Listing_Standard();
			listing.Begin(inRect);
			listing.Gap();
			listing.CheckboxLabeled("LimitCitySize".Translate(), ref settings.limitCitySize);
			listing.Gap();
			listing.CheckboxLabeled("EnableCityQuests".Translate(), ref settings.enableQuestSystem);
			listing.Gap();
			listing.CheckboxLabeled("EnableCityEvents".Translate(), ref settings.enableEvents);
			listing.Gap();
			listing.Label("AbandonedCityChance".Translate().Formatted(GenMath.RoundTo(settings.abandonedChance, 0.01F)));
			settings.abandonedChance = listing.Slider(settings.abandonedChance, 0, 1);
			listing.Gap();
			listing.Label("CitiesPer100kTiles".Translate());
			listing.IntRange(ref settings.citiesPer100kTiles, 0, 100);
			listing.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory() {
			return "RimCities";
		}
	}

	public class ModSettings_Cities : ModSettings {
		public bool limitCitySize;
		public bool enableQuestSystem;
		public bool enableEvents;
		public float abandonedChance;
		public IntRange citiesPer100kTiles;
		public IntRange abandonedPer100kTiles;

		public ModSettings_Cities() {
			Reset();
		}

		public override void ExposeData() {
			// TODO: DRY defaults
			base.ExposeData();
			Scribe_Values.Look(ref limitCitySize, "limitCitySize", true);
			Scribe_Values.Look(ref enableQuestSystem, "enableQuestSystem", true);
			Scribe_Values.Look(ref enableEvents, "enableEvents", true);
			Scribe_Values.Look(ref citiesPer100kTiles, "citiesPer100kTiles", new IntRange(10, 15));
			Scribe_Values.Look(ref abandonedPer100kTiles, "abandonedPer100kTiles", new IntRange(5, 10));
		}

		public void Reset() {
			limitCitySize = true;
			enableQuestSystem = true;
			enableEvents = true;
			abandonedChance = 0.3F;
			citiesPer100kTiles = new IntRange(10, 15);
			abandonedPer100kTiles = new IntRange(5, 10);
		}
	}

	public static class GenCity {

		public static Faction RandomCityFaction(System.Predicate<Faction> filter = null) {
			return (from x in Find.World.factionManager.AllFactionsListForReading
					where !x.def.isPlayer && !x.def.hidden && !x.def.techLevel.IsNeolithicOrWorse() && (filter == null || filter(x))
					select x).RandomElementByWeightWithFallback(f => f.def.settlementGenerationWeight);
		}

		public static TerrainDef RandomFloor(Map map, bool carpets = false) {
			return BaseGenUtility.RandomBasicFloorDef(map.ParentFaction, carpets);
		}

		public static ThingDef RandomWallStuff(Map map, bool expensive = false) {
			return RandomStuff(ThingDefOf.Wall, map, expensive);
		}

		public static ThingDef RandomStuff(ThingDef thing, Map map, bool expensive = false) {
			if(!thing.MadeFromStuff) {
				return null;
			}
			else if(expensive) {
				return GenStuff.RandomStuffByCommonalityFor(thing, map.ParentFaction.def.techLevel);
			}
			else {
				return GenStuff.RandomStuffInexpensiveFor(thing, map.ParentFaction);
			}
		}

		public static Pawn SpawnInhabitant(IntVec3 pos, Map map, LordJob job = null, bool friendlyJob = false, bool randomWorkSpot = false) {
			if(job == null || (!friendlyJob && !map.ParentFaction.HostileTo(Faction.OfPlayer))) {
				var workPos = randomWorkSpot ? CellRect.WholeMap(map).RandomCell : pos;
				job = new LordJob_LiveInCity(FindPawnSpot(workPos, map));
			}
			return SpawnInhabitant(pos, map, job != null ? LordMaker.MakeNewLord(map.ParentFaction, job, map) : null);
		}

		public static Pawn SpawnInhabitant(IntVec3 pos, Map map, Lord lord) {
			var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(map.ParentFaction.RandomPawnKind(), map.ParentFaction, PawnGenerationContext.NonPlayer, map.Tile, inhabitant: true));
			if(lord != null) {
				lord.AddPawn(pawn);
			}
			GenSpawn.Spawn(pawn, pos, map);
			return pawn;
		}

		public static IntVec3 FindPawnSpot(IntVec3 pos, Map map) {
			while(!pos.Walkable(map)) {
				pos = pos.RandomAdjacentCell8Way().ClampInsideMap(map);
			}
			return pos;
		}

		public static bool IsOwnedByCity(this Thing thing, Map map = null) {
			return (map ?? thing.Map)?.GetComponent<MapComponent_City>()?.cityOwnedThings.Contains(thing) ?? false;
		}

		public static void SetOwnedByCity(this Thing thing, bool owned, Map map = null) {
			try {
				if(owned) {
					thing.SetForbidden(true, false);
				}

				map = map ?? thing.Map;
				if(map?.Parent is City city && !city.Abandoned && !city.Faction.HostileTo(Faction.OfPlayer)) {
					var cityOwnedThings = map.GetComponent<MapComponent_City>().cityOwnedThings;
					if(owned) {
						cityOwnedThings.Add(thing);
					}
					else {
						cityOwnedThings.Remove(thing);
					}
				}
			}
			catch(System.Exception e) {
				Log.Message("Failed to set city ownership [" + owned + "] on thing: " + thing + " (" + e + ")");
			}
		}
	}

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