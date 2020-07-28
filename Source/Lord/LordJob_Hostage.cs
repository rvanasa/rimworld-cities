﻿using Verse;
using Verse.AI.Group;

namespace Cities
{
	public class LordJob_Hostage : LordJob {

		public override bool AddFleeToil => false;

		public LordJob_Hostage() {
		}

		public override StateGraph CreateGraph() {
			var graph = new StateGraph();
			graph.AddToil(new LordToil_Hostage());
			return graph;
		}
	}
}