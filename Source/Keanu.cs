using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Keanu {
	public class IncidentWorker_WickDog : IncidentWorker {

		protected override bool CanFireNowSub(IncidentParms parms) {
			if(!base.CanFireNowSub(parms)) {
				return false;
			}
			return TryFindEntryCell(parms.target as Map, out var cell);
		}

		protected override bool TryExecuteWorker(IncidentParms parms) {
			var map = parms.target as Map;
			if(!TryFindEntryCell(map, out var cell)) {
				return false;
			}
			bool pawnMustBeCapableOfViolence = def.pawnMustBeCapableOfViolence;
			var faction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("Keanu_Faction"));
			PawnGenerationRequest request = new PawnGenerationRequest(
				def.pawnKind,
				Faction.OfPlayer,
				forceGenerateNewPawn: true,
				fixedGender: def.pawnFixedGender);
			var pawn = PawnGenerator.GeneratePawn(request);
			pawn.Name = new NameSingle("Daisy");
			GenSpawn.Spawn(pawn, cell, map);
			var text = def.letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);
			var title = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);
			Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, pawn);
			return true;
		}

		bool TryFindEntryCell(Map map, out IntVec3 cell) {
			if(map == null) {
				cell = IntVec3.Invalid;
				return false;
			}
			return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out cell);
		}
	}

	public class IncidentWorker_WickRevenge : IncidentWorker {

		protected override bool CanFireNowSub(IncidentParms parms) {
			if(!base.CanFireNowSub(parms)) {
				return false;
			}
			return TryFindEntryCell(parms.target as Map, out var cell);
		}

		protected override bool TryExecuteWorker(IncidentParms parms) {
			var map = parms.target as Map;
			if(!TryFindEntryCell(map, out var cell)) {
				return false;
			}
			var faction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("Keanu_Faction"));
			PawnGenerationRequest request = new PawnGenerationRequest(
				def.pawnKind,
				faction,
				forceGenerateNewPawn: true,
				mustBeCapableOfViolence: true,
				fixedGender: Gender.Male);
			var pawn = PawnGenerator.GeneratePawn(request);
			pawn.Name = new NameTriple("Keenes", "Wick", "Reavu");
			try {
				var weapon = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Minigun"));
				weapon.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
				pawn.equipment.DestroyAllEquipment();
				pawn.equipment.AddEquipment((ThingWithComps)weapon);
			}
			catch(System.Exception e) {
				Log.Warning("Failed to provide custom weapon for revenge event: " + e);
			}
			pawn.skills.GetSkill(SkillDefOf.Shooting).Level = 20;
			pawn.skills.GetSkill(SkillDefOf.Melee).Level = 20;
			pawn.story.melanin = 0.4F;
			pawn.story.hairColor = new UnityEngine.Color(.1F, .1F, .1F);
			GenSpawn.Spawn(pawn, cell, map);
			LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, new[] { pawn });
			var text = def.letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);
			var title = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);
			PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref title, pawn);
			Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.ThreatBig, pawn);
			return true;
		}

		bool TryFindEntryCell(Map map, out IntVec3 cell) {
			if(map == null) {
				cell = IntVec3.Invalid;
				return false;
			}
			return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out cell);
		}
	}

	public class ThingComp_WickDog : ThingComp {

		public override void PostDestroy(DestroyMode mode, Map map) {
			if(mode == DestroyMode.KillFinalize) {
				for(var i = 0; i < 100; i++) {
					if(DefDatabase<IncidentDef>.GetNamed("Keanu_WickRevenge").Worker.TryExecute(new IncidentParms {
						target = map ?? Find.Maps.Where(m => m.mapPawns.AnyFreeColonistSpawned).RandomElement() ?? Find.CurrentMap,
					})) {
						break;
					}
				}
			}
		}
	}
}