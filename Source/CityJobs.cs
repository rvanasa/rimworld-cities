using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace Cities {

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

	public class LordToil_LiveInCity : LordToil {
		public IntVec3 workSpot;

		public LordToil_LiveInCity(IntVec3 workSpot) {
			this.workSpot = workSpot;
		}

		public override void UpdateAllDuties() {
			foreach(var pawn in lord.ownedPawns) {
				pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("LiveInCity"), workSpot);
			}
		}
	}
}