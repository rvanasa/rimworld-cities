using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class ScenPart_Allies : ScenPart {

        public override void Tick() {
            if (Find.TickManager.TicksGame == 100) {
                var faction = GenCity.RandomCityFaction(f => f.GoodwillWith(Faction.OfPlayer) >= 0);
                faction.TryAffectGoodwillWith(Faction.OfPlayer, 100, false, false);

                var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
                parms.faction = faction;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidArrivalModeForQuickMilitaryAid = true;
                parms.points = 5000;
                parms.biocodeWeaponsChance = 0;
                parms.raidNeverFleeIndividual = true;
                parms.dontUseSingleUseRocketLaunchers = true;
                IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
            }
        }
    }
}