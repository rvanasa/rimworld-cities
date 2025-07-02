using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Cities {

    public class GenStep_Emplacements : GenStep_RectScatterer {
        public List<EmplacementOption> options = new List<EmplacementOption>();

        public override void GenerateRect(Stencil s) {
            if (!Config_Cities.Instance.enableTurrets) {
                return;
            }
            var stuff = GenCity.RandomStuff(ThingDefOf.Sandbags, s.map);
            for (var dir = 0; dir < 4; dir++) {
                var sDir = s.Rotate(dir);
                for (var x = sDir.MinX; x <= sDir.MaxX; x++) {
                    if (x <= sDir.MinX / 2 || x >= sDir.MaxX / 2) {
                        sDir.Spawn(x, sDir.MaxZ, ThingDefOf.Sandbags, stuff);
                    }
                }
            }

            var emplacementOption = options
                .Where(opt => {
                    if (opt.selfDestructive) {
                        return Config_Cities.Instance.enableMortars && s.map.ParentFaction.def.permanentEnemy;
                    }
                    return Config_Cities.Instance.enableTurrets;
                })
                .RandomElementByWeight(opt => opt.weight);
            emplacementOption?.Generate(s);
        }

        protected override bool IsValidTile(Map map, IntVec3 pos) {
            return base.IsValidTile(map, pos) && pos.GetFirstThing<Thing>(map) == null;
        }

        public class EmplacementOption {
            public float weight = 1;
            public bool roofed = false;
            public bool manned = false;
            public bool selfDestructive = false;
            public ThingDef weaponDef;
            public ThingDef ammoDef;
            public IntRange ammoCount = new IntRange(1, 20);

            public virtual void Generate(Stencil s) {
                var weapon = s.Spawn(weaponDef, weaponDef.MadeFromStuff ? ThingDefOf.Steel : null);
                weapon.SetFactionDirect(s.map.GetCityFaction());
                if (ammoDef != null) {
                    var ammo = s.RotateRand().Spawn(s.RandInclusive(-1, 1), s.RandInclusive(2, 3), ammoDef);
                    ammo.stackCount = ammoCount.RandomInRange;
                    ammo.SetOwnedByCity(true, s.map);
                }

                if (roofed) {
                    var stuff = GenCity.RandomWallStuff(s.map);
                    s.Spawn(s.MinX, s.MinZ, ThingDefOf.Wall, stuff);
                    s.Spawn(s.MaxX, s.MinZ, ThingDefOf.Wall, stuff);
                    s.Spawn(s.MinX, s.MaxZ, ThingDefOf.Wall, stuff);
                    s.Spawn(s.MaxX, s.MaxZ, ThingDefOf.Wall, stuff);
                    s.FillRoof(RoofDefOf.RoofConstructed);
                }

                if (manned) {
                    GenCity.SpawnInhabitant(s.pos, s.map, new LordJob_ManTurrets());
                }
            }
        }
    }
}