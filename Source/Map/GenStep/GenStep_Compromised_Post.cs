using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public class GenStep_Compromised_Post : GenStep_Abandoned_Post {
        public override int SeedPart => GetType().Name.GetHashCode();

        public int innocentPawns = 10;

        public override void Generate(Map map, GenStepParams parms) {
            base.Generate(map, parms);

            var faction = GenCity.RandomCityFaction(f => f.PlayerGoodwill > -50);

            for (var i = 0; i < innocentPawns; i++) {
                var pawn = GenCity.SpawnInhabitant(CellRect.WholeMap(map).RandomCell, map/*, new LordJob_ExitMapBest()*/);
                pawn.SetFactionDirect(faction);
            }

            // map.weatherManager.curWeather = DefDatabase<WeatherDef>.GetNamed("Fog");
        }

        public override bool ShouldDestroy(Thing thing, float decay) {
            if (thing is Pawn pawn) {
                return pawn.IsPrisoner;
            }
            return base.ShouldDestroy(thing, decay);
        }
    }
}