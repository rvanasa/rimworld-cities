using RimWorld;
using Verse;

namespace Cities {
    public struct Stencil {
        public Map map;
        public IntVec3 pos;
        public Rot4 rot;
        public CellRect bounds;

        public int Width => rot.IsHorizontal ? bounds.Height : bounds.Width;
        public int Height => rot.IsHorizontal ? bounds.Width : bounds.Height;
        public int Area => bounds.Area;

        public CellRect MapBounds => CellRect.WholeMap(map);

        public int MinX => -Max(3);
        public int MinZ => -Max(2);
        public int MaxX => Max(1);
        public int MaxZ => Max(0);

        public int RandX => RandInclusive(MinX, MaxX);
        public int RandZ => RandInclusive(MinZ, MaxZ);

        public Stencil(Map map, IntVec3 pos, Rot4 rot, CellRect bounds) {
            this.map = map;
            this.pos = pos;
            this.rot = rot;
            this.bounds = bounds;
        }

        public Stencil(Map map) : this(map, map.Center, Rot4.North, CellRect.WholeMap(map)) {
        }

        public IntVec3 Coords(int x, int z) {
            return pos + rot.RighthandCell * x + rot.FacingCell * z;
        }

        public Stencil Move(int x, int z) {
            return MoveTo(Coords(x, z));
        }

        public Stencil MoveWithBounds(int x, int z) {
            int minX = MinX, minZ = MinZ, maxX = MaxX, maxZ = MaxZ;
            return Move(x, z).Bound(minX, minZ, maxX, maxZ);
        }

        public Stencil MoveTo(IntVec3 pos) {
            return new Stencil(map, pos, rot, bounds);
        }

        public Stencil ClampInsideMap() {
            return new Stencil(map, pos.ClampInsideMap(map), rot, bounds);
        }

        public Stencil MoveRand() {
            return MoveTo(bounds.RandomCell);
        }

        public Stencil Center() {
            return MoveTo(bounds.CenterCell);
        }

        public Stencil Bound(int x1, int z1, int x2, int z2) {
            return BoundTo(CellRect.FromLimits(Coords(x1, z1), Coords(x2, z2)));
        }

        public Stencil BoundTo(CellRect rect) {
            return new Stencil(map, pos, rot, rect);
        }

        public Stencil Expand(int amount) {
            return new Stencil(map, pos, rot, bounds.ExpandedBy(amount).ClipInsideMap(map));
        }

        public Stencil Expand(int minX, int minZ, int maxX, int maxZ) {
            return Bound(MinX - minX, MinZ - minZ, MaxX + maxX, MaxZ + maxZ);
        }

        private int RelativeDir(int dir) {
            return (rot.AsInt + dir) & 0b11;
        }

        public Stencil RotateTo(int dir) {
            return new Stencil(map, pos, new Rot4(dir), bounds);
        }

        public Stencil Rotate(int dir) {
            return RotateTo(RelativeDir(dir));
        }

        public Stencil Rotate(RotationDirection dir) {
            return new Stencil(map, pos, rot.Rotated(dir), bounds);
        }

        public Stencil Left() {
            return Rotate(-1);
        }

        public Stencil Right() {
            return Rotate(1);
        }

        public Stencil Back() {
            return Rotate(2);
        }

        public Stencil North() {
            return RotateTo(0);
        }

        public Stencil East() {
            return RotateTo(1);
        }

        public Stencil South() {
            return RotateTo(2);
        }

        public Stencil West() {
            return RotateTo(3);
        }

        public Stencil RotateRand() {
            return Rotate(Rand.Range(0, 4));
        }

        public int RandInclusive(int min, int max) {
            return Rand.RangeInclusive(min, max);
        }

        public bool Chance(float chance) {
            return chance > 0 && (chance >= 1 || Rand.Value < chance);
        }

        public int Max(byte dir) {
            switch (RelativeDir(dir)) {
                case 0:
                    return bounds.maxZ - pos.z;
                case 1:
                    return bounds.maxX - pos.x;
                case 2:
                    return pos.z - bounds.minZ;
                case 3:
                    return pos.x - bounds.minX;
                default:
                    return 0;
            }
        }

        public bool IsInBounds(int x, int z) {
            return bounds.Contains(Coords(x, z));
        }

        public bool IsInBounds() {
            return bounds.Contains(pos);
        }

        public bool IsInMap(int x, int z) {
            return MapBounds.Contains(Coords(x, z));
        }

        public bool IsInMap() {
            return MapBounds.Contains(pos);
        }

        public Stencil SetTerrain(TerrainDef terrain, Mask mask = null) {
            DoSetTerrain(pos, terrain, mask);
            return this;
        }

        public Stencil SetTerrain(int x, int z, TerrainDef terrain) {
            return Move(x, z).SetTerrain(terrain);
        }

        private void DoSetTerrain(IntVec3 pos, TerrainDef terrain, Mask mask) {
            if (Check(pos, mask)) {
                map.terrainGrid.SetTerrain(pos, terrain);
            }
        }

        public Stencil FillTerrain(TerrainDef terrain, Mask mask = null) {
            foreach (var pos in bounds.Cells) {
                DoSetTerrain(pos, terrain, mask);
            }

            return this;
        }

        public Stencil FillTerrain(int x1, int z1, int x2, int z2, TerrainDef terrain, Mask mask = null) {
            return this.Bound(x1, z1, x2, z2).FillTerrain(terrain, mask);
        }

        public Stencil BorderTerrain(TerrainDef terrain, Mask mask = null) {
            foreach (var pos in bounds.EdgeCells) {
                DoSetTerrain(pos, terrain, mask);
            }

            return this;
        }

        public Stencil BorderTerrain(int x1, int z1, int x2, int z2, TerrainDef terrain, Mask mask = null) {
            return this.Bound(x1, z1, x2, z2).BorderTerrain(terrain, mask);
        }

        public Stencil ClearRoof(Mask mask = null) {
            foreach (var pos in bounds.Cells) {
                if (Check(pos, mask)) {
                    map.roofGrid.SetRoof(pos, null);
                }
            }

            return this;
        }

        public Stencil FillRoof(RoofDef roof, Mask mask = null) {
            foreach (var pos in bounds.Cells) {
                if (Check(pos, mask)) {
                    map.roofGrid.SetRoof(pos, roof);
                }
            }

            return this;
        }

        public Stencil FillRoof(int x1, int z1, int x2, int z2, RoofDef roof, Mask mask = null) {
            return Bound(x1, z1, x2, z2).FillRoof(roof, mask);
        }

        // public Stencil FillFog() {
        //     var grid = map.fogGrid.fogGrid;
        //     foreach (var pos in bounds.Cells) {
        //         grid[map.cellIndices.CellToIndex(pos)] = true;
        //     }
        //     return this;
        // }

        public bool Any(Mask mask) {
            foreach (var pos in bounds.Cells) {
                if (mask(map, pos)) {
                    return true;
                }
            }

            return false;
        }

        public bool All(Mask mask) {
            foreach (var pos in bounds.Cells) {
                if (!mask(map, pos)) {
                    return false;
                }
            }

            return true;
        }

        public bool Check(Mask mask) {
            return Check(pos, mask);
        }

        public bool Check(IntVec3 pos, Mask mask = null) {
            return pos.InBounds(map) && (mask == null || mask(map, pos));
        }

        /*public bool IsEmptyInBounds<T>() where T : Thing {
            foreach(var pos in bounds.Cells) {
                if(pos.x < 0 || pos.z < 0 || pos.x >= map.Size.x || pos.z >= map.Size.z) {
                    return false;
                }
                if(pos.GetFirstThing<T>(map) != null) {
                    return false;
                }
            }
            return true;
        }*/

        public Stencil ClearThingsInBounds() {
            foreach (var pos in bounds.Cells) {
                DoClear(pos);
            }
            return this;
        }

        public Stencil ClearThingsAtPos() {
            DoClear(pos);
            return this;
        }

        public Stencil ClearBuildingsAtPos() {
            if (pos.InBounds(map)) {
                DoClearBuildings(pos);
            }
            return this;
        }

        public Stencil ClearThingsAtPos(int x, int y) {
            return Move(x, y).ClearThingsAtPos();
        }

        public Thing Spawn(int x, int z, ThingDef thing, ThingDef stuff = null) {
            return Move(x, z).ClampInsideMap().Spawn(thing, stuff);
        }

        public Thing Spawn(ThingDef thing, ThingDef stuff = null) {
            return DoSpawn(pos, thing, stuff);
        }

        Thing DoSpawn(IntVec3 pos, ThingDef thingDef, ThingDef stuff) {
            Thing thing = ThingMaker.MakeThing(thingDef, stuff);

            GenCity.AssignQuality(thing);

            return GenSpawn.Spawn(thing, pos, map, thingDef.rotatable ? rot : default(Rot4));
        }

        void DoClear(IntVec3 pos) {
            var things = pos.GetThingList(map);
            for (var num = things.Count - 1; num >= 0; num--) {
                var thing = things[num];
                if (thing.def.destroyable) {
                    thing.Destroy();
                }
            }
        }

        // TODO dry
        void DoClearBuildings(IntVec3 pos) {
            var things = pos.GetThingList(map);
            for (var num = things.Count - 1; num >= 0; num--) {
                var thing = things[num];
                if (thing.def.destroyable && thing is Building) {
                    thing.Destroy();
                }
            }
        }

        public Stencil Fill(int x1, int z1, int x2, int z2, ThingDef thing, ThingDef stuff = null, Mask mask = null) {
            return Bound(x1, z1, x2, z2).Fill(thing, stuff);
        }

        public Stencil Fill(ThingDef thing, ThingDef stuff = null, Mask mask = null) {
            foreach (var pos in bounds.Cells) {
                if (Check(pos, mask)) {
                    DoSpawn(pos, thing, stuff);
                }
            }

            return this;
        }

        // public Stencil Replace(int x1, int z1, int x2, int z2, ThingDef thing, ThingDef stuff = null,
        //     Mask mask = null) {
        //     return Bound(x1, z1, x2, z2).Replace(thing, stuff);
        // }
        //
        // public Stencil Replace(ThingDef thing, ThingDef stuff = null, Mask mask = null) {
        //     foreach (var pos in bounds.Cells) {
        //         if (Check(pos, mask)) {
        //             DoSpawn(pos, thing, stuff);
        //             DoClear(pos);
        //         }
        //     }
        //
        //     return this;
        // }

        public Stencil Border(int x1, int z1, int x2, int z2, ThingDef thing, ThingDef stuff = null, Mask mask = null) {
            return Bound(x1, z1, x2, z2).Border(thing, stuff, mask);
        }

        public Stencil Border(ThingDef thing, ThingDef stuff = null, Mask mask = null) {
            foreach (var pos in bounds.EdgeCells) {
                if (Check(pos, mask)) {
                    DoSpawn(pos, thing, stuff);
                }
            }

            return this;
        }

        public Stencil ExpandRegion(Mask isValid, float maxArea = float.PositiveInfinity) {
            var dists = new int[4];
            var flags = 0b1111;
            var area = 1;
            var dir = 0;
            while (area < maxArea && flags != 0) {
                var dist = dists[dir];
                var mask = 1 << dir;
                if ((flags & mask) != 0) {
                    var sCurrent = Rotate(dir).Bound(-dists[(dir + 3) % 4], dist + 1, dists[(dir + 1) % 4], dist + 1);
                    var clippedBounds = sCurrent.bounds;
                    clippedBounds.ClipInsideMap(map);
                    if (sCurrent.bounds == clippedBounds && sCurrent.All(isValid)) {
                        dists[dir]++;
                    }
                    else {
                        flags &= ~mask;
                    }
                }

                area = (dists[0] + dists[2]) * (dists[1] + dists[3]);
                dir = (dir + 1) % 4;
            }

            return Bound(-dists[3], -dists[2], dists[1], dists[0]);
        }

        public delegate bool Mask(Map map, IntVec3 pos);
    }
}