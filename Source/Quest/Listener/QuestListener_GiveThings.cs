using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Cities
{
	public class QuestListener_GiveThings : QuestListener {
		public FloatRange value = new FloatRange(1000, 2000);

		public List<ThingSetMakerDef> thingSetMakerOptions = new List<ThingSetMakerDef>();

		public override IEnumerable<Result> GetResults(Quest quest) {
			var maker = thingSetMakerOptions.RandomElementWithFallback()
			            ?? ThingSetMakerDefOf.Reward_StandardByDropPod;
			var things = maker.root.Generate(new ThingSetMakerParams {
				totalMarketValueRange = value,
			});
			yield return new DropPodResult(things);
		}
	}
}