using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Cities {
    public class IncidentWorker_Quest : IncidentWorker_Cached {
        Quest quest;

        public QuestDef Def => (QuestDef) def;

        public override string LetterText => quest.DetailText;
        public override Faction LetterIssuer => quest.Issuer;
        public override LookTargets LetterTargets => quest.Targets;
        public override NamedArgument[] LetterParams => quest.FormatArgs;

        void SetupQuest(IncidentParms parms) {
            if (quest == null || quest.Started) {
                var questDef = Def;
                quest = (Quest) System.Activator.CreateInstance(questDef.questClass);
                quest.def = questDef;
            }
            quest.ChooseParts();
        }

        public override bool CheckIncident(IncidentParms parms) {
            if (!Config_Cities.Instance.enableQuestSystem) {
                return false;
            }
            SetupQuest(parms);
            return quest.CanReceiveRandomly();
        }

        public override bool StartIncident(IncidentParms parms) {
            if (quest == null || quest.Started) {
                SetupQuest(parms);
            }
            if (quest.AllPartsValid()) {
                quest.Start();
                return true;
            }
            return false;
        }
    }
}