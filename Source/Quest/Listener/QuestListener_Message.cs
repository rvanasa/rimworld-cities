using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Cities
{
	public class QuestListener_Message : QuestListener {
		public string letter;
		public string message;
		public MessageTypeDef messageType;

		public List<ThingSetMakerDef> thingSetMakerOptions = new List<ThingSetMakerDef>();

		public override IEnumerable<Result> GetResults(Quest quest) {
			yield return new MessageResult(message, messageType ?? MessageTypeDefOf.NeutralEvent);
			if(letter != null) {
				yield return new LetterResult(letter, message, DefDatabase<LetterDef>.GetNamed(messageType.defName) ?? LetterDefOf.NeutralEvent);
			}
		}
	}
}