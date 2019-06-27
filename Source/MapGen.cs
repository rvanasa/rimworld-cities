using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

	public class MapComponent_QuestTracker : MapComponent {
		public MapComponent_QuestTracker(Map map) : base(map) {
		}

		public override void MapGenerated() {
			bool hasQuest = false;
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				quest.MapGenerated(map);
				hasQuest = true;
			}
			if(hasQuest) {
				Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
			}
		}

		public override void MapRemoved() {
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				quest.MapRemoved(map);
			}
		}
	}

	public class MapComponent_City : MapComponent {
		const float DailyFoodGivenPerPerson = 0.5F;
		const int EventTimeCycle = 40_000;
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
			if((Find.TickManager.TicksGame + map.Parent.ID) % EventTimeCycle == 0) {
				if(map.Parent is City city && !city.Abandoned) {
					if(city.Faction.HostileTo(Faction.OfPlayer)) {
						var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
						var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
						parms.faction = city.Faction;
						parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
						parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetRandom();
						parms.raidArrivalModeForQuickMilitaryAid = true;
						parms.points += RaidPointIncrease;
						IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
					}
					else {
						var pos = QuestUtility.FindDropSpot(map);
						var things = new List<Thing>();
						var foodCount = (int)(map.mapPawns.SpawnedPawnsInFaction(map.ParentFaction).Count * EventTimeCycle / 60000F * DailyFoodGivenPerPerson);
						for(var i = 0; i < foodCount; i++) {
							var thing = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
							map.GetComponent<MapComponent_City>().cityOwnedThings.Add(thing);
							things.Add(thing);
						}
						DropPodUtility.DropThingsNear(pos, map, things, canRoofPunch: false);
					}
				}
			}
		}
	}

	public abstract class Decorator {
		public float weight = 1;

		public abstract void Decorate(Stencil s);
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
		public FloatRange corpseChance = new FloatRange(0, 0.1F);
		public float remnantDensity = 0.15F;
		public float scavengerDensity = 0.5F;

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			var playerFaction = Faction.OfPlayer;
			var decay = this.decay.RandomInRange;
			var corpseChance = this.corpseChance.RandomInRange;
			//var autoClaim = Find.Maps.Count == 1;
			var autoClaim = false;
			foreach(var pos in map.cellsInRandomOrder.GetAll()) {
				var things = pos.GetThingList(map);
				for(var i = things.Count - 1; i >= 0; i--) {
					var thing = things[i];
					if(ShouldDestroy(thing, decay) && !thing.Destroyed) {
						if(thing is Pawn pawn && !pawn.Faction.IsPlayer && Rand.Chance(corpseChance)) {
							pawn.Kill(null);
							pawn.Corpse.timeOfDeath -= Rand.RangeInclusive(10, 500) * 1000;
						}
						else {
							thing.Destroy();
						}
					}
					else if(autoClaim && !(thing is Pawn) && thing.def.CanHaveFaction && thing.Faction == null) {
						thing.SetFactionDirect(playerFaction);
					}
				}

				if(Rand.Chance(decay)) {
					var terrain = pos.RandomAdjacentCell8Way().ClampInsideMap(map).GetTerrain(map);
					map.terrainGrid.SetTerrain(pos, terrain);
				}
			}
		}

		bool ShouldDestroy(Thing thing, float decay) {
			float remnantDensity = this.remnantDensity * (1 - decay);
			bool isScavenger = thing is Pawn pawn && !pawn.NonHumanlikeOrWildMan();
			return thing.def.destroyable && !Rand.Chance(isScavenger ? remnantDensity * scavengerDensity : remnantDensity) && (
				isScavenger
				|| (thing is Building_Turret)
				|| (thing is Plant plant && plant.IsCrop)
				|| (!thing.def.thingCategories.NullOrEmpty() && !thing.def.IsWithinCategory(ThingCategoryDefOf.Buildings) && !thing.def.IsWithinCategory(ThingCategoryDefOf.Chunks))
				|| (thing.TryGetComp<CompGlower>() != null)
				|| (thing is Building && thing.def.CanHaveFaction && Rand.Chance(thing.def == ThingDefOf.Wall ? remnantDensity * remnantDensity : decay)));
		}
	}

	public class GenStep_ClearAnimals : GenStep {
		public FloatRange decay = new FloatRange(0, 1);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			if(map.Parent is City city && city.Abandoned) {
				return;
			}
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
			var stuff = GenCity.RandomWallStuff(map);
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
		public IntRange centerRadius = new IntRange(5, 15);
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

			var radius = centerRadius.RandomInRange;

			var s = new Stencil(map);
			s = s.MoveTo(s.Expand(-radius - 2).bounds.RandomCell);

			s.Bound(-radius, -radius, radius, radius)
				.ClearThingsInBounds()
				.FillTerrain(sidewalkTerrains.RandomElement());

			s.FillTerrain(-2, -2, 2, 2, roadTerrain);

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

	public class GenStep_Bazaars : GenStep_RectScatterer {
		public float standChance = 0.4F;

		public override void GenerateRect(Stencil s) {
			s.FillTerrain(GenCity.RandomFloor(s.map));
			var pawn = (Pawn)null;
			if(!s.map.ParentFaction.HostileTo(Faction.OfPlayer)) {
				pawn = GenCity.SpawnInhabitant(s.Coords(s.RandX / 2, s.RandZ / 2), s.map);
				var traderKind = DefDatabase<TraderKindDef>.AllDefs.RandomElement();
				pawn.mindState.wantsToTradeWithColony = true;
				PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned: true);
				pawn.trader.traderKind = traderKind;
				pawn.inventory.DestroyAll();
				ThingSetMakerParams parms = new ThingSetMakerParams {
					traderDef = traderKind,
					tile = s.map.Tile,
					traderFaction = s.map.ParentFaction,
				};
				foreach(var item in ThingSetMakerDefOf.TraderStock.root.Generate(parms)) {
					if(!(item is Pawn) && !pawn.inventory.innerContainer.TryAdd(item)) {
						item.Destroy();
					}
				}
				PawnInventoryGenerator.GiveRandomFood(pawn);
			}
			for(var dir = 0; dir < 4; dir++) {
				if(s.Chance(standChance)) {
					var sStand = s.Rotate(dir).Move(Mathf.RoundToInt(s.RandX / 2F), s.MaxZ - s.RandInclusive(0, 2)).Bound(-1, 0, 1, 0);
					sStand.FillTerrain(GenCity.RandomFloor(s.map, true));
					var wallStuff = GenCity.RandomWallStuff(s.map);
					sStand.Spawn(sStand.MinX - 1, 0, ThingDefOf.Wall, wallStuff);
					sStand.Spawn(sStand.MaxX + 1, 0, ThingDefOf.Wall, wallStuff);
					sStand.Expand(1).FillRoof(RoofDefOf.RoofConstructed);
					if(pawn != null) {
						var itemPos = sStand.Coords(sStand.RandX, sStand.RandZ);
						var item = pawn.inventory.innerContainer.FirstOrDefault();
						if(item != null) {
							pawn.inventory.innerContainer.TryDrop(item, itemPos, s.map, ThingPlaceMode.Direct, out Thing result);
							item.SetOwnedByCity(true);
						}
					}
				}
			}
		}
	}

	public class GenStep_Fields : GenStep_RectScatterer {
		public float density = 0.9F;
		public AltitudeLayer altitudeLayer = AltitudeLayer.LowPlant;

		public override void GenerateRect(Stencil s) {
			ThingDef plant = DefDatabase<ThingDef>.AllDefs
				.Where(t => t.category == ThingCategory.Plant && t.altitudeLayer == altitudeLayer && !t.plant.cavePlant)
				.RandomElement();
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

		void GenRooms(Stencil s, bool parentWall) {
			s = s.Center();
			var room = roomDecorators.RandomElementByWeight(r => r.weight / r.maxArea);
			if(s.Area > room.maxArea) {
				if(s.Width < s.Height) {
					s = s.Rotate(1 + s.RandInclusive(0, 1) * 2);
				}

				var wallX = Mathf.RoundToInt(s.RandX * .3F);
				var hasWall = s.Chance(wallChance);

				if(hasWall) {
					s.Fill(wallX, s.MinZ + 1, wallX, s.MaxZ - 1, ThingDefOf.Wall, RandomWallStuff(s.map));
				}

				var left = s.Bound(s.MinX, s.MinZ, wallX, s.MaxZ);
				var right = s.Bound(wallX, s.MinZ, s.MaxX, s.MaxZ);
				GenRooms(left, hasWall);
				GenRooms(right, hasWall);

				if(hasWall && parentWall) {
					var offset = s.RandInclusive(0, 2) + 1;
					s.Spawn(wallX, s.Chance(.5F) ? s.MinZ + offset : s.MaxZ - offset, ThingDefOf.Door, RandomWallStuff(s.map/*, true*/));
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
					sLamp.Spawn(ThingDefOf.StandingLamp);
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

		TerrainDef RandomFloor(Map map) {
			return floorOptions.RandomElementWithFallback() ?? GenCity.RandomFloor(map);
		}

		ThingDef RandomWallStuff(Map map) {
			return wallStuffOptions.RandomElementWithFallback() ?? GenCity.RandomWallStuff(map);
		}
	}
}