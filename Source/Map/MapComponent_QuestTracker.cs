
using Verse;

namespace Cities {

	public class MapComponent_QuestTracker : MapComponent {
		public MapComponent_QuestTracker(Map map) : base(map) {
		}

		public override void MapGenerated() {
			var hasQuest = false;
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				quest.MapGenerated(map);
				hasQuest = true;
			}
			if(hasQuest) {
				Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
			}
		}

		public override void MapRemoved() {
			foreach(var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
				quest.MapRemoved(map);
			}
		}
	}
}