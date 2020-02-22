using Verse;
using Verse.AI.Group;

namespace Cities
{
	// TODO dry
	public class LordJob_LiveInCitadel : LordJob {
		public IntVec3 workSpot;

		public override bool AddFleeToil => false;

		public LordJob_LiveInCitadel() {
		}

		public LordJob_LiveInCitadel(IntVec3 workSpot) {
			this.workSpot = workSpot;
		}

		public override StateGraph CreateGraph() {
			var graph = new StateGraph();
			graph.AddToil(new LordToil_LiveInCitadel(workSpot));
			return graph;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref workSpot, "workSpot");
		}
	}
}