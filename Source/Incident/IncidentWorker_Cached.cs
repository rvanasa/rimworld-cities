using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities
{
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

		public virtual void UpdateCache(IncidentParms parms) {
			var currentWorld = Find.World;
			var currentTick = Find.TickManager.TicksGame;
			if(lastTick != currentTick || lastWorld != currentWorld) {
				lastWorld = currentWorld;
				lastTick = currentTick;
				lastSuccess = CheckIncident(parms);
			}
		}

		protected override bool CanFireNowSub(IncidentParms parms) {
			if(!base.CanFireNowSub(parms)) {
				return false;
			}
			UpdateCache(parms);
			return lastSuccess;
		}

		protected override bool TryExecuteWorker(IncidentParms parms) {
			UpdateCache(parms);
			var started = StartIncident(parms);
			if(started && def.letterText != null) {
				Find.LetterStack.ReceiveLetter(MakeLetter());
			}
			return started;
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
}