using System.Collections.Generic;
using Verse;

namespace Cities
{
    public class RoomDecorator_Centerpiece : RoomDecorator {
        public List<ThingDef> options = new List<ThingDef>();
        public List<ThingDef> chairOptions = new List<ThingDef>();
        public float chairDensity;
        //public float chairPawnChance = 0.1F;
        public float chairPawnChance = 0;

        public override void Decorate(Stencil s) {
            s.Bound(s.RandInclusive(s.MinX, s.MinX / 2) - 1, s.RandInclusive(s.MinZ, s.MinZ / 2) - 1, s.RandInclusive(s.MaxX / 2, s.MaxX) + 1, s.RandInclusive(s.MaxZ / 2, s.MaxZ) + 1)
                .FillTerrain(GenCity.RandomFloor(s.map, true));

            var def = options.RandomElement();
            var thing = s.Spawn(def, GenCity.RandomStuff(def, s.map));
            if(chairDensity > 0) {
                var chairDef = chairOptions.RandomElement();
                var chairStuff = GenCity.RandomStuff(chairDef, s.map);
                var sThing = s.BoundTo(thing.OccupiedRect());
                for(var dir = 0; dir < 4; dir++) {
                    var sDir = sThing.Rotate(dir);
                    for(var x = sDir.MinX; x <= sDir.MaxX; x++) {
                        if(s.Chance(chairDensity)) {
                            SpawnChair(sDir.Move(x, sDir.MinZ - 1), chairDef, chairStuff);
                        }
                    }
                }
            }
            else if(thing.def.hasInteractionCell && chairOptions.Count > 0) {
                var chairDef = chairOptions.RandomElement();
                var chairStuff = GenCity.RandomStuff(chairDef, s.map);
                SpawnChair(s.MoveTo(thing.InteractionCell), chairDef, chairStuff);
            }
        }

        private void SpawnChair(Stencil s, ThingDef thing, ThingDef stuff) {
            s.Spawn(thing, stuff);
            if(s.Chance(chairPawnChance)) {
                GenCity.SpawnInhabitant(s.pos, s.map);
            }
        }
    }
}