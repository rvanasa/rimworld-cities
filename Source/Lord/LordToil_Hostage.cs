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
    public class LordToil_Hostage : LordToil {

        public LordToil_Hostage() {
        }

        public override void UpdateAllDuties() {
            foreach (var pawn in lord.ownedPawns) {
                pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("CityHostage"));
            }
        }
    }
}