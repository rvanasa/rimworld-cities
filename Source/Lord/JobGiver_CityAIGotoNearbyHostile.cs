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
    public class JobGiver_CityAIGotoNearbyHostile : JobGiver_AIGotoNearestHostile {
        public float maxDistance = 40;

        protected override Job TryGiveJob(Pawn pawn) {
            var bestDistSq = maxDistance * maxDistance;
            var bestThing = (Thing) null;
            var potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            foreach (var target in potentialTargetsFor) {
                if (!target.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(target)) {
                    var thing = (Thing) target;
                    var distSq = thing.Position.DistanceToSquared(pawn.Position);
                    if (distSq < bestDistSq && pawn.CanReach((LocalTargetInfo) thing, PathEndMode.OnCell, Danger.Deadly, true, true)) {
                        bestDistSq = distSq;
                        bestThing = thing;
                    }
                }
            }
            if (bestThing == null) {
                return null;
            }
            var job = JobMaker.MakeJob(JobDefOf.Goto, (LocalTargetInfo) bestThing);
            job.checkOverrideOnExpire = true;
            job.expiryInterval = 500;
            job.collideWithPawns = true;
            return job;
        }
    }
}