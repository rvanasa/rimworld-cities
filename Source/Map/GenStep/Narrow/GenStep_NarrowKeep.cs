using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace Cities {

    public class GenStep_NarrowKeep : GenStep {
        public override int SeedPart => GetType().Name.GetHashCode();

        public float heightRatio = 1;

        public IntRange marginRange = new IntRange(4, 6);

        public GenStepDef buildingGenStepDef;

        public override void Generate(Map map, GenStepParams parms) {
            var s = new Stencil(map);
            s = s.Bound(s.MinX, s.MaxZ - Mathf.RoundToInt(map.Size.x * heightRatio), s.MaxX, s.MaxZ)
                .ClearThingsInBounds()
                .Center();
            
            // Courtyard floor
            s.FillTerrain(BaseGenUtility.RandomHightechFloorDef());

            // Outer barricade
            s.Fill(s.MinX, s.MinZ - 6, s.MaxX, s.MinZ - 6, ThingDefOf.Barricade, GenCity.RandomStuff(ThingDefOf.Barricade, map), IsValidBarricadeTile);

            // Outer wall
            var wallStuff = BaseGenUtility.RandomHightechWallStuff();
            s.Fill(s.MinX, s.MinZ - 3, s.MaxX, s.MinZ, ThingDefOf.Wall, wallStuff);
            
            // Outer door
            var doorX = s.RandX;
            s.Bound(doorX, s.MinZ - 3, doorX, s.MinZ)
                .Fill(ThingDefOf.Door, wallStuff);

            // Inner keep
            s = s.Expand(-marginRange.RandomInRange, -marginRange.RandomInRange, -marginRange.RandomInRange, -marginRange.RandomInRange);

            var genStep = (GenStep_Buildings) buildingGenStepDef.genStep;
            genStep.GenerateRect(s);
            
            if (map.ParentFaction?.GoodwillWith(Faction.OfPlayer) < 0) {
                
                // Mechanoids
                var mechs = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms {
                    groupKind = PawnGroupKindDefOf.Combat,
                    tile = map.Tile,
                    faction = Faction.OfMechanoids,
                    points = 10000,
                });
                foreach (var mech in mechs) {
                    GenSpawn.Spawn(mech, GenCity.FindPawnSpot(s.MoveRand().pos, map), map);
                    mech.SetFactionDirect(map.ParentFaction);
                }
            }

            //TODO throne room   
        }

        bool IsValidBarricadeTile(Map map, IntVec3 pos) {
            return TerrainUtility.IsNaturalExcludingRock(pos.GetTerrain(map));
        }
    }
}