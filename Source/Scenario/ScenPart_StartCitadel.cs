using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System;

namespace Cities {

    public class ScenPart_StartCitadel : ScenPart {

        public override bool CanCoexistWith(ScenPart other) {
            return !(other is ScenPart_StartCity);
        }

        public override void PreMapGenerate() {
            var defaultSettlement = Find.WorldObjects.MapParentAt(Find.GameInitData.startingTile);
            Find.WorldObjects.Remove(defaultSettlement);

            var citadel = (Citadel)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("City_Citadel"));
            citadel.SetFaction(Find.GameInitData.playerFaction);
            // var factionDef = DefDatabase<FactionDef>.GetNamed("OutlanderCivil", errorOnFail: false);
            // Faction faction;
            // if (factionDef != null) {
            //     faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(factionDef));
            // } else {
            //     Log.Warning("RimCities: unable to find suitable citadel inhabitant faction. Choosing random!");
            //     faction = GenCity.RandomCityFaction(f => !f.def.CanEverBeNonHostile);
            // }
            var faction = GenCity.RandomCityFaction(f => !f.def.CanEverBeNonHostile);
            faction.SetRelation(new FactionRelation(Faction.OfPlayer, FactionRelationKind.Hostile));
            // citadel.SetFaction(faction);
            citadel.inhabitantFaction = faction;
            citadel.Tile = Find.GameInitData.startingTile;
            citadel.Name = citadel.ChooseName();
            Find.WorldObjects.Add(citadel);
        }
    }
}