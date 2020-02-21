using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;

namespace Cities {
    public class QuestPart_CityQuest : RimWorld.QuestPart {

        public Faction[] factions = { };
        public GlobalTargetInfo[] targets = { };

        public override IEnumerable<Faction> InvolvedFactions {
            get {
                foreach (var faction in factions) {
                    yield return faction;
                }
            }
        }

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets {
            get {
                foreach (var target in targets) {
                    yield return target;
                }
            }
        }
    }
}