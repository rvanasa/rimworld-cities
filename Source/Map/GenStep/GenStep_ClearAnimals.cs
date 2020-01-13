using Verse;

namespace Cities
{
	public class GenStep_ClearAnimals : GenStep {
		public FloatRange decay = new FloatRange(0, 1);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			if(map.Parent is City city && city.Abandoned) {
				return;
			}
			foreach(var pos in map.AllCells) {
				var things = pos.GetThingList(map);
				for(var i = things.Count - 1; i >= 0; i--) {
					var thing = things[i];
					if(!TerrainUtility.IsNatural(pos.GetTerrain(map))) {
						if(thing is Pawn pawn && pawn.AnimalOrWildMan() && !pawn.IsWildMan()) {
							thing.Destroy();
						}
					}
				}
			}
		}
	}
}