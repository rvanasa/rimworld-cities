using System;
using System.Linq;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace Cities {
    public class QuestNode_GetNearbyCity : QuestNode_GetNearbySettlement {

        public SlateRef<bool> visitable = true;
        public SlateRef<bool> abandoned = false;
        public SlateRef<bool> hasMap = false;

        City RandomNearbyCity(Map homeMap, Slate slate) {
            return Find.WorldObjects.Settlements
                .OfType<City>()
                .Where(s => s.Visitable == visitable.GetValue(slate)
                            && s.Abandoned == abandoned.GetValue(slate)
                            && s.HasMap == hasMap.GetValue(slate))
                .RandomByDistance(homeMap?.Parent, Mathf.CeilToInt(maxTileDistance.GetValue(slate)));
        }

        protected override void RunInt() {
            var slate = QuestGen.slate;
            var home = QuestGen.slate.Get<Map>("map");
            var city = RandomNearbyCity(home, slate);

            QuestGen.slate.Set(storeAs.GetValue(slate), city);

            if (!string.IsNullOrEmpty(storeFactionAs.GetValue(slate))) {
                QuestGen.slate.Set(storeFactionAs.GetValue(slate), city.Faction);
            }

            if (!storeFactionLeaderAs.GetValue(slate).NullOrEmpty()) {
                QuestGen.slate.Set(storeFactionLeaderAs.GetValue(slate), city.Faction.leader);
            }
        }

        protected override bool TestRunInt(Slate slate) {
            var home = slate.Get<Map>("map");
            var city = RandomNearbyCity(home, slate);
            if (city == null) {
                return false;
            }

            slate.Set(storeAs.GetValue(slate), city);

            if (!string.IsNullOrEmpty(storeFactionAs.GetValue(slate))) {
                slate.Set(storeFactionAs.GetValue(slate), city.Faction);
            }

            return true;
        }
    }
}