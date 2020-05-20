using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cities {
    public class GenStep_Abandoned_Post : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public FloatRange decay = new FloatRange(0, 1);
        public FloatRange corpseChance = new FloatRange(0, 0.3F);
        public float remnantDensity = 0.15F;
        public float scavengerDensity = 0.5F;
        public FloatRange maxItemValue = new FloatRange(300, 400);

        public override void Generate(Map map, GenStepParams parms) {
            var decay = this.decay.RandomInRange;
            var corpseChance = this.corpseChance.RandomInRange;
            var maxItemValue = this.maxItemValue.RandomInRange;
            var _currentThing = (Thing) null;
            foreach (var pos in map.cellsInRandomOrder.GetAll()) {
                try {
                    var things = pos.GetThingList(map);
                    for (var i = things.Count - 1; i >= 0; i--) {
                        var thing = things[i];
                        _currentThing = thing;
                        if ((thing.MarketValue > maxItemValue || ShouldDestroy(thing, decay)) && !thing.Destroyed) {
                            if (thing is Pawn pawn && !pawn.Faction.IsPlayer && Rand.Chance(corpseChance)) {
                                pawn.equipment.DestroyAllEquipment();
                                foreach (var apparel in pawn.apparel.WornApparel) {
                                    if (apparel.MarketValue > maxItemValue * 2) {
                                        pawn.apparel.Remove(apparel);
                                    }
                                }
                                pawn.Kill(null);
                                pawn.Corpse.timeOfDeath -= Rand.RangeInclusive(10, 500) * 1000;
                            }
                            else {
                                thing.Destroy();
                            }
                            GenLeaving.DropFilthDueToDamage(thing, thing.MaxHitPoints);
                        }
                        else {
                            if (thing is Building_Bed bed) {
                                bed.SetFactionDirect(null);
                            }
                            if (Rand.Chance(decay * decay)) {
                                thing.HitPoints = (int) (thing.MaxHitPoints * (1 - Rand.Value * decay));
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Log.Warning("Failed to remove " + _currentThing + " at tile " + pos + " : " + e.Message);
                }

                if (Rand.Chance(decay)) {
                    var terrain = pos.RandomAdjacentCell8Way().ClampInsideMap(map).GetTerrain(map);
                    map.terrainGrid.SetTerrain(pos, terrain);
                }
            }
        }

        bool ShouldDestroy(Thing thing, float decay) {
            var remnantDensity = this.remnantDensity * (1 - decay);
            var isScavenger = thing is Pawn pawn && !pawn.NonHumanlikeOrWildMan();
            return thing.def.destroyable
                   && !Rand.Chance(isScavenger ? remnantDensity * scavengerDensity : remnantDensity)
                   && thing.def != ThingDefOf.Door
                   && (isScavenger
                       || (thing is Building_Turret)
                       || (thing is Plant plant && plant.IsCrop)
                       || (!thing.def.thingCategories.NullOrEmpty() && !thing.def.IsWithinCategory(ThingCategoryDefOf.Buildings) && !thing.def.IsWithinCategory(ThingCategoryDefOf.Chunks))
                       || (thing.TryGetComp<CompGlower>() != null)
                       || (thing is Building && thing.def.CanHaveFaction && Rand.Chance(thing.def == ThingDefOf.Wall ? remnantDensity * remnantDensity : decay)));
        }
    }
}