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
            base.ExposeData();
            Scribe_Values.Look(ref inhabitantKind, "inhabitantKind");
        }

        public override void DoEditInterface(Listing_ScenEdit listing) {
            var scenPartRect = listing.GetScenPartRect(this, RowHeight * 3 + 31);
            if (Widgets.ButtonTextSubtle(scenPartRect.TopPartPixels(RowHeight), inhabitantKind.ToString())) {
                FloatMenuUtility.MakeMenu(System.Enum.GetNames(typeof(InhabitantKind)), s => (s + "CityKind").Translate(), s => { return () => inhabitantKind = (InhabitantKind)System.Enum.Parse(typeof(InhabitantKind), s); });
            }
        }

        public override void PreMapGenerate() {
            var defaultSettlement = Find.WorldObjects.MapParentAt(Find.GameInitData.startingTile);
            Find.WorldObjects.Remove(defaultSettlement);

            var city = (City)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed(GetObjectDefName()));
            city.SetFaction(Find.GameInitData.playerFaction);
            city.inhabitantFaction = GenCity.RandomCityFaction(IsValidFaction);
            city.Tile = Find.GameInitData.startingTile;
            city.Name = city.ChooseName();
            Find.WorldObjects.Add(city);
        }

        string GetObjectDefName() {
            switch (inhabitantKind) {
                case InhabitantKind.Abandoned:
                    return "City_Abandoned";
                case InhabitantKind.Ghost:
                    return "City_Ghost";
                case InhabitantKind.Compromised:
                    return "City_Compromised";
                default:
                    return "City_Faction";
            }
        }

        bool IsValidFaction(Faction faction) {
            var playerFaction = Faction.OfPlayer;
            switch (inhabitantKind) {
                case InhabitantKind.Friendly:
                    return !faction.HostileTo(playerFaction);
                case InhabitantKind.Hostile:
                    return faction.HostileTo(playerFaction) && faction.def.CanEverBeNonHostile;
                case InhabitantKind.Pirate:
                    return !faction.def.CanEverBeNonHostile;
                case InhabitantKind.Compromised:
                    return !faction.def.CanEverBeNonHostile;
                default:
                    return false;
            }
        }

        enum InhabitantKind {
            Abandoned,
            Ghost,
            Friendly,
            Hostile,
            Pirate,
            Compromised,
        }
    }
}