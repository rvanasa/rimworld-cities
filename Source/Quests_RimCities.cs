using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace Cities {

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
				.Where(s => s is City city && city.Visitable && !city.Abandoned
				&& QuestUtility.Reachable(HomeMap?.Parent, s, 50)
				&& !s.HasMap)
				.RandomElementWithFallback() as City;
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

	public class Quest_Assault : Quest {
		Faction alliedFaction;
		Settlement target;

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
			//alliedFaction = Find.FactionManager.RandomNonHostileFaction(minTechLevel: TechLevel.Industrial);
			target = Find.WorldObjects.Settlements
				.Where(s => s.Faction.HostileTo(Faction.OfPlayer)
				&& QuestUtility.Reachable(HomeMap?.Parent, s, 50)
				&& !s.HasMap)
				.RandomElementWithFallback();
		}

		public override bool AllPartsValid() {
			return base.AllPartsValid() && alliedFaction != null && target != null;
		}

		public override void OnMapGenerated(Map map) {
			if(map.Parent == target) {
				var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
				var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
				parms.faction = alliedFaction;
				parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
				parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetRandom();
				parms.raidArrivalModeForQuickMilitaryAid = true;
				parms.points = 4000;
				IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
			}
		}

		public override void OnMapRemoved(Map map) {
			if(map.Parent == target) {
				Cancel();
			}
		}

		public override void OnTick() {
			if(AtInterval(100)) {
				if(Find.WorldObjects.AnyDestroyedSettlementAt(target.Tile)) {
					Complete();
				}
			}
		}

		public override void OnComplete() {
			target.SetFaction(Faction.OfPlayer);
		}

		public override void OnExpire() {
			target.SetFaction(alliedFaction);
		}
	}

	public class Quest_Assassinate : Quest {
		City city;
		Pawn pawn;

		public override LookTargets Targets => city;

		public override NamedArgument[] FormatArgs =>
			new NamedArgument[] { pawn.Name.ToStringFull, pawn.Name.ToStringShort, city.Faction.Name, city.Name };

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref city, "city");
			Scribe_References.Look(ref pawn, "pawn");
		}

		public override void ChooseParts() {
			base.ChooseParts();
			city = Find.WorldObjects.Settlements
				.Where(s => s is City city && city.Visitable && !city.Abandoned
				&& QuestUtility.Reachable(HomeMap?.Parent, s, 50)
				&& !s.HasMap)
				.RandomElementWithFallback() as City;
			if(city == null) {
				return;
			}
			var faction = city.Faction;
			pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(faction.RandomPawnKind(), faction, PawnGenerationContext.NonPlayer, city.Tile));
			Find.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
		}

		public override bool AllPartsValid() {
			return base.AllPartsValid() && city != null && pawn != null;
		}

		public override void OnMapGenerated(Map map) {
			if(map.Parent == city) {
				var lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_LiveInCity(), map, new[] { pawn });
				GenSpawn.Spawn(pawn, CellRect.WholeMap(map).ExpandedBy(-20).RandomCell, map);
				var bed = map.listerThings.AllThings.OfType<Building_Bed>().Where(b => !b.ForPrisoners && !b.Medical).RandomElementWithFallback();
				if(bed != null) {
					bed.SetFactionDirect(pawn.Faction);
					bed.TryAssignPawn(pawn);
				}
			}
		}

		public override void OnMapRemoved(Map map) {
			if(map.Parent == city) {
				Cancel();
			}
		}

		public override void OnTick() {
			if(AtInterval(100)) {
				if(pawn.Dead) {
					Complete();
				}
				else if(city.HasMap && !pawn.SpawnedOrAnyParentSpawned) {
					Cancel();
				}
			}
		}

		public override void OnEnd() {
			Find.World.worldPawns.ForcefullyKeptPawns.Remove(pawn);
		}
	}

	public class Quest_Defend : Quest {
		const int MaxStageCount = 8;
		const int CheckInterval = 1000;
		//static readonly IntRange StageInterval = new IntRange(5_000, 10_000);
		static readonly IntRange StageInterval = new IntRange(1_000, 3_000);

		Faction enemyFaction;
		City city;
		int stage;
		int ticksTillNextStage = Rand.RangeInclusive(3_000, 5_000);

		public override int MinCapableColonists => 5;

		public override LookTargets Targets => city;

		public override NamedArgument[] FormatArgs =>
			new NamedArgument[] { enemyFaction.Name, city.Faction.Name, city.Name };

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref enemyFaction, "enemyFaction");
			Scribe_References.Look(ref city, "city");
			Scribe_Values.Look(ref stage, "stage");
			Scribe_Values.Look(ref ticksTillNextStage, "ticksTillNextStage");
		}

		public override void ChooseParts() {
			base.ChooseParts();
			enemyFaction = Find.FactionManager.RandomEnemyFaction();
			city = Find.WorldObjects.Settlements
				.Where(s => s is City city && city.Visitable && !city.Abandoned
				&& QuestUtility.Reachable(HomeMap?.Parent, s, 50)
				&& !s.HasMap)
				.RandomElementWithFallback() as City;
		}

		public override bool AllPartsValid() {
			return base.AllPartsValid() && enemyFaction != null && city != null;
		}

		public override void OnMapGenerated(Map map) {
			if(map.Parent == city) {
				map.GetComponent<MapComponent_City>().cityOwnedThings.Clear();
				var playerFaction = Faction.OfPlayer;
				foreach(var thing in map.listerThings.AllThings) {
					if(!(thing is Pawn) && thing.def.CanHaveFaction) {
						thing.SetFactionDirect(playerFaction);
					}
				}
			}
		}

		public override void OnMapRemoved(Map map) {
			if(map.Parent == city) {
				Cancel();
			}
		}

		public override void OnTick() {
			if(AtInterval(CheckInterval)) {
				var map = city.Map;
				if(map != null) {
					ticksTillNextStage -= CheckInterval;
					if(ticksTillNextStage <= 0) {
						if(stage < MaxStageCount) {
							stage++;
							ticksTillNextStage += StageInterval.RandomInRange;
							city.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 100);
							var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
							var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
							parms.faction = enemyFaction;
							parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
							parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetRandom();
							parms.points += Mathf.RoundToInt((5 + stage) * 1000);
							IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);

							Messages.Message("QuestDefendWave".Translate().Formatted(stage, MaxStageCount), MessageTypeDefOf.ThreatBig);
						}
						else {
							//var hasEnemy = false;
							//var playerFaction = Faction.OfPlayer;
							//foreach(var pawn in map.mapPawns.AllPawnsSpawned) {
							//	if(!pawn.IsPrisoner && pawn.Faction.HostileTo(playerFaction)) {
							//		hasEnemy = true;
							//		break;
							//	}
							//}
							//if(!hasEnemy) {
								Complete();
							//}
						}
					}
				}
			}
		}

		public override void OnComplete() {
			var map = city.Map;
			if(map == null) {
				return;
			}
			var faction = map.ParentFaction;
			var playerFaction = Faction.OfPlayer;
			map.Parent.SetFaction(playerFaction);
			faction.TryAffectGoodwillWith(playerFaction, 200);
			foreach(var pawn in map.mapPawns.AllPawns) {
				if(pawn.Faction == faction) {
					pawn.SetFactionDirect(playerFaction);
				}
			}
		}

		public override void OnCancel() {
			OnExpire();
		}

		public override void OnExpire() {
			city.SetFaction(enemyFaction);
		}
	}

	public class Quest_PrisonBreak : Quest {
		//const int MaxPrisoners = 5;

		City city;

		public override int MinCapableColonists => 1;

		public override LookTargets Targets => city;

		public override NamedArgument[] FormatArgs =>
			new NamedArgument[] { city.Faction.Name, city.Name };

		public override void ExposeData() {
			base.ExposeData();
			Scribe_References.Look(ref city, "city");
		}

		public override void ChooseParts() {
			base.ChooseParts();
			city = Find.WorldObjects.Settlements
				.Where(s => s is City city && city.Visitable && !city.Abandoned
				&& QuestUtility.Reachable(HomeMap?.Parent, s, 50)
				&& !s.HasMap)
				.RandomElementWithFallback() as City;
		}

		public override bool AllPartsValid() {
			return base.AllPartsValid() && city != null;
		}

		public override void OnMapGenerated(Map map) {
			if(map.Parent == city) {
				var playerFaction = Faction.OfPlayer;
				var count = 0;
				foreach(var pawn in map.mapPawns.AllPawnsSpawned) {
					if(pawn.IsPrisoner) {
						count++;
						pawn.mindState.WillJoinColonyIfRescued = true;
						//pawn.guest = null;
						//if(count < MaxPrisoners) {
						//	pawn.SetFactionDirect(playerFaction);
						//}
					}
				}
				if(count > 0) {
					Complete();
				}
				else {
					Cancel();
				}
			}
		}

		public override void OnMapRemoved(Map map) {
			if(map.Parent == city) {
				Cancel();
			}
		}

		public override void OnComplete() {
			city.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -200);
		}
	}
}