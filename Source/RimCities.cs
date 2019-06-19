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

			RimCities_Patches.Setup();
		}

		public override void DoSettingsWindowContents(Rect inRect) {
			var listing = new Listing_Standard();
			listing.Begin(inRect);
			listing.CheckboxLabeled("Limit city map size (disable for larger and probably laggier maps)", ref settings.limitCitySize);
			listing.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory() {
			return "RimCities";
		}
	}

	public class ModSettings_Cities : ModSettings {
		public bool limitCitySize = true;

		public override void ExposeData() {
			Scribe_Values.Look(ref limitCitySize, "limitCitySize");
			base.ExposeData();
		}
	}

	public class City : Settlement {
		public override MapGeneratorDef MapGeneratorDef => DefDatabase<MapGeneratorDef>.GetNamed("City_Faction");

		public override Texture2D ExpandingIcon => def.ExpandingIconTexture;

		public virtual void PrepareMapSize(ref IntVec3 mapSize) {
			if(LoadedModManager.GetMod<Mod_Cities>().GetSettings<ModSettings_Cities>().limitCitySize) {
				mapSize.x = Mathf.Min(mapSize.x, 200);
				mapSize.z = Mathf.Min(mapSize.z, 200);
			}
		}
	}

	public class AbandonedCity : City {
		public override MapGeneratorDef MapGeneratorDef => DefDatabase<MapGeneratorDef>.GetNamed("City_Abandoned");
	}

	public class WorldGenStep_Cities : WorldGenStep {
		public IntRange citiesPer100kTiles = new IntRange(1, 3);
		//public IntRange citiesPer100kTiles = new IntRange(100, 100);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void GenerateFresh(string seed) {
			int cityCount = GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100000F * citiesPer100kTiles.RandomInRange);
			for(int i = 0; i < cityCount; i++) {
				Faction faction = (from x in Find.World.factionManager.AllFactionsListForReading
								   where !x.def.isPlayer && !x.def.hidden && !x.def.techLevel.IsNeolithicOrWorse()
								   select x).RandomElementByWeight(f => f.def.settlementGenerationWeight);
				var city = (City)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("City"));
				city.SetFaction(faction);
				city.Tile = TileFinder.RandomSettlementTileFor(faction);
				city.Name = SettlementNameGenerator.GenerateSettlementName(city);
				Find.WorldObjects.Add(city);
			}
		}
	}

	public class GenStep_Abandoned_Pre : GenStep {
		public FloatRange decay = new FloatRange(0, 1);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			// TODO
		}
	}

	public class GenStep_Abandoned_Post : GenStep {
		public FloatRange decay = new FloatRange(0, 1);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			// TODO
		}
	}

	public class GenStep_ClearAnimals : GenStep {
		public FloatRange decay = new FloatRange(0, 1);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			foreach(var pos in map.AllCells) {
				var things = pos.GetThingList(map);
				for(var i = things.Count - 1; i >= 0; i--) {
					var thing = things[i];
					if(!TerrainUtility.IsNatural(pos.GetTerrain(map))) {
						if(thing is Pawn pawn && pawn.AnimalOrWildMan() && !pawn.IsWildMan()) {
							thing.Destroy();
						}
					}
				}
			}
		}
	}

	public class GenStep_Walls : GenStep_Scatterer {
		public override int SeedPart => GetType().Name.GetHashCode();

		protected override void ScatterAt(IntVec3 pos, Map map, int count) {
			var s = new Stencil(map);
			s = s.BoundTo(CellRect.FromLimits(pos, s.bounds.RandomCell));
			var stuff = GenCity.RandomWallStuff(map, true);
			for(var dir = 0; dir < 4; dir++) {
				var sDir = s.Rotate(dir);
				sDir.Fill(sDir.MinX, sDir.MaxZ, sDir.MaxX, sDir.MaxZ, ThingDefOf.Wall, stuff, p => IsValidTile(map, p));
			}
		}

		private bool IsValidTile(Map map, IntVec3 pos) {
			return TerrainUtility.IsNatural(pos.GetTerrain(map));
		}
	}

	public class GenStep_Streets : GenStep {
		public IntRange roadSpacing = new IntRange(30, 50);
		public float roadChance = 0.5F;
		public List<TerrainDef> roadTerrains = new List<TerrainDef>();
		public List<TerrainDef> divTerrains = new List<TerrainDef>();
		public List<TerrainDef> sidewalkTerrains = new List<TerrainDef>();

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			var roadTerrain = roadTerrains.RandomElement();
			var divTerrain = divTerrains.RandomElement();
			var sidewalkTerrain = sidewalkTerrains.RandomElement();

			var s = new Stencil(map);
			s = s.MoveTo(s.Expand(-4).bounds.RandomCell);

			s.Bound(-2, -2, 2, 2)
				.ClearThingsInBounds()
				.FillTerrain(roadTerrain);

			for(var dir = 0; dir < 4; dir++) {
				GenMainRoad(s.Rotate(dir).Move(0, 3), roadTerrain, divTerrain, sidewalkTerrain);
			}
		}

		private void GenMainRoad(Stencil s, TerrainDef roadTerrain, TerrainDef divTerrain, TerrainDef sidewalkTerrain) {
			s.Bound(-3, 0, 3, s.MaxZ)
				.BorderTerrain(sidewalkTerrain, p => IsValidSidewalkTerrain(p.GetTerrain(s.map)));
			s.Bound(-2, 0, 2, s.MaxZ)
				.ClearThingsInBounds()
				.FillTerrain(roadTerrain);
			s.Bound(-4, 0, 4, s.MaxZ)
				.BorderTerrain(TerrainUtility.Bridge, p => p.GetTerrain(s.map).IsWater);
			s.FillTerrain(0, 0, 0, s.MaxZ, divTerrain);
			s = s.Move(0, roadSpacing.RandomInRange);
			while(s.Expand(-2).IsInBounds()) {
				if(s.Chance(roadChance)) {
					GenSideRoad(s.Left().Move(0, 3), roadTerrain, sidewalkTerrain);
				}
				if(s.Chance(roadChance)) {
					GenSideRoad(s.Right().Move(0, 3), roadTerrain, sidewalkTerrain);
				}
				s = s.Move(0, roadSpacing.RandomInRange);
			}
		}

		private void GenSideRoad(Stencil s, TerrainDef roadTerrain, TerrainDef sidewalkTerrain) {
			s.Bound(-2, 0, 2, s.MaxZ)
				.BorderTerrain(sidewalkTerrain, p => IsValidSidewalkTerrain(p.GetTerrain(s.map)));
			s.Bound(-1, 0, 1, s.MaxZ)
				.ClearThingsInBounds()
				.FillTerrain(roadTerrain);
			s.Bound(-3, 0, 3, s.MaxZ)
				.BorderTerrain(TerrainUtility.Bridge, p => p.GetTerrain(s.map).IsWater);
		}

		private bool IsValidSidewalkTerrain(TerrainDef terrain) {
			return TerrainUtility.IsNatural(terrain) || terrain == TerrainUtility.Bridge || terrain.IsWater;
		}
	}

	public class GenStep_Docks : GenStep_Scatterer {
		public IntRange size = new IntRange(5, 10);

		public override int SeedPart => GetType().Name.GetHashCode();

		protected override void ScatterAt(IntVec3 pos, Map map, int count) {
			var width = size.RandomInRange;
			var height = size.RandomInRange;
			var s = new Stencil(map);
			var x = s.RandInclusive(s.MinX, s.MaxX - width);
			var z = s.RandInclusive(s.MinZ, s.MaxZ - height);
			var sX = s.Bound(x, s.MinZ, x + width, s.MaxZ);
			var sZ = s.Bound(s.MinX, z, s.MaxX, z + height);
			foreach(var cell in sX.bounds.Cells.Concat(sZ.bounds.Cells)) {
				if(cell.GetTerrain(s.map).affordances.Contains(TerrainUtility.Bridgeable)) {
					s.map.terrainGrid.SetTerrain(cell, TerrainUtility.Bridge);
				}
			}
		}
	}

	public class GenStep_Sidewalks : GenStep_Scatterer {
		public List<TerrainDef> sidewalkTerrains = new List<TerrainDef>();

		public override int SeedPart => GetType().Name.GetHashCode();

		protected override void ScatterAt(IntVec3 pos, Map map, int count) {
			var terrain = sidewalkTerrains.RandomElement();

			var s = new Stencil(map).MoveTo(pos).RotateRand();
			if(IsValidTile(map, s.pos)) {
				s.SetTerrain(terrain);
			}

			var s1 = s.Move(0, 1);
			while(s1.Check(s1.pos, p => IsValidTile(map, p))) {
				s1.SetTerrain(terrain);
				s1 = s1.Move(0, 1);
			}
			var s2 = s.Move(0, -1);
			while(s2.Check(s2.pos, p => IsValidTile(map, p))) {
				s2.SetTerrain(terrain);
				s2 = s2.Move(0, -1);
			}
		}

		private bool IsValidTile(Map map, IntVec3 pos) {
			return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
		}
	}

	public abstract class GenStep_RectScatterer : GenStep_Scatterer {
		public IntRange areaConstraints = new IntRange(100, 1000);
		public float maxRatio = 3;

		[Unsaved]
		private IntVec3? cachedPos;

		[Unsaved]
		private Stencil? cachedStencil;

		public int MaxAttempts => 100;

		public override int SeedPart => GetType().Name.GetHashCode();

		protected override void ScatterAt(IntVec3 pos, Map map, int count) {
			var s = GetStencil(map, pos);
			GenerateRect(s.Value.Center().RotateRand());
		}

		protected override bool TryFindScatterCell(Map map, out IntVec3 result) {
			var attempts = MaxAttempts;
			do {
				if(base.TryFindScatterCell(map, out result)) {
					if(GetStencil(map, result).HasValue) {
						return true;
					}
				}
			} while(--attempts > 0);
			return false;
		}

		private Stencil? GetStencil(Map map, IntVec3 pos) {
			if(cachedPos != null && pos == cachedPos) {
				return cachedStencil;
			}

			cachedPos = pos;
			var s = new Stencil(map)
				.MoveTo(pos)
				.ExpandRegion(p => IsValidTile(map, p), areaConstraints.max);

			var ratio = (float)s.Width / s.Height;
			if(s.Area < areaConstraints.min || ratio > maxRatio || 1 / ratio > maxRatio) {
				return cachedStencil = null;
			}
			return cachedStencil = s;
		}

		public abstract void GenerateRect(Stencil s);

		protected virtual bool IsValidTile(Map map, IntVec3 pos) {
			return IsValidTerrain(pos.GetTerrain(map));
		}

		protected virtual bool IsValidTerrain(TerrainDef terrain) {
			return TerrainUtility.IsNaturalExcludingRock(terrain);
		}
	}

	public class GenStep_Fields : GenStep_RectScatterer {
		public float density = 0.9F;
		public AltitudeLayer altitudeLayer = AltitudeLayer.LowPlant;

		public override void GenerateRect(Stencil s) {
			ThingDef plant = DefDatabase<ThingDef>.AllDefs
				.Where(t => t.category == ThingCategory.Plant && t.altitudeLayer == altitudeLayer && !t.plant.cavePlant)
				.RandomElement();
			//s.ClearThingsInBounds();
			foreach(var pos in s.bounds.Cells) {
				if(s.Chance(density) && pos.GetFirstThing<Thing>(s.map) == null) {
					GenSpawn.Spawn(plant, pos, s.map);
				}
			}
		}
	}

	public class GenStep_ThingGroups : GenStep_RectScatterer {
		public List<ThingGroup> options = new List<ThingGroup>();

		public override void GenerateRect(Stencil s) {
			options.RandomElementByWeight(g => g.weight).Generate(s);
		}

		public class ThingGroup {
			public float weight = 1;
			public bool naturalFloor = false;
			public bool roofed = false;
			public ThingDef thingDef;
			public ThingDef thingStuff;
			public int spacing = 1;
			public int spacingX;
			public int spacingZ;

			public virtual void Generate(Stencil s) {
				var spacingX = this.spacingX > 0 ? this.spacingX : spacing;
				var spacingZ = this.spacingZ > 0 ? this.spacingZ : spacing;

				s.ClearThingsInBounds();
				if(!naturalFloor) {
					s.FillTerrain(GenCity.RandomFloor(s.map));
				}
				if(roofed) {
					var wallStuff = GenCity.RandomWallStuff(s.map);
					s.Spawn(s.MinX, s.MinZ, ThingDefOf.Wall, wallStuff);
					s.Spawn(s.MaxX, s.MinZ, ThingDefOf.Wall, wallStuff);
					s.Spawn(s.MinX, s.MaxZ, ThingDefOf.Wall, wallStuff);
					s.Spawn(s.MaxX, s.MaxZ, ThingDefOf.Wall, wallStuff);
					s.FillRoof(RoofDefOf.RoofConstructed);
				}

				var stuff = thingStuff ?? GenCity.RandomStuff(thingDef, s.map);

				var paddingX = spacingX / 2 + 1;
				var paddingZ = spacingZ / 2 + 1;
				for(var x = s.MinX + paddingX; x <= s.MaxX - paddingX; x += spacingX) {
					for(var z = s.MinZ + paddingZ; z <= s.MaxZ - paddingZ; z += spacingZ) {
						s.Spawn(x, z, thingDef, stuff);
					}
				}
			}
		}
	}

	public class GenStep_Emplacements : GenStep_RectScatterer {
		public List<EmplacementOption> options = new List<EmplacementOption>();

		public override void GenerateRect(Stencil s) {
			for(var dir = 0; dir < 4; dir++) {
				var sDir = s.Rotate(dir);
				for(var x = sDir.MinX; x <= sDir.MaxX; x++) {
					if(x <= sDir.MinX / 2 || x >= sDir.MaxX / 2) {
						sDir.Spawn(x, sDir.MaxZ, ThingDefOf.Sandbags);
					}
				}
			}

			options.Where(opt => !opt.selfDestructive || s.map.ParentFaction.def.permanentEnemy)
				.RandomElementByWeight(opt => opt.weight).Generate(s);
		}

		protected override bool IsValidTile(Map map, IntVec3 pos) {
			return base.IsValidTile(map, pos) && pos.GetFirstThing<Thing>(map) == null;
		}

		public class EmplacementOption {
			public float weight = 1;
			public bool roofed = false;
			public bool manned = false;
			public bool selfDestructive = false;
			public ThingDef weaponDef;
			public ThingDef ammoDef;
			public IntRange ammoCount = new IntRange(1, 20);

			public virtual void Generate(Stencil s) {
				var weapon = s.Spawn(weaponDef, weaponDef.MadeFromStuff ? ThingDefOf.Steel : null);
				weapon.SetFactionDirect(s.map.ParentFaction);
				if(ammoDef != null) {
					var ammo = s.RotateRand().Spawn(s.RandInclusive(-1, 1), s.RandInclusive(2, 3), ammoDef);
					ammo.stackCount = ammoCount.RandomInRange;
				}
				if(roofed) {
					var stuff = GenCity.RandomWallStuff(s.map);
					s.Spawn(s.MinX, s.MinZ, ThingDefOf.Wall, stuff);
					s.Spawn(s.MaxX, s.MinZ, ThingDefOf.Wall, stuff);
					s.Spawn(s.MinX, s.MaxZ, ThingDefOf.Wall, stuff);
					s.Spawn(s.MaxX, s.MaxZ, ThingDefOf.Wall, stuff);
					s.FillRoof(RoofDefOf.RoofConstructed);
				}
				if(manned) {
					var pawn = GenCity.SpawnInhabitant(s.pos, s.map, new LordJob_ManTurrets());
				}
			}
		}
	}

	public class GenStep_Buildings : GenStep_RectScatterer {
		public float wallChance = 0.9F;
		public float doorChance = 0.75F;
		public List<TerrainDef> floorOptions = new List<TerrainDef>();
		public List<ThingDef> wallStuffOptions = new List<ThingDef>();
		public List<BuildingDecorator> buildingDecorators = new List<BuildingDecorator>();
		public List<RoomDecorator> roomDecorators = new List<RoomDecorator>();

		public override void GenerateRect(Stencil s) {
			//s.ClearThingsInBounds();
			GenRooms(s, true);

			var stuff = RandomWallStuff(s.map);
			s.Border(ThingDefOf.Wall, stuff);

			bool hasDoor = false;
			for(var dir = 0; dir < 4; dir++) {
				if(!hasDoor || s.Chance(doorChance)) {
					hasDoor = true;
					var sDoor = s.Rotate(dir);
					var offset = sDoor.RandInclusive(0, 2) + 2;
					var doorZ = sDoor.Chance(.5F) ? sDoor.MinZ + offset : sDoor.MaxZ - offset;
					sDoor.Spawn(sDoor.MaxX, doorZ, ThingDefOf.Door, stuff);
				}
			}

			if(buildingDecorators.Count > 0) {
				buildingDecorators.RandomElement().Decorate(s);
			}

			s.Expand(1).BorderTerrain(GenCity.RandomFloor(s.map), p => IsValidTile(s.map, p));
		}

		private void GenRooms(Stencil s, bool parentWall) {
			s = s.Center();
			var room = roomDecorators.RandomElementByWeight(r => r.weight / r.maxArea);
			if(s.Area > room.maxArea) {
				if(s.Width < s.Height) {
					s = s.Rotate(1 + s.RandInclusive(0, 1) * 2);
				}

				var wallX = Mathf.RoundToInt(s.RandX * .3F);
				var hasWall = s.Chance(wallChance);

				var left = s.Bound(s.MinX, s.MinZ, wallX, s.MaxZ);
				var right = s.Bound(wallX, s.MinZ, s.MaxX, s.MaxZ);
				GenRooms(left, hasWall);
				GenRooms(right, hasWall);

				if(hasWall) {
					var stuff = RandomWallStuff(s.map, true);
					s.Fill(wallX, s.MinZ + 1, wallX, s.MaxZ - 1, ThingDefOf.Wall, stuff);
					if(parentWall) {
						var offset = s.RandInclusive(0, 2) + 1;
						s.Spawn(wallX, s.Chance(.5F) ? s.MinZ + offset : s.MaxZ - offset, ThingDefOf.Door, stuff);
					}
				}
			}
			else {
				var sInterior = s.Expand(-1);
				sInterior.ClearThingsInBounds();
				s.FillTerrain(RandomFloor(s.map));

				if(room.roofed) {
					s.FillRoof(RoofDefOf.RoofConstructed);
				}
				if(s.Chance(room.lightChance)) {
					var sLamp = s.Expand(-1).MoveRand();
					//if(sLamp.Bound(0, 0, 0, 0).All(p => p.GetFirstThing<Thing>(sLamp.map) == null)) {
					sLamp.Spawn(ThingDefOf.StandingLamp);
					//}
				}
				try {
					room.Decorate(sInterior);
				}
				catch(System.Exception e) {
					Log.Error("Error occurred in room decorator type: " + room.GetType().Name);
					throw e;
				}
			}
		}

		private TerrainDef RandomFloor(Map map) {
			return floorOptions.RandomElementWithFallback() ?? GenCity.RandomFloor(map);
		}

		private ThingDef RandomWallStuff(Map map, bool interior = false) {
			return wallStuffOptions.RandomElementWithFallback() ?? GenCity.RandomWallStuff(map, interior);
		}
	}

	public abstract class Decorator {
		public float weight = 1;

		public abstract void Decorate(Stencil s);
	}

	public static class GenCity {
		public static TerrainDef RandomFloor(Map map) {
			return BaseGenUtility.RandomBasicFloorDef(map.ParentFaction);
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

		public static Pawn SpawnInhabitant(IntVec3 pos, Map map, LordJob job) {
			return SpawnInhabitant(pos, map, LordMaker.MakeNewLord(map.ParentFaction, job, map));
		}

		public static Pawn SpawnInhabitant(IntVec3 pos, Map map, Lord lord = null) {
			var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(map.ParentFaction.RandomPawnKind(), map.ParentFaction, PawnGenerationContext.NonPlayer, map.Tile, inhabitant: true));
			if(lord != null) {
				lord.AddPawn(pawn);
			}
			GenSpawn.Spawn(pawn, pos, map);
			return pawn;
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