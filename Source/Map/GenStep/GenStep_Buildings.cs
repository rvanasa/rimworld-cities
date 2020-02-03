using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public class GenStep_Buildings : GenStep_RectScatterer {
        public float wallChance = 0.9F;
        public float doorChance = 0.75F;
        public List<TerrainDef> floorOptions = new List<TerrainDef>();
        public List<ThingDef> wallStuffOptions = new List<ThingDef>();
        public List<BuildingDecorator> buildingDecorators = new List<BuildingDecorator>();
        public List<RoomDecorator> roomDecorators = new List<RoomDecorator>();

        public override void GenerateRect(Stencil s) {
            GenRooms(s, true);

            var stuff = RandomWallStuff(s.map);
            s.Border(ThingDefOf.Wall, stuff);

            bool hasDoor = false;
            for (var dir = 0; dir < 4; dir++) {
                if (!hasDoor || s.Chance(doorChance)) {
                    hasDoor = true;
                    var sDoor = s.Rotate(dir);
                    // var offset = sDoor.RandInclusive(0, 2) + 2;
                    var offset = sDoor.RandInclusive(1, 2);
                    var doorZ = sDoor.Chance(.5F) ? sDoor.MinZ + offset : sDoor.MaxZ - offset;
                    sDoor.Spawn(sDoor.MaxX, doorZ, ThingDefOf.Door, stuff);
                }
            }

            if (buildingDecorators.Count > 0) {
                buildingDecorators.RandomElement().Decorate(s);
            }

            s.Expand(1).BorderTerrain(GenCity.RandomFloor(s.map), IsValidTile);
        }

        void GenRooms(Stencil s, bool parentWall) {
            s = s.Center();
            var room = roomDecorators.RandomElementByWeight(r => r.weight / r.maxArea);
            if (s.Area > room.maxArea) {
                if (s.Width < s.Height) {
                    s = s.Rotate(1 + s.RandInclusive(0, 1) * 2);
                }

                var wallX = Mathf.RoundToInt(s.RandX * .3F);
                var hasWall = s.Chance(wallChance);

                if (hasWall) {
                    s.Fill(wallX, s.MinZ + 1, wallX, s.MaxZ - 1, ThingDefOf.Wall, RandomWallStuff(s.map));
                }

                var left = s.Bound(s.MinX, s.MinZ, wallX, s.MaxZ);
                var right = s.Bound(wallX, s.MinZ, s.MaxX, s.MaxZ);
                GenRooms(left, hasWall);
                GenRooms(right, hasWall);

                if (hasWall && parentWall) {
                    var offset = s.RandInclusive(0, 2) + 1;
                    s.Spawn(wallX, s.Chance(.5F) ? s.MinZ + offset : s.MaxZ - offset, ThingDefOf.Door,
                        RandomWallStuff(s.map /*, true*/));
                }
            }
            else {
                var sInterior = s.Expand(-1);
                sInterior.ClearThingsInBounds();
                s.FillTerrain(RandomFloor(s.map));

                if (room.roofed) {
                    s.FillRoof(RoofDefOf.RoofConstructed);
                }

                if (s.Chance(room.lightChance)) {
                    var sLamp = s.Expand(-1).MoveRand();
                    sLamp.Spawn(ThingDefOf.StandingLamp);
                }

                try {
                    room.Decorate(sInterior);
                }
                catch (System.Exception e) {
                    Log.Error("Error occurred in room decorator type: " + room.GetType().Name);
                    throw e;
                }
            }
        }

        TerrainDef RandomFloor(Map map) {
            return floorOptions.RandomElementWithFallback() ?? GenCity.RandomFloor(map);
        }

        ThingDef RandomWallStuff(Map map) {
            return wallStuffOptions.RandomElementWithFallback() ?? GenCity.RandomWallStuff(map);
        }
    }
}