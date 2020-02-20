using System.Collections.Generic;
using RimWorld;

namespace Cities
{
	public class QuestDef : IncidentDef {
		public System.Type questClass;
		public List<QuestPart> questParts = new List<QuestPart>();

		public QuestDef() {
			workerClass = typeof(IncidentWorker_Quest);
			questClass = typeof(Quest);
			//targetTags = new List<IncidentTargetTagDef>(new[] { IncidentTargetTagDefOf.World });
		}
	}
}