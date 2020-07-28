﻿using Verse;
using Verse.AI.Group;

namespace Cities {
    public class LordJob_Captor : LordJob {
        public Pawn hostage;

        public override bool AddFleeToil => false;

        public LordJob_Captor() {
        }

        public LordJob_Captor(Pawn hostage) {
            this.hostage = hostage;
        }

        public override StateGraph CreateGraph() {
            var graph = new StateGraph();
            graph.AddToil(new LordToil_Captor(hostage));
            return graph;
        }

        public override void ExposeData() {
            Scribe_References.Look(ref hostage, "hostage");
        }
    }
}