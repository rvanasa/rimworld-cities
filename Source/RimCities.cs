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
			listing.CheckboxLabeled("Limit city map size (reduces lag)", ref settings.limitCitySize);
			listing.Gap();
			listing.Label("Abandoned city chance: [" + GenMath.RoundTo(settings.abandonedChance, 0.01F) + "]");
			settings.abandonedChance = listing.Slider(settings.abandonedChance, 0, 1);
			listing.Gap();
			listing.Label("City spawn frequency:");
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
		public float abandonedChance;
		public IntRange citiesPer100kTiles;

		public ModSettings_Cities() {
			Reset();
		}

		public override void ExposeData() {
			// TODO: DRY defaults
			Scribe_Values.Look(ref limitCitySize, "limitCitySize", true);
			Scribe_Values.Look(ref abandonedChance, "abandonedChance", 0.4F);
			Scribe_Values.Look(ref citiesPer100kTiles, "citiesPer100kTiles", new IntRange(10, 15));
			base.ExposeData();
		}

		public void Reset() {
			limitCitySize = true;
			abandonedChance = 0.4F;
			citiesPer100kTiles = new IntRange(10, 15);
		}
	}

	public class City : Settlement {
		public Faction inhabitantFaction;

		public bool Abandoned => inhabitantFaction == null;

		public override MapGeneratorDef MapGeneratorDef => DefDatabase<MapGeneratorDef>.GetNamed(
				inhabitantFaction != null ? "City_Faction" : "City_Abandoned");

		public override Texture2D ExpandingIcon => def.ExpandingIconTexture;

		public override Color ExpandingIconColor => inhabitantFaction != null ? base.ExpandingIconColor : Color.grey;

		public override bool Visitable => base.Visitable || Abandoned;

		public override bool Attackable => base.Attackable && !Abandoned;

		public override void PostMake() {
			base.PostMake();
			if(Abandoned) {
				trader = null;
			}
		}

		public override void PostMapGenerate() {
			base.PostMapGenerate();
			if(Abandoned) {
				SetFaction(Faction.OfPlayer);
			}
		}

		public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan) {
			if(Visitable) {
				yield return new Command_Action {
					icon = SettleUtility.SettleCommandTex,
					defaultLabel = "Enter City",
					defaultDesc = "Enter the selected city.",
					action = () => {
						LongEventHandler.QueueLongEvent(() => {
							var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(Tile, null);
							CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
						}, "GeneratingMapForNewEncounter", false, null);
					},
				};
			}
			foreach(var gizmo in base.GetCaravanGizmos(caravan)) {
				yield return gizmo;
			}
		}

		public override void ExposeData() {
			Scribe_References.Look(ref inhabitantFaction, "inhabitantFaction");
			base.ExposeData();
		}

		public Faction FindFaction(System.Predicate<Faction> filter = null) {
			return (from x in Find.World.factionManager.AllFactionsListForReading
					where !x.def.isPlayer && !x.def.hidden && !x.def.techLevel.IsNeolithicOrWorse() && (filter == null || filter(x))
					select x).RandomElementByWeightWithFallback(f => f.def.settlementGenerationWeight);
		}

		public virtual void PreMapGenerate(Map map) {
			SetFaction(inhabitantFaction ?? FindFaction(f => f.HostileTo(Faction.OfPlayer)));
		}
	}

	public class MapComponent_City : MapComponent {
		const int RaidTimeCycle = 50_000;
		const int RaidPointIncrease = 500;

		public HashSet<Thing> cityOwnedThings = new HashSet<Thing>();

		public City City => (City)map.Parent;

		public MapComponent_City(Map map) : base(map) {
		}

		public override void ExposeData() {
			Scribe_Collections.Look(ref cityOwnedThings, "cityOwnedThings", LookMode.Reference);
			base.ExposeData();
		}

		public override void MapComponentTick() {
			if((Find.TickManager.TicksGame + map.Parent.ID) % RaidTimeCycle == 0) {
				if(map.Parent is City city && !city.Abandoned && city.Faction.HostileTo(Faction.OfPlayer)) {
					var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
					var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
					parms.faction = city.Faction;
					parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
					parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetRandom();
					parms.raidArrivalModeForQuickMilitaryAid = true;
					parms.points += RaidPointIncrease;
					IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
				}
			}
		}
	}

	public class WorldGenStep_Cities : WorldGenStep {
		public override int SeedPart => GetType().Name.GetHashCode();

		public override void GenerateFresh(string seed) {
			var settings = LoadedModManager.GetMod<Mod_Cities>().GetSettings<ModSettings_Cities>();
			var abandonedChance = settings.abandonedChance;
			var citiesPer100kTiles = settings.citiesPer100kTiles;
			//var citiesPer100kTiles = new IntRange(400, 400);/////
			int cityCount = GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100000F * citiesPer100kTiles.RandomInRange);
			for(int i = 0; i < cityCount; i++) {
				var abandoned = i == 0 || Rand.Chance(abandonedChance);
				var city = (City)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed(abandoned ? "City_Abandoned" : "City_Faction"));
				city.Tile = TileFinder.RandomSettlementTileFor(city.Faction);
				city.SetFaction(city.FindFaction());
				if(!abandoned) {
					city.inhabitantFaction = city.Faction;
				}
				city.Name = SettlementNameGenerator.GenerateSettlementName(city);
				Find.WorldObjects.Add(city);
			}
		}

		public override void GenerateFromScribe(string seed) {
			if(!Find.WorldObjects.AllWorldObjects.Any(obj => obj is City)) {
				Log.Warning("No cities found; regenerating");
				GenerateFresh(seed);
			}
		}
	}

	public abstract class Decorator {
		public float weight = 1;

		public abstract void Decorate(Stencil s);
	}

	public static class GenCity {

		public static TerrainDef RandomFloor(Map map, bool carpets = false) {
			return BaseGenUtility.RandomBasicFloorDef(map.ParentFaction, carpets);
		}

		public static ThingDef RandomWallStuff(Map map, bool onlyCheap = false) {
			return RandomStuff(ThingDefOf.Wall, map, onlyCheap);
		}

		public static ThingDef RandomStuff(ThingDef thing, Map map, bool onlyCheap = false) {
			if(!thing.MadeFromStuff) {
				return null;
			}
			else if(onlyCheap) {
				return GenStuff.RandomStuffInexpensiveFor(thing, map.ParentFaction);
			}
			else {
				return GenStuff.RandomStuffByCommonalityFor(thing, map.ParentFaction.def.techLevel);
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
				if(map?.Parent is City city && !city.Abandoned) {
					var cityOwnedThings = thing.Map.GetComponent<MapComponent_City>().cityOwnedThings;
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