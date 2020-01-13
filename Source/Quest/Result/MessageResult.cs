using Verse;

namespace Cities
{
	public class MessageResult : Result {
		public string message;
		public MessageTypeDef type;

		public MessageResult() {
		}

		public MessageResult(string message, MessageTypeDef type) {
			this.message = message;
			this.type = type;
		}

		public override void ExposeData() {
			Scribe_Values.Look(ref message, "message");
			Scribe_Defs.Look(ref type, "type");
		}

		public override void OnResult(Quest quest) {
			var text = message.Formatted(quest.FormatArgs);
			Messages.Message(text, type);
		}
	}
}