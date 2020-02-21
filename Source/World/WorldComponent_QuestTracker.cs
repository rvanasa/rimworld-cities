using System.Collections.Generic;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

	public class WorldComponent_QuestTracker : WorldComponent {
		public List<Quest> quests = new List<Quest>();

		public WorldComponent_QuestTracker(World world) : base(world) {
		}

		public override void ExposeData() {
			Scribe_Collections.Look(ref quests, "quests");
		}

		public override void WorldComponentTick() {
			var tick = Find.TickManager.TicksGame;
			for(var i = quests.Count - 1; i >= 0; i--) {
				var quest = quests[i];
				if(tick >= quest.expireTime && !quest.Ended) {
					quest.Expire();
				}

				if(quest.Ended) {
					quests.RemoveAt(i);
				}
				else {
					quest.OnTick();
				}
			}
		}
	}

	/*public class WorldObjectCompProperties_Quests : WorldObjectCompProperties {
		public WorldObjectCompProperties_Quests() {
			compClass = typeof(WorldObjectComp_Quests);
		}
	}

	public class WorldObjectComp_Quests : WorldObjectComp {
	}*/
}