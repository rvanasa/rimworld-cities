using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

    public class ScenPart_StartCitadel : ScenPart {

        public override bool CanCoexistWith(ScenPart other) {
            return !(other is ScenPart_StartCity);
        }

        public override void PreMapGenerate() {
            var defaultSettlement = Find.WorldObjects.MapParentAt(Find.GameInitData.startingTile);
            Find.WorldObjects.Remove(defaultSettlement);

            var citadel = (Citadel) WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("City_Citadel"));
            citadel.SetFaction(Find.GameInitData.playerFaction);
            citadel.inhabitantFaction = GenCity.RandomCityFaction(f => !f.def.CanEverBeNonHostile);
            citadel.Tile = Find.GameInitData.startingTile;
            citadel.Name = citadel.ChooseName();
            Find.WorldObjects.Add(citadel);
        }
    }
}