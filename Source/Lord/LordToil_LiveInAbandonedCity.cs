﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace Cities {
    // TODO dry
    public class LordToil_LiveInAbandonedCity : LordToil {
        public IntVec3 workSpot;

        public LordToil_LiveInAbandonedCity(IntVec3 workSpot) {
            this.workSpot = workSpot;
        }

        public override void UpdateAllDuties() {
            foreach (var pawn in lord.ownedPawns) {
                pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("LiveInAbandonedCity"), workSpot);
            }
        }
    }
}