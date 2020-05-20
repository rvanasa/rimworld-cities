using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class ScenPart_Allies : ScenPart {

        public override void Tick() {
            var map = Find.CurrentMap;
            if (map != null && Find.TickManager.TicksGame == 20) {
                var faction = GenCity.RandomCityFaction(f => f.GoodwillWith(Faction.OfPlayer) >= 0 && !f.def.HasRoyalTitles);
                faction.TryAffectGoodwillWith(Faction.OfPlayer, 100, false, false);

                var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                var parms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
                parms.faction = faction;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidArrivalModeForQuickMilitaryAid = true;
                parms.points = 2500;
                parms.biocodeWeaponsChance = 0;
                parms.raidNeverFleeIndividual = true;
                parms.dontUseSingleUseRocketLaunchers = true;
                IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
            }
        }
    }
}