using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

	public class GenStep_Docks : GenStep_Scatterer {
		public IntRange size = new IntRange(5, 10);

		public override int SeedPart => GetType().Name.GetHashCode();

		protected override void ScatterAt(IntVec3 pos, Map map, int count) {
			var width = size.RandomInRange;
			var height = size.RandomInRange;
			var s = new Stencil(map);
			var x = s.RandInclusive(s.MinX, s.MaxX - width);
			var z = s.RandInclusive(s.MinZ, s.MaxZ - height);
			var sX = s.Bound(x, s.MinZ, x + width, s.MaxZ);
			var sZ = s.Bound(s.MinX, z, s.MaxX, z + height);
			foreach(var cell in sX.bounds.Cells.Concat(sZ.bounds.Cells)) {
				if(cell.GetTerrain(s.map).affordances.Contains(TerrainUtility.Bridgeable)) {
					s.map.terrainGrid.SetTerrain(cell, TerrainUtility.Bridge);
				}
			}
		}
	}
}
