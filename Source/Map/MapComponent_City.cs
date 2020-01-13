using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Cities {

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
				if(!Config_Cities.Instance.enableEvents) {
					return;
				}

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
}