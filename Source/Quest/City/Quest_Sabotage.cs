using System.Linq;
using RimWorld;
using Verse;

namespace Cities
{
	public class Quest_Sabotage : Quest {
		City target;

		public override int MinCapableColonists => 3;

		public override LookTargets Targets => target;

		public override NamedArgument[] FormatArgs =>
			new NamedArgument[] { target.Faction.Name, target.Name };

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref target, "target");
		}

		public override void ChooseParts() {
			base.ChooseParts();
			target = Find.WorldObjects.Settlements
				.OfType<City>()
				.Where(s =>  s.Visitable && !s.Abandoned && !s.HasMap)
				.RandomByDistance(HomeMap?.Parent, 50);
		}

		public override bool AllPartsValid() {
			return base.AllPartsValid() && target != null;
		}

		public override void OnMapGenerated(Map map) {
			if(map.Parent == target) {
				var playerFaction = Faction.OfPlayer;
				foreach(var thing in map.listerThings.AllThings) {
					if(IsDefense(thing)) {
						thing.SetFactionDirect(playerFaction);
						//map.designationManager.AddDesignation(new Designation(thing, DesignationDefOf.Deconstruct));
					}
				}
			}
		}

		public override void OnMapRemoved(Map map) {
			if(map.Parent == target) {
				Cancel();
			}
		}

		public override void OnTick() {
			if(AtInterval(200)) {
				var map = target.Map;
				if(map == null) {
					return;
				}
				var hasDefenses = false;
				foreach(var thing in map.listerThings.AllThings) {
					if(IsDefense(thing)) {
						hasDefenses = true;
						Log.Warning("Building remaining: " + thing + " :: " + thing.Position, false);
						break;
					}
				}
				if(!hasDefenses) {
					Complete();
				}
			}
		}

		public override void OnComplete() {
			var faction = Find.FactionManager.RandomNonHostileFaction(minTechLevel: TechLevel.Industrial);
			if(faction == null) {
				faction = FactionGenerator.NewGeneratedFaction();
			}
			faction.TryAffectGoodwillWith(Faction.OfPlayer, 50);
			target.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -50);
			faction.TryAffectGoodwillWith(target.Faction, -200);
			target.SetFaction(faction);

			var map = target.Map;
			if(map == null) {
				return;
			}
			var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
			var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
			parms.faction = faction;
			parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
			parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
			parms.raidArrivalModeForQuickMilitaryAid = true;
			parms.points = 4000;
			IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
		}

		bool IsDefense(Thing thing) {
			return thing is Building_Turret && thing.def.CanHaveFaction;
		}
	}
}