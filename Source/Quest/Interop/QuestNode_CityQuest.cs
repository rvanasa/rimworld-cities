using RimWorld.QuestGen;
using Verse;

namespace Cities {
    public class QuestNode_CityQuest : QuestNode {

        public SlateRef<QuestDef> questDef;

        protected override bool TestRunInt(Slate slate) {
            var quest = questDef.GetValue(slate);
            return false;
        }

        protected override void RunInt() {
            var slate = QuestGen.slate;
        }
    }
}