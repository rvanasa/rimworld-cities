using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cities {
    public class GenStep_Narrow_Post : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void Generate(Map map, GenStepParams parms) {
            foreach (var thing in map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Bed))) {
                if (thing is Building_Bed bed) {
                    bed.SetFactionDirect(Faction.OfPlayer);
                    bed.Medical = true;
                }
            }

            var safeZone = 50;
            foreach (var pawn in map.mapPawns.AllPawns) {
                if (pawn.Position.z <= safeZone && pawn.Faction.HostileTo(Faction.OfPlayer)) {
                    pawn.Position = new IntVec3(pawn.Position.x, 0, pawn.Position.z + safeZone);
                }
            }
        }
    }
}