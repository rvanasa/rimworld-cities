using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Cities {

	public abstract class BuildingDecorator : Decorator {
	}

	public class BuildingDecorator_None : BuildingDecorator {
		public override void Decorate(Stencil s) {
		}
	}

	public class BuildingDecorator_Sandbags : BuildingDecorator {
		public int distance = 2;

		public override void Decorate(Stencil s) {
			Stencil.Mask mask = p => TerrainUtility.IsNatural(p.GetTerrain(s.map));

			for(var dir = 0; dir < 4; dir++) {
				var sDir = s.Rotate(dir);
				sDir.Bound(sDir.MinX - distance, sDir.MaxZ + distance, sDir.MinX / 2, sDir.MaxZ + distance)
					.Fill(ThingDefOf.Sandbags, mask: mask);
				sDir.Bound(sDir.MaxX / 2, sDir.MaxZ + distance, sDir.MaxX + distance, sDir.MaxZ + distance)
					.Fill(ThingDefOf.Sandbags, mask: mask);
			}
		}
	}
}