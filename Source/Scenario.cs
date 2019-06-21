using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

	public class ScenPart_StartCity : ScenPart {
		InhabitantKind inhabitantKind = InhabitantKind.Abandoned;

		public override void ExposeData() {
			Scribe_Values.Look(ref inhabitantKind, "inhabitantKind");
			base.ExposeData();
		}

		public override void DoEditInterface(Listing_ScenEdit listing) {
			var scenPartRect = listing.GetScenPartRect(this, RowHeight * 3 + 31);
			if(Widgets.ButtonText(scenPartRect.TopPartPixels(RowHeight), inhabitantKind.ToString())) {
				FloatMenuUtility.MakeMenu(System.Enum.GetNames(typeof(InhabitantKind)), s => s, s => {
					return () => inhabitantKind = (InhabitantKind)System.Enum.Parse(typeof(InhabitantKind), s);
				});
			}
		}

		public override void PreMapGenerate() {
			var defaultSettlement = Find.WorldObjects.MapParentAt(Find.GameInitData.startingTile);
			Find.WorldObjects.Remove(defaultSettlement);

			var city = (City)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed(GetObjectDefName()));
			city.SetFaction(Find.GameInitData.playerFaction);
			city.inhabitantFaction = city.FindFaction(IsValidFaction);
			city.Tile = Find.GameInitData.startingTile;
			city.Name = SettlementNameGenerator.GenerateSettlementName(city, city.inhabitantFaction?.def.factionNameMaker);
			Find.WorldObjects.Add(city);
		}

		public override string Summary(Scenario scen) {
			return "Instead of spawning in the wilderness, you will start inside a city.";
		}

		string GetObjectDefName() {
			return inhabitantKind == InhabitantKind.Abandoned ? "City_Abandoned" : "City_Faction";
		}

		bool IsValidFaction(Faction faction) {
			var playerFaction = Faction.OfPlayer;
			switch(inhabitantKind) {
				case InhabitantKind.Abandoned:
					return false;
				case InhabitantKind.Hostile:
					return faction.HostileTo(playerFaction);
				case InhabitantKind.Friendly:
					return !faction.HostileTo(playerFaction);
				default:
					return true;
			}
		}

		enum InhabitantKind {
			Abandoned,
			Friendly,
			Hostile,
		}
	}
}