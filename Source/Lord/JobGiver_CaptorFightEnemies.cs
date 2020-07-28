﻿﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace Cities {
    public class JobGiver_CaptorFightEnemies : JobGiver_AIFightEnemies {

        protected override bool ExtraTargetValidator(Pawn pawn, Thing target) {
            return /*pawn.mindState.enemyTarget != null || */(target is Pawn other && other.Faction == Faction.OfPlayer);
        }

    }
}