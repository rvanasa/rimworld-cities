using RimWorld;
using Verse;

namespace Cities {

    public class ScenPart_Elite : ScenPart {

        public override void Notify_NewPawnGenerating(Pawn pawn, PawnGenerationContext context) {
            if (context == PawnGenerationContext.PlayerStarter) {
                foreach (var skill in pawn.skills.skills) {
                    skill.Level = Rand.RangeInclusive(15, 20);
                }
            }
        }
    }
}