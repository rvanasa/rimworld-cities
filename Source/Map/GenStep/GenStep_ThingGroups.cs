using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {

	public class GenStep_ThingGroups : GenStep_RectScatterer {
		public List<ThingGroup> options = new List<ThingGroup>();

		public override void GenerateRect(Stencil s) {
			options.RandomElementByWeight(g => g.weight).Generate(s);
		}

		public class ThingGroup {
			public float weight = 1;
			public bool naturalFloor = false;
			public bool roofed = false;
			public ThingDef thingDef;
			public ThingDef thingStuff;
			public int spacing = 1;
			public int spacingX;
			public int spacingZ;

			public virtual void Generate(Stencil s) {
				var spacingX = this.spacingX > 0 ? this.spacingX : spacing;
				var spacingZ = this.spacingZ > 0 ? this.spacingZ : spacing;

				s.ClearThingsInBounds();
				if(!naturalFloor) {
					s.FillTerrain(GenCity.RandomFloor(s.map));
				}
				if(roofed) {
					var wallStuff = GenCity.RandomWallStuff(s.map);
					s.Spawn(s.MinX, s.MinZ, ThingDefOf.Wall, wallStuff);
					s.Spawn(s.MaxX, s.MinZ, ThingDefOf.Wall, wallStuff);
					s.Spawn(s.MinX, s.MaxZ, ThingDefOf.Wall, wallStuff);
					s.Spawn(s.MaxX, s.MaxZ, ThingDefOf.Wall, wallStuff);
					s.FillRoof(RoofDefOf.RoofConstructed);
				}

				var stuff = thingStuff ?? GenCity.RandomStuff(thingDef, s.map);

				var paddingX = spacingX / 2 + 1;
				var paddingZ = spacingZ / 2 + 1;
				for(var x = s.MinX + paddingX; x <= s.MaxX - paddingX; x += spacingX) {
					for(var z = s.MinZ + paddingZ; z <= s.MaxZ - paddingZ; z += spacingZ) {
						s.Spawn(x, z, thingDef, stuff);
					}
				}
			}
		}
	}
}
