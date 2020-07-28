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
        }
    }
}