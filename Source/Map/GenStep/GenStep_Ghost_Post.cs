using RimWorld;
using Verse;

namespace Cities
{
	public class GenStep_Ghost_Post : GenStep {
		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			foreach(var pos in map.AllCells) {
				var things = pos.GetThingList(map);
				for(var i = things.Count - 1; i >= 0; i--) {
					var thing = things[i];
					if((thing is Pawn || thing is Building_Turret) && thing.Faction != null && !thing.Faction.IsPlayer) {
						thing.Destroy();
					}
				}
			}
			map.weatherManager.curWeather = DefDatabase<WeatherDef>.GetNamed("Fog");
		}
	}
}