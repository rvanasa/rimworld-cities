using Verse;

namespace Cities
{
	public class GenStep_Abandoned_Pre : GenStep {
		public FloatRange decay = new FloatRange(0, 1);

		public override int SeedPart => GetType().Name.GetHashCode();

		public override void Generate(Map map, GenStepParams parms) {
			// TODO
		}
	}
}