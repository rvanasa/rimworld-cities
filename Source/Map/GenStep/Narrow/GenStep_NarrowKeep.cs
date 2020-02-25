using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace Cities {

    public class GenStep_NarrowKeep : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public static float HeightRatio = 1;

        public IntRange marginRange = new IntRange(4, 6);

        public GenStepDef buildingGenStepDef;

        public List<string> mechanoidNames;

        public override void Generate(Map map, GenStepParams parms) {
            var s = new Stencil(map);
            s = s.Bound(s.MinX, s.MaxZ - Mathf.RoundToInt(map.Size.x * HeightRatio), s.MaxX, s.MaxZ)
                .Center();

            // Clear area
            s.Expand(0, 8, 0, 0).ClearThingsInBounds();

            // Courtyard floor
            s.FillTerrain(BaseGenUtility.RandomHightechFloorDef());

            // Outer floor
            s.Bound(0, s.MinZ - 1, 0, s.MinZ - 8).SetTerrain(GenCity.RandomFloor(map));

            // Outer barricade
            s.Fill(s.MinX, s.MinZ - 7, s.MaxX, s.MinZ - 7, ThingDefOf.Barricade, GenCity.RandomStuff(ThingDefOf.Barricade, map));

            // Outer wall
            var wallStuff = BaseGenUtility.RandomHightechWallStuff();
            var doorX = s.MinX + map.Size.x / 2;
            s.Fill(s.MinX, s.MinZ - 3, doorX - 1, s.MinZ, ThingDefOf.Wall, wallStuff);
            s.Fill(doorX + 1, s.MinZ - 3, s.MaxX, s.MinZ, ThingDefOf.Wall, wallStuff);
            s.Bound(doorX, s.MinZ - 3, doorX, s.MinZ)
                .Fill(ThingDefOf.Door, wallStuff);

            // Inner keep
            s = s.Expand(-marginRange.RandomInRange, -marginRange.RandomInRange, -marginRange.RandomInRange, -marginRange.RandomInRange);

            var genStep = (GenStep_Buildings) buildingGenStepDef.genStep;
            genStep.GenerateRect(s);

            // Mechanoids
            var mechs = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = Faction.OfMechanoids,
                points = 10000,
            });
            foreach (var mech in mechs) {
                GenSpawn.Spawn(mech, GenCity.FindPawnSpot(s.Bound(-5, -5, 5, 5).MoveRand().pos, map), map);
                mech.SetFactionDirect(map.ParentFaction);
                mech.Name = new NameSingle(mechanoidNames[Rand.Range(0, mechanoidNames.Count)] + " #" + Rand.RangeInclusive(10, 40));
            }

            //TODO throne room   
        }

        // bool IsValidTile(Map map, IntVec3 pos) {
        //     return TerrainUtility.IsNatural(pos.GetTerrain(map));
        // }

    }

}