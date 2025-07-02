using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {
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
            var live = new LordToil_LiveInCitadel(workSpot);
            // graph.StartingToil = live;
            graph.AddToil(live);
            // var attack = new LordToil_AssaultColony(canPickUpOpportunisticWeapons: true);
            // graph.AddToil(attack);
            // var transition = new Transition(live, attack);
            // graph.AddTransition(transition);
            // transition.AddTrigger(new Trigger_PawnHarmed());
            // transition.AddTrigger(new Trigger_ChanceOnTickInterval(2500, 0.03f));
            return graph;
        }

        public override void ExposeData() {
            Scribe_Values.Look(ref workSpot, "workSpot");
        }
    }
}