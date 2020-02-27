using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cities {

    public class ScenPart_Equipment : ScenPart_ThingCount {

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed) {
            if (pawn.Faction?.IsPlayer ?? false) {
                var thing = ThingMaker.MakeThing(thingDef, stuff);
                if (thing is Apparel apparel) {
                    pawn.apparel.Wear(apparel, false);
                }
                else if (thing.def.equipmentType != EquipmentType.None && thing is ThingWithComps comps) {
                    pawn.equipment.AddEquipment(comps);
                }
                else if (pawn.inventory.innerContainer.TryAdd(thing, count) != count) {
                    Debug.LogWarning("Could not give equipment: " + thing);
                }
            }
        }

        public override bool AllowPlayerStartingPawn(Pawn pawn, bool tryingToRedress, PawnGenerationRequest req) {
            if (thingDef.equipmentType != EquipmentType.None) {
                return pawn.skills.GetSkill(SkillDefOf.Shooting).Level > 0
                       && pawn.skills.GetSkill(SkillDefOf.Melee).Level > 0;
            }
            return true;
        }

        public override void Randomize() {
            base.Randomize();
            count = 1;
        }
    }
}