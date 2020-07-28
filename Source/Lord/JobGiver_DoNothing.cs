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
    public class JobGiver_DoNothing : ThinkNode_JobGiver {
        public override float GetPriority(Pawn pawn) {
            return 0;
        }

        protected override Job TryGiveJob(Pawn pawn) {
            return JobMaker.MakeJob(JobDefOf.Wait, 100);
        }
    }
}