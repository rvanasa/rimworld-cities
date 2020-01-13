using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Cities
{
	public class DropPodResult : Result {
		public List<Thing> things;

		public override string Label => GenThing.ThingsToCommaList(things) + " (" + "QuestAppendWorth".Translate().Formatted(GenThing.GetMarketValue(things).ToStringMoney()) + ")";

		public DropPodResult() {
		}

		public DropPodResult(List<Thing> things) {
			this.things = things;
		}

		public override void ExposeData() {
			Scribe_Collections.Look(ref things, "things", LookMode.Deep);
		}

		public override void OnResult(Quest quest) {
			var map = quest.HomeMap;
			var pos = QuestUtility.FindDropSpot(map);
			DropPodUtility.DropThingsNear(pos, map, things, canRoofPunch: false);
			Messages.Message("QuestReceived".Translate().Formatted(Label), new LookTargets(pos, map), MessageTypeDefOf.PositiveEvent);
		}
	}
}