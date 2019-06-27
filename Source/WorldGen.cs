using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

	public class City : Settlement {
		public Faction inhabitantFaction;

		public bool Abandoned => inhabitantFaction == null;

		public override MapGeneratorDef MapGeneratorDef => DefDatabase<MapGeneratorDef>.GetNamed(
				inhabitantFaction != null ? "City_Faction" : "City_Abandoned");

		public override Texture2D ExpandingIcon => def.ExpandingIconTexture;

		public override Color ExpandingIconColor => inhabitantFaction != null ? base.ExpandingIconColor : Color.grey;

		public override bool Visitable => base.Visitable || Abandoned;

		public override bool Attackable => base.Attackable && !Abandoned;

		public override void SetFaction(Faction newFaction) {
			base.SetFaction(newFaction);
			if(inhabitantFaction != null) {
				inhabitantFaction = newFaction;
			}
		}

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
				var action = new Command_Action {
					icon = SettleUtility.SettleCommandTex,
					defaultLabel = "EnterCity".Translate(),
					defaultDesc = "EnterCityDesc".Translate(),
					action = () => {
						LongEventHandler.QueueLongEvent(() => {
							var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(Tile, null);
							CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
						}, "GeneratingMapForNewEncounter", false, null);
					},
				};
				if(this.EnterCooldownBlocksEntering()) {
					action.disabled = true;
					action.disabledReason = "MessageEnterCooldownBlocksEntering".Translate(this.EnterCooldownDaysLeft().ToString("0.#"));
				}
				yield return action;
			}
			foreach(var gizmo in base.GetCaravanGizmos(caravan)) {
				yield return gizmo;
			}
		}

		public override void ExposeData() {
			Scribe_References.Look(ref inhabitantFaction, "inhabitantFaction");
			base.ExposeData();
		}

		public virtual void PreMapGenerate(Map map) {
			SetFaction(inhabitantFaction ?? GenCity.RandomCityFaction(f => f.HostileTo(Faction.OfPlayer)));
		}
	}

	public class WorldGenStep_Cities : WorldGenStep {
		public override int SeedPart => GetType().Name.GetHashCode();

		public override void GenerateFresh(string seed) {
			var settings = LoadedModManager.GetMod<Mod_Cities>().GetSettings<ModSettings_Cities>();
			GenerateCities(settings.citiesPer100kTiles.RandomInRange, false);
			GenerateCities(settings.abandonedPer100kTiles.RandomInRange, true);
		}

		public override void GenerateFromScribe(string seed) {
			if(!Find.WorldObjects.AllWorldObjects.Any(obj => obj is City)) {
				Log.Warning("No cities found; regenerating");
				GenerateFresh(seed);
			}
		}

		void GenerateCities(int per100kTiles, bool abandoned) {
			int cityCount = GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100000F * per100kTiles);
			for(int i = 0; i < cityCount; i++) {
				var city = (City)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed(abandoned ? "City_Abandoned" : "City_Faction"));
				city.SetFaction(GenCity.RandomCityFaction());
				if(!abandoned) {
					city.inhabitantFaction = city.Faction;
				}
				city.Tile = TileFinder.RandomSettlementTileFor(city.Faction);
				city.Name = SettlementNameGenerator.GenerateSettlementName(city);
				if(!TileFinder.IsValidTileForNewSettlement(city.Tile)) {
					// (Faction Control) ensure valid tile for existing saves
					city.Tile = TileFinder.RandomStartingTile();
				}
				Find.WorldObjects.Add(city);
			}
		}
	}

	public class WorldComponent_QuestTracker : WorldComponent {
		public List<Quest> quests = new List<Quest>();

		public WorldComponent_QuestTracker(World world) : base(world) {
		}

		public override void ExposeData() {
			Scribe_Collections.Look(ref quests, "quests");
		}

		public override void WorldComponentTick() {
			var tick = Find.TickManager.TicksGame;
			for(int i = quests.Count - 1; i >= 0; i--) {
				var quest = quests[i];
				if(tick >= quest.expireTime && !quest.Ended) {
					quest.Expire();
				}

				if(quest.Ended) {
					quests.RemoveAt(i);
				}
				else {
					quest.OnTick();
				}
			}
		}
	}

	/*public class WorldObjectCompProperties_Quests : WorldObjectCompProperties {
		public WorldObjectCompProperties_Quests() {
			compClass = typeof(WorldObjectComp_Quests);
		}
	}

	public class WorldObjectComp_Quests : WorldObjectComp {
	}*/
}