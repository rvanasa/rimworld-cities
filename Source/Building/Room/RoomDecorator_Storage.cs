using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Cities {
    public class RoomDecorator_Storage : RoomDecorator {
        public float density = 0.2F;

        public List<StockGenerator> stockGenerators = new List<StockGenerator>();
        public List<TraderKindDef> traderKinds = new List<TraderKindDef>();

        public override void Decorate(Stencil s) {
            var generators = stockGenerators.Count > 0
                ? stockGenerators
                : (traderKinds.RandomElementWithFallback()
                   ?? DefDatabase<TraderKindDef>.AllDefs.Where(t => t.stockGenerators.Count > 0).RandomElement()).stockGenerators;

            // var friendly = !cityFaction.HostileTo(Faction.OfPlayer);
            foreach (var pos in s.bounds.Cells) {
                if (s.Chance(density * Config_Cities.Instance.lootScale)) {
                    var thing = generators.RandomElement().GenerateThings(s.map.Tile).FirstOrDefault();
                    if (thing != null) {
                        GenCity.AssignQuality(thing);
                        if (thing.stackCount > thing.def.stackLimit) {
                            thing.stackCount = s.RandInclusive(1, thing.def.stackLimit);
                        }
                        thing = thing.TryMakeMinified();
                        GenSpawn.Spawn(thing, pos, s.map);
                        if (thing is Pawn pawn && pawn.Faction != null) {
                            if (pawn.guest == null) {
                                pawn.guest = new Pawn_GuestTracker(pawn);
                            }
                            if (pawn.skills == null) {
                                pawn.skills = new Pawn_SkillTracker(pawn);
                            }
                            var cityFaction = s.map.GetCityFaction();
                            if (pawn.Faction.HostileTo(cityFaction)) {
                                pawn.guest.SetGuestStatus(cityFaction, GuestStatus.Prisoner);
                            }
                        }
                        else {
                            thing.SetOwnedByCity(true, s.map);
                        }
                    }
                }
            }
        }
    }
}