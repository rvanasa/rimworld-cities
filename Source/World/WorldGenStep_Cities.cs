using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cities {
    public class WorldGenStep_Cities : WorldGenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void GenerateFresh(string seed) {
            var config = Config_Cities.Instance;

            if (!Find.WorldObjects.AllWorldObjects.Any(obj => obj is City)) {
                GenerateCities(config.citiesPer100kTiles.RandomInRange, "City_Faction", false);
                GenerateCities(config.abandonedPer100kTiles.RandomInRange, "City_Abandoned", true);
                GenerateCities(config.compromisedPer100kTiles.RandomInRange, "City_Compromised", false, f => !f.def.CanEverBeNonHostile);
            }

            var missingCitadels = Config_Cities.Instance.minCitadelsPerWorld
                                  - Find.WorldObjects.AllWorldObjects.Count(obj => obj is Citadel);
            while (missingCitadels-- > 0) {
                GenerateCity(DefDatabase<WorldObjectDef>.GetNamed("City_Citadel"), false, f => f.def.CanEverBeNonHostile);
            }
        }

        public override void GenerateFromScribe(string seed) {
            GenerateFresh(seed);
        }

        void GenerateCities(int per100kTiles, string defName, bool abandoned, System.Predicate<Faction> factionFilter = null) {
            var cityCount = Mathf.Max(1, GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100_000F * per100kTiles));
            for (var i = 0; i < cityCount; i++) {
                var def = DefDatabase<WorldObjectDef>.GetNamed(defName);
                GenerateCity(def, abandoned, factionFilter);
            }
        }

        void GenerateCity(WorldObjectDef def, bool abandoned, System.Predicate<Faction> factionFilter = null) {
            try {
                var faction = GenCity.RandomCityFaction(factionFilter);
                if (faction == null) {
                    Log.Warning("No suitable faction was found for city generation!");
                    return;
                }

                var city = (City)WorldObjectMaker.MakeWorldObject(def);
                city.SetFaction(faction);
                if (!abandoned) {
                    city.inhabitantFaction = city.Faction;
                }

                city.Tile = TileFinder.RandomSettlementTileFor(city.Faction);
                city.Name = city.ChooseName();
                if (!TileFinder.IsValidTileForNewSettlement(city.Tile)) {
                    // (Faction Control) ensure valid tile for existing saves
                    city.Tile = TileFinder.RandomStartingTile();
                }

                Find.WorldObjects.Add(city);
            } catch (Exception e) {
                Log.Error($"Error while generating city: {e}");
            }
        }
    }
}