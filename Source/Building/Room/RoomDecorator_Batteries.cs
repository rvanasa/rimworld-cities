using RimWorld;
using Verse;

namespace Cities
{
    public class RoomDecorator_Batteries : RoomDecorator {
        public FloatRange charge = new FloatRange(0.5F, 1);

        public override void Decorate(Stencil s) {
            var charge = this.charge.RandomInRange;
            for(int x = s.MinX + 1; x <= s.MaxX - 1; x++) {
                var batteryComp = s.Spawn(x, 0, ThingDefOf.Battery).TryGetComp<CompPowerBattery>();
                if(batteryComp != null) {
                    batteryComp.SetStoredEnergyPct(charge);
                }
            }
        }
    }
}