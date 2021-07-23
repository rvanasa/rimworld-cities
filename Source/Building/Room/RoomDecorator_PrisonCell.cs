using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Cities {
    public class RoomDecorator_PrisonCell : RoomDecorator {
        public List<ThingDef> bedOptions = new List<ThingDef>();
        public float prisonerChance = 0.25F;

        public RoomDecorator_PrisonCell() {
            lightChance = 0.2F;
        }

        public override void Decorate(Stencil s) {
            var sBed = s.Expand(-1);
            var thing = bedOptions.RandomElement();
            var stuff = GenCity.RandomStuff(thing, s.map);
            var bed = (Building_Bed) sBed.Spawn(sBed.RandX, sBed.RandZ, thing, stuff);
            bed.SetFactionDirect(s.map.ParentFaction);
            bed.ForPrisoners = true;
            if (s.Chance(prisonerChance)) {
                var pawn = GenCity.SpawnInhabitant(s.pos, s.map, kind: PawnKindDefOf.Slave);
                if (pawn.guest == null) {
                    pawn.guest = new Pawn_GuestTracker(pawn);
                }
                if (pawn.skills == null) {
                    pawn.skills = new Pawn_SkillTracker(pawn);
                }
                if (pawn.Faction.HostileTo(s.map.ParentFaction)) {
                    pawn.equipment.DestroyAllEquipment();
                    pawn.guest.SetGuestStatus(s.map.ParentFaction, GuestStatus.Prisoner);
                }
            }
        }
    }
}