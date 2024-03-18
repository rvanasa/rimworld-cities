using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace Cities {
    public static class DebugActionsCities {
        
        [DebugAction("General", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
        public static void PopulateGhostCities() {
            foreach (var settlement in Find.WorldObjects.Settlements) {
                if (settlement is City city && !city.Destroyed && city.inhabitantFaction == null) {
                    city.inhabitantFaction = city.Faction;
                }
            }
        }
    }
}