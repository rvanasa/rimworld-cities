using System.Linq;
using RimWorld;
using Verse;

namespace Cities {
    public class Quest_PrisonBreak : Quest {
        //const int MaxPrisoners = 5;

        City city;

        public override int MinCapableColonists => 1;

        public override LookTargets Targets => city;

        public override NamedArgument[] FormatArgs =>
            new NamedArgument[] {city.Faction.Name, city.Name};

        public override void ExposeData() {
            base.ExposeData();
            Scribe_References.Look(ref city, "city");
        }

        public override void ChooseParts() {
            base.ChooseParts();
            city = Find.WorldObjects.Settlements
                .Where(s => s is City city && city.Visitable && !city.Abandoned && !(s is Citadel)
                            && QuestUtility.Reachable(HomeMap?.Parent, s, 80)
                            && !s.HasMap)
                .RandomElementWithFallback() as City;
        }

        public override bool AllPartsValid() {
            return base.AllPartsValid() && city != null;
        }

        public override void OnMapGenerated(Map map) {
            if (map.Parent == city) {
                var count = 0;
                foreach (var pawn in map.mapPawns.AllPawnsSpawned) {
                    if (pawn.IsPrisoner) {
                        count++;
                        pawn.mindState.WillJoinColonyIfRescued = true;
                        //pawn.guest = null;
                        //if(count < MaxPrisoners) {
                        //	pawn.SetFactionDirect(Faction.OfPlayer);
                        //}
                    }
                }

                if (count > 0) {
                    Complete();
                }
                else {
                    Cancel();
                }
            }
        }

        public override void OnMapRemoved(Map map) {
            if (map.Parent == city) {
                Cancel();
            }
        }

        public override void OnComplete() {
            city.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -200);
        }
    }
}