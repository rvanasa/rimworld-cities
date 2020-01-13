using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
    public class WorldGenStep_Cities : WorldGenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void GenerateFresh(string seed) {
            var config = Config_Cities.Instance;
            GenerateCities(config.citiesPer100kTiles.RandomInRange, false);
            GenerateCities(config.abandonedPer100kTiles.RandomInRange, true);
        }

        public override void GenerateFromScribe(string seed) {
            if (!Find.WorldObjects.AllWorldObjects.Any(obj => obj is City)) {
                Log.Warning("No cities found; regenerating");
                GenerateFresh(seed);
            }
        }

        void GenerateCities(int per100kTiles, bool abandoned) {
            var cityCount = GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100000F * per100kTiles);
            for (var i = 0; i < cityCount; i++) {
                var city = (City) WorldObjectMaker.MakeWorldObject(
                    DefDatabase<WorldObjectDef>.GetNamed(abandoned ? "City_Abandoned" : "City_Faction"));
                city.SetFaction(GenCity.RandomCityFaction());
                if (!abandoned) {
                    city.inhabitantFaction = city.Faction;
                }

                city.Tile = TileFinder.RandomSettlementTileFor(city.Faction);
                city.Name = SettlementNameGenerator.GenerateSettlementName(city);
                if (!TileFinder.IsValidTileForNewSettlement(city.Tile)) {
                    // (Faction Control) ensure valid tile for existing saves
                    city.Tile = TileFinder.RandomStartingTile();
                }

                Find.WorldObjects.Add(city);
            }
        }
    }
}