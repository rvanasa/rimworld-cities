using Verse;
using Verse.AI.Group;

namespace Cities
{
	// TODO dry
	public class LordJob_LiveInAbandonedCity : LordJob {
		public IntVec3 workSpot;

		public override bool AddFleeToil => true;

		public LordJob_LiveInAbandonedCity() {
		}

		public LordJob_LiveInAbandonedCity(IntVec3 workSpot) {
			this.workSpot = workSpot;
		}

		public override StateGraph CreateGraph() {
			var graph = new StateGraph();
			graph.AddToil(new LordToil_LiveInAbandonedCity(workSpot));
			return graph;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref workSpot, "workSpot");
		}
	}
}