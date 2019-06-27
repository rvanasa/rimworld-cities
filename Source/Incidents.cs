using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

	public abstract class IncidentWorker_Cached : IncidentWorker {
		World lastWorld;
		int lastTick;
		bool lastSuccess;

		public int MinCapableColonists => 2;

		public virtual string LetterText => def.letterText.Formatted(LetterParams);

		public virtual LookTargets LetterTargets => null;

		public virtual Faction LetterIssuer => null;

		public virtual NamedArgument[] LetterParams => new NamedArgument[0];

		public abstract bool CheckIncident(IncidentParms parms);

		public abstract bool StartIncident(IncidentParms parms);

		protected override bool CanFireNowSub(IncidentParms parms) {
			if(!base.CanFireNowSub(parms)) {
				return false;
			}
			var currentWorld = Find.World;
			var currentTick = Find.TickManager.TicksGame;
			if(lastTick != currentTick || lastWorld != currentWorld) {
				lastWorld = currentWorld;
				lastTick = currentTick;
				lastSuccess = CheckIncident(parms);
			}
			return lastSuccess;
		}

		protected override bool TryExecuteWorker(IncidentParms parms) {
			if(CanFireNowSub(parms) && StartIncident(parms)) {
				if(def.letterText != null) {
					Find.LetterStack.ReceiveLetter(MakeLetter());
				}
				return true;
			}
			return false;
		}

		public virtual Letter MakeLetter() {
			return LetterMaker.MakeLetter(def.letterLabel, LetterText, def.letterDef, LetterTargets, LetterIssuer);
		}

		public virtual void TickQuest(WorldObject obj) {
		}

		public virtual void TickQuest(Map map) {
		}

		public virtual void PostQuestMapGenerate(WorldObject obj) {
		}
	}

	public class IncidentWorker_Quest : IncidentWorker_Cached {
		Quest quest;

		public QuestDef Def => (QuestDef)def;

		public override string LetterText => quest.DetailText;

		public override Faction LetterIssuer => quest.Issuer;

		public override LookTargets LetterTargets => quest.Targets;

		public override NamedArgument[] LetterParams => quest.FormatArgs;

		public override bool CheckIncident(IncidentParms parms) {
			if(!LoadedModManager.GetMod<Mod_Cities>().GetSettings<ModSettings_Cities>().enableQuestSystem) {
				return false;
			}
			if(quest == null || quest.Started) {
				var def = Def;
				quest = (Quest)System.Activator.CreateInstance(def.questClass);
				quest.def = def;
			}
			quest.ChooseParts();
			return quest.CanReceiveRandomly();
		}

		public override bool StartIncident(IncidentParms parms) {
			quest.Start();
			return true;
		}
	}
}