using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class ScenPart_RescuePrisoners : ScenPart {

        public override void Tick() {
            var map = Find.CurrentMap;
            if (map != null && Find.TickManager.TicksGame == 20) {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned) {
                    if (pawn.IsPrisoner) {
                        pawn.SetFactionDirect(Find.FactionManager.RandomNonHostileFaction());
                        pawn.mindState.WillJoinColonyIfRescued = true;
                    }
                }
            }
        }
    }
}