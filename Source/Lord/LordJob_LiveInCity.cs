using Verse;
using Verse.AI.Group;

namespace Cities
{
	public class LordJob_LiveInCity : LordJob {
		public IntVec3 workSpot;

		public override bool AddFleeToil => false;

		public LordJob_LiveInCity() {
		}

		public LordJob_LiveInCity(IntVec3 workSpot) {
			this.workSpot = workSpot;
		}

		public override StateGraph CreateGraph() {
			var graph = new StateGraph();
			graph.AddToil(new LordToil_LiveInCity(workSpot));
			return graph;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref workSpot, "workSpot");
		}
	}
}