using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace Cities {
    public class GenStep_Buildings : GenStep_RectScatterer {
        public bool expensive = false;
        public float wallChance = 0.9F;
        public float doorChance = 0.75F;
        public List<TerrainDef> floorOptions = new List<TerrainDef>();
        public List<ThingDef> wallStuffOptions = new List<ThingDef>();
        public List<BuildingDecorator> buildingDecorators = new List<BuildingDecorator>();
        public List<RoomDecorator> roomDecorators = new List<RoomDecorator>();

        public override void GenerateRect(Stencil s) {
            GenRooms(s, true);

            var wallStuff = RandomWallStuff(s.map);
            var doorStuff = RandomWallStuff(s.map);
            var hasDoor = false;
            for (var dir = 0; dir < 4; dir++) {
                var sDir = s.Rotate(dir);

                var doorHere = !hasDoor || s.Chance(doorChance);
                if (doorHere) {
                    hasDoor = true;
                    var offset = sDir.RandInclusive(0, 2) + 2;
                    var doorZ = sDir.Chance(.5F) ? sDir.MinZ + offset : sDir.MaxZ - offset;
                    for (var z = sDir.MinZ; z < sDir.MaxZ; z++) {
                        if (z != doorZ) {
                            sDir.Spawn(sDir.MaxX, z, ThingDefOf.Wall, doorStuff);
                        }
                        else {
                            sDir.Move(sDir.MaxX + 1, z).ClearBuildingsAtPos();
                            sDir.Spawn(sDir.MaxX, z, ThingDefOf.Door, wallStuff);
                        }
                    }
                }
                else {
                    sDir.Fill(sDir.MaxX, sDir.MinZ, sDir.MaxX, sDir.MaxZ - 1, ThingDefOf.Wall, wallStuff);
                }
            }

            if (buildingDecorators.Count > 0) {
                buildingDecorators.RandomElement().Decorate(s);
            }

            // s.FillFog();
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

                var doorZ = 0; // default unused
                if (hasWall) {
                    var wallStuff = RandomWallStuff(s.map);
                    if (parentWall) {
                        var offset = s.RandInclusive(0, 2) + 1;
                        doorZ = s.Chance(.5F) ? s.MinZ + offset : s.MaxZ - offset;

                        var minZ = s.MinZ + 1;
                        var maxZ = s.MaxZ - 1;
                        for (var z = minZ; z <= maxZ; z++) {
                            if (z != doorZ) {
                                s.Spawn(wallX, z, ThingDefOf.Wall, wallStuff);
                            }
                        }
                        // TODO dry
                    }
                    else {
                        s.Fill(wallX, s.MinZ + 1, wallX, s.MaxZ - 1, ThingDefOf.Wall, RandomWallStuff(s.map));
                    }
                }

                var left = s.Bound(s.MinX, s.MinZ, wallX, s.MaxZ);
                var right = s.Bound(wallX, s.MinZ, s.MaxX, s.MaxZ);
                GenRooms(left, hasWall);
                GenRooms(right, hasWall);

                if (hasWall && parentWall) {
                    s.Spawn(wallX, doorZ, ThingDefOf.Door, RandomWallStuff(s.map));
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
                catch {
                    Log.Error("Error occurred in room decorator type: " + room.GetType().Name);
                    throw;
                }
            }
        }

        TerrainDef RandomFloor(Map map) {
            return floorOptions.RandomElementWithFallback()
                   ?? (expensive ? BaseGenUtility.RandomHightechFloorDef() : GenCity.RandomFloor(map));
        }

        ThingDef RandomWallStuff(Map map) {
            return wallStuffOptions.RandomElementWithFallback()
                   ?? GenCity.RandomWallStuff(map, expensive);
        }
    }
}