using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
    public class QuestPart_CityQuest : RimWorld.QuestPart {
        public List<Faction> factions = new List<Faction>();
        public List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Collections.Look(ref factions, "factions", LookMode.Reference);
            Scribe_Collections.Look(ref targets, "targets", LookMode.GlobalTargetInfo);
        }

        public override IEnumerable<Faction> InvolvedFactions => factions;
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets => targets;
    }
}