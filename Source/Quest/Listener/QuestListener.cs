using System.Collections.Generic;

namespace Cities
{
	public abstract class QuestListener : QuestPart {
		public List<string> events = new List<string>();

		public override void OnStart(Quest quest) {
			foreach(var result in GetResults(quest)) {
				foreach(var key in events) {
					quest.Listen(key, result);
				}
			}
		}

		public abstract IEnumerable<Result> GetResults(Quest quest);
	}
}