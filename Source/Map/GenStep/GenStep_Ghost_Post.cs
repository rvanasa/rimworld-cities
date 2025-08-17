using System.Linq;
using RimWorld;
using Verse;

namespace Cities {
    public class GenStep_Ghost_Post : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public override void Generate(Map map, GenStepParams parms) {
            foreach (var thing in map.spawnedThings.ToList()) {
                if ((thing is Pawn || thing is Building_Turret) && thing.Faction != null && !thing.Faction.IsPlayer) {
                    thing.Destroy();
                }
            }
            map.weatherManager.curWeather = DefDatabase<WeatherDef>.GetNamed("Fog");
        }
    }
}