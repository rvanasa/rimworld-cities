using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cities {
    public class Citadel : City {
        const int MapGenArea = 100_000;
        const int MapGenWidth = 80;

        public override int RaidPointIncrease => 1000;

        public override string ChooseName() {
            return "CitadelName".Translate(base.ChooseName());
        }

        public override IntVec3 ChooseMapSize(IntVec3 mapSize) {
            mapSize.x = MapGenWidth;
            mapSize.z = MapGenArea / MapGenWidth;

            return mapSize;
        }
    }
}
