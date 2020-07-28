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
    public class LordToil_Captor : LordToil {
        public Pawn hostage;

        public LordToil_Captor(Pawn hostage) {
            this.hostage = hostage;
        }

        public override void UpdateAllDuties() {
            foreach (var pawn in lord.ownedPawns) {
                pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("CityCaptor"), hostage);
            }
        }
    }
}
