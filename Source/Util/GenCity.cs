using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public static class GenCity {

        public static Faction GetCityFaction(this Map map) {
            return (map.Parent as City)?.inhabitantFaction ?? map.ParentFaction;
        }

        public static Faction RandomCityFaction(System.Predicate<Faction> filter = null) {
            return (from x in Find.World.factionManager.AllFactionsListForReading
                    where !x.def.isPlayer && !x.def.hidden && !TechLevelUtility.IsNeolithicOrWorse(x.def.techLevel) && (filter == null || filter(x))
                    select x).RandomElementByWeightWithFallback(f => f.def.settlementGenerationWeight);
        }

        public static TerrainDef RandomFloor(Map map, bool carpets = false) {
            return BaseGenUtility.RandomBasicFloorDef(map.ParentFaction, carpets);
        }

        public static ThingDef RandomWallStuff(Map map, bool expensive = false) {
            return RandomStuff(ThingDefOf.Wall, map, expensive);
        }

        public static ThingDef RandomStuff(ThingDef thing, Map map, bool expensive = false) {
            if (!thing.MadeFromStuff) {
                return null;
            } else if (expensive) {
                return GenStuff.RandomStuffByCommonalityFor(thing, map.ParentFaction.def.techLevel);
            } else {
                return GenStuff.RandomStuffInexpensiveFor(thing, map.ParentFaction);
            }
        }

        public static void AssignQuality(Thing thing)
        {
            // The multiplicative factor between adjacent quality tiers. Each lower-quality tier is `weightMultiplier`
            // times more likely than the tier above it.
            const int weightMultiplier = 3;
            const int legendaryWeight = 1;
            const int masterworkWeight = legendaryWeight * weightMultiplier;
            const int excellentWeight = masterworkWeight * weightMultiplier;
            const int goodWeight = excellentWeight * weightMultiplier;
            const int normalWeight = goodWeight * weightMultiplier;
            const int normalThreshold = normalWeight;
            const int goodThreshold = normalThreshold + goodWeight;
            const int excellentThreshold = goodThreshold + excellentWeight;
            const int masterworkThreshold = excellentThreshold + masterworkWeight;
            const int totalWeight = masterworkThreshold + legendaryWeight;

            CompQuality compQuality = thing.TryGetComp<CompQuality>();

            if (compQuality != null) {
                QualityCategory qualityCategory;
                int randomValue = Rand.Range(0, totalWeight);

                if (randomValue < normalThreshold) {
                    qualityCategory = QualityCategory.Normal;
                } else if (randomValue < goodThreshold) {
                    qualityCategory = QualityCategory.Good;
                } else if (randomValue < excellentThreshold) {
                    qualityCategory = QualityCategory.Excellent;
                } else if (randomValue < masterworkThreshold) {
                    qualityCategory = QualityCategory.Masterwork;
                } else {
                    qualityCategory = QualityCategory.Legendary;
                }

                compQuality.SetQuality(qualityCategory, ArtGenerationContext.Outsider);
            }
        }

        public static Pawn SpawnInhabitant(IntVec3 pos, Map map, LordJob job = null, bool friendlyJob = false, bool randomWorkSpot = false, PawnKindDef kind = null) {
            var faction = map.GetCityFaction();
            if (job == null || (!friendlyJob && !faction.HostileTo(Faction.OfPlayer))) {
                var workPos = randomWorkSpot ? CellRect.WholeMap(map).RandomCell : pos;
                workPos = FindPawnSpot(workPos, map);
                job = map.Parent is Citadel
                    ? new LordJob_LiveInCitadel(workPos)
                    : map.Parent is City city && city.Abandoned
                        ? (LordJob)new LordJob_LiveInAbandonedCity(workPos)
                        : new LordJob_LiveInCity(workPos);
            }
            return SpawnInhabitant(pos, map, LordMaker.MakeNewLord(faction, job, map), kind);
        }

        public static Pawn SpawnInhabitant(IntVec3 pos, Map map, Lord lord, PawnKindDef kind = null) {
            pos = FindPawnSpot(pos, map);

            var faction = map.GetCityFaction();
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind ?? faction.RandomPawnKind(),
                faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                mustBeCapableOfViolence: true,
                forceGenerateNewPawn: true, /////
                inhabitant: true
            ));

            lord?.AddPawn(pawn);
            GenSpawn.Spawn(pawn, pos, map);

            if (pawn.guilt == null) {
                pawn.guilt = new Pawn_GuiltTracker(pawn);
            }
            pawn.guilt.Notify_Guilty(0);

            return pawn;
        }

        public static IntVec3 FindPawnSpot(IntVec3 pos, Map map) {
            var maxAttempts = 50;
            while (maxAttempts > 0 && !pos.Walkable(map)) {
                maxAttempts--;
                pos = pos.RandomAdjacentCell8Way().ClampInsideMap(map);
            }
            return pos;
        }

        public static bool IsOwnedByCity(this Thing thing, Map map = null) {
            return (map ?? thing.Map)?.GetComponent<MapComponent_City>()?.cityOwnedThings.Contains(thing) ?? false;
        }

        public static void SetOwnedByCity(this Thing thing, bool owned, Map map /* = null*/) {
            try {
                if (owned) {
                    thing.SetForbidden(true, false);
                }

                // map = map ?? thing.Map;
                if (map?.Parent is City city && !city.Abandoned && !city.Faction.HostileTo(Faction.OfPlayer)) {
                    var cityOwnedThings = map.GetComponent<MapComponent_City>().cityOwnedThings;
                    if (owned) {
                        cityOwnedThings.Add(thing);
                    } else {
                        cityOwnedThings.Remove(thing);
                    }
                }
            } catch (System.Exception e) {
                Log.Message("Failed to set city ownership [" + owned + "] on thing: " + thing + " (" + e + ")");
            }
        }
    }
}