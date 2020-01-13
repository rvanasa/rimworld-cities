using RimWorld;
using Verse;

namespace Cities
{
    public class RoomDecorator_HospitalBed : RoomDecorator {
        public RoomDecorator_HospitalBed() {
            lightChance = 1;
        }

        public override void Decorate(Stencil s) {
            var sBed = s.Expand(-1);
            var thing = ThingDefOf.Bed;
            var stuff = GenCity.RandomStuff(thing, s.map);
            var monitorDef = DefDatabase<ThingDef>.GetNamed("VitalsMonitor");
            var monitor = sBed.Spawn(sBed.RandX, sBed.MaxZ, monitorDef);
            var bed = (Building_Bed)sBed.Spawn(sBed.RandX, sBed.RandZ, thing, stuff);
            bed.SetFactionDirect(s.map.ParentFaction);
            bed.Medical = true;
        }
    }
}