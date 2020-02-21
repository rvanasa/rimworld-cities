using JetBrains.Annotations;
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
                GenerateCities(config.citiesPer100kTiles.RandomInRange, false);
                GenerateCities(config.abandonedPer100kTiles.RandomInRange, true);
            }

            if (!Find.WorldObjects.AllWorldObjects.Any(obj => obj is Citadel)) {
                GenerateCity(DefDatabase<WorldObjectDef>.GetNamed("City_Citadel"), false,
                    f => f.def.CanEverBeNonHostile);
            }
        }

        public override void GenerateFromScribe(string seed) {
            GenerateFresh(seed);
        }

        void GenerateCities(int per100kTiles, bool abandoned) {
            var cityCount = Mathf.Max(1, GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100_000F * per100kTiles));
            for (var i = 0; i < cityCount; i++) {
                var def = DefDatabase<WorldObjectDef>.GetNamed(abandoned ? "City_Abandoned" : "City_Faction");
                GenerateCity(def, abandoned);
            }
        }

        void GenerateCity(WorldObjectDef def, bool abandoned, System.Predicate<Faction> factionFilter = null) {
            var city = (City) WorldObjectMaker.MakeWorldObject(def);
            city.SetFaction(GenCity.RandomCityFaction(factionFilter));
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
        }
    }
}