using System.Linq;
using RimWorld;
using Verse;

namespace Cities
{
	public class Quest_Assault : Quest {
		Faction alliedFaction;
		City target;

		public override int MinCapableColonists => 5;

		public override LookTargets Targets => target;

		public override NamedArgument[] FormatArgs =>
			new NamedArgument[] { alliedFaction.Name, target.Faction.Name, target.Name };

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref alliedFaction, "alliedFaction");
			Scribe_References.Look(ref target, "target");
		}

		public override void ChooseParts() {
			base.ChooseParts();
			alliedFaction = Find.FactionManager.RandomAlliedFaction(minTechLevel: TechLevel.Industrial);
			target = Find.WorldObjects.Settlements
				.OfType<City>()
				.Where(s => s.Faction.HostileTo(Faction.OfPlayer) && !s.HasMap)
				.RandomByDistance(HomeMap?.Parent, 50);
		}

		public override bool AllPartsValid() {
			return base.AllPartsValid() && alliedFaction != null && target != null;
		}

		public override void OnMapGenerated(Map map) {
			if(map.Parent == target) {
				Complete();
				var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
				var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
				parms.faction = alliedFaction;
				parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
				parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
				parms.raidArrivalModeForQuickMilitaryAid = true;
				parms.points = 5000;
				IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
			}
		}

		public override void OnMapRemoved(Map map) {
			if(map.Parent == target) {
				Cancel();
			}
		}

		public override void OnComplete() {
		}

		public override void OnExpire() {
			target.SetFaction(alliedFaction);
		}
	}
}