using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {
    public class Quest_Assault : Quest {
        Faction alliedFaction;
        City target;

        public override int MinCapableColonists => 5;
        public override int ChallengeRating => 3;

        public override LookTargets Targets => target;

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {alliedFaction.Name, target.Faction.Name, target.Name};

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
                .Where(s => s.Faction.HostileTo(Faction.OfPlayer) && !s.HasMap && !(s is Citadel))
                .RandomByDistance(HomeMap?.Parent, 80);
        }

        public override bool AllPartsValid() {
            return base.AllPartsValid() && alliedFaction != null && target != null;
        }

        protected override void OnSetupHandle(RimWorld.Quest handle) {
            handle.AddPart(new QuestPart_CityQuest {
                factions = new[] {alliedFaction},
                targets = new GlobalTargetInfo[] {target},
            });
        }

        public override void OnTick() {
            var map = Find.CurrentMap;
            if (map != null && map.Parent == target && (Find.TickManager.TicksGame - map.generationTick + 1) % 1000 == 0) {
                Complete();
                var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
                parms.faction = alliedFaction;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidArrivalModeForQuickMilitaryAid = true;
                parms.points = 5000;
                IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);

                target.SetFaction(alliedFaction);
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == target) {
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