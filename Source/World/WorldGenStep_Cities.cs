using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cities {
    public class WorldGenStep_Cities : WorldGenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void GenerateFresh(string seed, PlanetLayer layer) {
            var config = Config_Cities.Instance;

            if (!Find.WorldObjects.AllWorldObjects.Any(obj => obj is City)) {
                GenerateCities(layer, config.citiesPer100kTiles.RandomInRange, "City_Faction", false);
                GenerateCities(layer, config.abandonedPer100kTiles.RandomInRange, "City_Abandoned", true);
                GenerateCities(layer, config.compromisedPer100kTiles.RandomInRange, "City_Compromised", false, f => !f.def.CanEverBeNonHostile);
            }

            var missingCitadels = Config_Cities.Instance.minCitadelsPerWorld
                                  - Find.WorldObjects.AllWorldObjects.Count(obj => obj is Citadel);
            while (missingCitadels-- > 0) {
                GenerateCity(layer, "City_Citadel", false, f => f.def.CanEverBeNonHostile);
            }
        }

        public override void GenerateFromScribe(string seed, PlanetLayer layer) {
            GenerateFresh(seed, layer);
        }

        void GenerateCities(PlanetLayer layer, int per100kTiles, string defName, bool abandoned, Predicate<Faction> factionFilter = null) {
            var cityCount = Mathf.Max(1, GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100_000F * per100kTiles));
            for (var i = 0; i < cityCount; i++) {
                GenerateCity(layer, defName, abandoned, factionFilter);
            }
        }

        void GenerateCity(PlanetLayer layer, string defName, bool abandoned, Predicate<Faction> factionFilter = null) {
            try {
                var def = DefDatabase<WorldObjectDef>.GetNamed(defName);
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

                city.Tile = TileFinder.RandomSettlementTileFor(layer, city.Faction);
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