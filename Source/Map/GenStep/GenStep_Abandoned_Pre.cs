using Verse;

namespace Cities {
    public class GenStep_Abandoned_Pre : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public FloatRange decay = new FloatRange(0, 1);

        public override void Generate(Map map, GenStepParams parms) {
            // TODO
        }
    }
}