using RimWorld;
using Verse;

namespace Cities {
    
    public class RoomDecorator_FrozenStorage : RoomDecorator_Storage {
        public override void Decorate(Stencil s) {
            base.Decorate(s);

            var sVent = s.RotateRand().Move(0, s.MaxZ - 1).Bound(-1, -1, 1, 1);
            sVent.Border(ThingDefOf.Wall, GenCity.RandomWallStuff(s.map));
            sVent.Bound(0, 0, 0, 0).ClearRoof();
            var tempControl = sVent.Spawn(0, -1, ThingDefOf.Cooler).TryGetComp<CompTempControl>();
            tempControl.targetTemperature = -1;
        }
    }
}