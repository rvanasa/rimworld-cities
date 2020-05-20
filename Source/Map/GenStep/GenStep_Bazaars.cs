using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

    public class GenStep_Bazaars : GenStep_RectScatterer {

        public float standChance = 0.4F;

        public override void GenerateRect(Stencil s) {
            s.FillTerrain(GenCity.RandomFloor(s.map));
            var pawn = (Pawn) null;
            if (!s.map.ParentFaction.HostileTo(Faction.OfPlayer)) {
                pawn = GenCity.SpawnInhabitant(s.Coords(s.RandX / 2, s.RandZ / 2), s.map);
                var traderKind = DefDatabase<TraderKindDef>.AllDefs.RandomElement();
                pawn.mindState.wantsToTradeWithColony = true;
                PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
                pawn.trader.traderKind = traderKind;
                pawn.inventory.DestroyAll();
                var parms = new ThingSetMakerParams {
                    traderDef = traderKind,
                    tile = s.map.Tile,
                    makingFaction = s.map.ParentFaction,
                };
                foreach (var item in ThingSetMakerDefOf.TraderStock.root.Generate(parms)) {
                    if (!(item is Pawn) && !pawn.inventory.innerContainer.TryAdd(item)) {
                        item.Destroy();
                    }
                }

                PawnInventoryGenerator.GiveRandomFood(pawn);
            }

            for (var dir = 0; dir < 4; dir++) {
                if (s.Chance(standChance)) {
                    var sStand = s.Rotate(dir).Move(Mathf.RoundToInt(s.RandX / 2F), s.MaxZ - s.RandInclusive(0, 2)).Bound(-1, 0, 1, 0);
                    sStand.FillTerrain(GenCity.RandomFloor(s.map, true));
                    var wallStuff = GenCity.RandomWallStuff(s.map);
                    sStand.Spawn(sStand.MinX - 1, 0, ThingDefOf.Wall, wallStuff);
                    sStand.Spawn(sStand.MaxX + 1, 0, ThingDefOf.Wall, wallStuff);
                    sStand.Expand(1).FillRoof(RoofDefOf.RoofConstructed);
                    // if (pawn != null) {
                    //     var itemPos = sStand.Coords(sStand.RandX, sStand.RandZ);
                    //     var item = pawn.inventory.innerContainer.FirstOrDefault();
                    //     if (item != null) {
                    //         pawn.inventory.innerContainer.TryDrop(item, itemPos, s.map, ThingPlaceMode.Direct, out var result);
                    //         item.SetOwnedByCity(true, s.map);
                    //     }
                    // }
                }
            }
        }
    }
}