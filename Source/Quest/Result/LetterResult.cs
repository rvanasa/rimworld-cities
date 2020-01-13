using Verse;

namespace Cities
{
	public class LetterResult : Result {
		public string label;
		public string text;
		public LetterDef type;

		public LetterResult() {
		}

		public LetterResult(string label, string text, LetterDef type) {
			this.label = label;
			this.text = text;
			this.type = type;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref label, "label");
			Scribe_Values.Look(ref text, "text");
			Scribe_Defs.Look(ref type, "type");
		}

		public override void OnResult(Quest quest) {
			var text = this.text.Formatted(quest.FormatArgs);
			if(label != null) {
				Find.LetterStack.ReceiveLetter(label, text, type);
			}
		}
	}
}