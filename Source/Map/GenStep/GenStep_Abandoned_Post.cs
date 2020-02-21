using RimWorld;
using Verse;

namespace Cities {
    public class GenStep_Abandoned_Post : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public FloatRange decay = new FloatRange(0, 1);
        public FloatRange corpseChance = new FloatRange(0, 0.3F);
        public float remnantDensity = 0.15F;
        public float scavengerDensity = 0.5F;

        public override void Generate(Map map, GenStepParams parms) {
            var playerFaction = Faction.OfPlayer;
            var decay = this.decay.RandomInRange;
            var corpseChance = this.corpseChance.RandomInRange;
            var autoClaim = false;
            foreach (var pos in map.cellsInRandomOrder.GetAll()) {
                var things = pos.GetThingList(map);
                for (var i = things.Count - 1; i >= 0; i--) {
                    var thing = things[i];
                    if (ShouldDestroy(thing, decay) && !thing.Destroyed) {
                        if (thing is Pawn pawn && !pawn.Faction.IsPlayer && Rand.Chance(corpseChance)) {
                            pawn.Kill(null);
                            pawn.Corpse.timeOfDeath -= Rand.RangeInclusive(10, 500) * 1000;
                        }
                        else {
                            thing.Destroy();
                        }
                    }
                    else if (autoClaim && !(thing is Pawn) && thing.def.CanHaveFaction && thing.Faction == null) {
                        thing.SetFactionDirect(playerFaction);
                    }
                    else if (!autoClaim && thing is Building_Bed bed) {
                        bed.SetFactionDirect(null);
                    }
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