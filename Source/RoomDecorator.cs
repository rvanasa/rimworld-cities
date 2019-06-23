using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Cities {

	public abstract class RoomDecorator : Decorator {
		public bool roofed = true;
		public float lightChance = 0.9F;
		public int maxArea = 100;
	}

	public class RoomDecorator_Storage : RoomDecorator {
		public float density = 0.2F;

		public List<TraderKindDef> traderKinds = new List<TraderKindDef>();
		public List<StockGenerator> stockGenerators = new List<StockGenerator>();

		public override void Decorate(Stencil s) {
			var generators = stockGenerators.Count > 0 ? stockGenerators :
				(traderKinds.RandomElementWithFallback()
				?? DefDatabase<TraderKindDef>.AllDefs.Where(t => t.stockGenerators.Count > 0).RandomElement()).stockGenerators;

			var friendly = !s.map.ParentFaction.HostileTo(Faction.OfPlayer);
			foreach(IntVec3 pos in s.bounds.Cells) {
				if(s.Chance(density)) {
					var thing = generators.RandomElement().GenerateThings(s.map.Tile).FirstOrDefault();
					if(thing != null) {
						if(thing.stackCount > thing.def.stackLimit) {
							thing.stackCount = s.RandInclusive(1, thing.def.stackLimit);
						}
						GenSpawn.Spawn(thing, pos, s.map);
						if(thing is Pawn pawn) {
							if(pawn.guest == null) {
								pawn.guest = new Pawn_GuestTracker(pawn);
							}
							pawn.guest.SetGuestStatus(s.map.ParentFaction, true);
						}
						else {
							thing.SetOwnedByCity(true);
						}
					}
				}
			}
		}
	}

	public class RoomDecorator_FrozenStorage : RoomDecorator_Storage {

		public override void Decorate(Stencil s) {
			base.Decorate(s);

			var sVent = s.RotateRand().Move(0, s.MaxZ - 1).Bound(-1, -1, 1, 1);
			sVent.Border(ThingDefOf.Wall, GenCity.RandomWallStuff(s.map, true));
			sVent.Bound(0, 0, 0, 0).ClearRoof();
			var tempControl = sVent.Spawn(0, -1, ThingDefOf.Cooler).TryGetComp<CompTempControl>();
			tempControl.targetTemperature = -1;
		}
	}

	public class RoomDecorator_Centerpiece : RoomDecorator {
		public List<ThingDef> options = new List<ThingDef>();
		public List<ThingDef> chairOptions = new List<ThingDef>();
		public float chairDensity;
		public float chairPawnChance = 0.1F;

		public override void Decorate(Stencil s) {
			s.Bound(s.RandInclusive(s.MinX, s.MinX / 2) - 1, s.RandInclusive(s.MinZ, s.MinZ / 2) - 1, s.RandInclusive(s.MaxX / 2, s.MaxX) + 1, s.RandInclusive(s.MaxZ / 2, s.MaxZ) + 1)
				.FillTerrain(GenCity.RandomFloor(s.map, true));

			var def = options.RandomElement();
			var thing = s.Spawn(def, GenCity.RandomStuff(def, s.map));
			if(chairDensity > 0) {
				var chairDef = chairOptions.RandomElement();
				var chairStuff = GenCity.RandomStuff(chairDef, s.map);
				var sThing = s.BoundTo(thing.OccupiedRect());
				for(var dir = 0; dir < 4; dir++) {
					var sDir = sThing.Rotate(dir);
					for(var x = sDir.MinX; x <= sDir.MaxX; x++) {
						if(s.Chance(chairDensity)) {
							SpawnChair(sDir.Move(x, sDir.MinZ - 1), chairDef, chairStuff);
						}
					}
				}
			}
			else if(thing.def.hasInteractionCell && chairOptions.Count > 0) {
				var chairDef = chairOptions.RandomElement();
				var chairStuff = GenCity.RandomStuff(chairDef, s.map);
				SpawnChair(s.MoveTo(thing.InteractionCell), chairDef, chairStuff);
			}
		}

		private void SpawnChair(Stencil s, ThingDef thing, ThingDef stuff) {
			s.Spawn(thing, stuff);
			if(s.Chance(chairPawnChance)) {
				GenCity.SpawnInhabitant(s.pos, s.map);
			}
		}
	}

	public class RoomDecorator_Batteries : RoomDecorator {
		public FloatRange charge = new FloatRange(0.5F, 1);

		public override void Decorate(Stencil s) {
			var charge = this.charge.RandomInRange;
			for(int x = s.MinX + 1; x <= s.MaxX - 1; x++) {
				var batteryComp = s.Spawn(x, 0, ThingDefOf.Battery).TryGetComp<CompPowerBattery>();
				if(batteryComp != null) {
					batteryComp.SetStoredEnergyPct(charge);
				}
			}
		}
	}

	public class RoomDecorator_Bedroom : RoomDecorator {
		public float headTableChance = 0.9F;
		public float dresserChance = 0.9F;
		public float pawnInBedroomChance = 0.2F;

		public RoomDecorator_Bedroom() {
			lightChance = 0;
		}

		public override void Decorate(Stencil s) {
			var stuff = GenCity.RandomStuff(ThingDefOf.Bed, s.map);
			var sDresser = s.Rotate(s.RandInclusive(0, 1) * 2);
			sDresser.Spawn(s.RandInclusive(sDresser.MinX + 1, sDresser.MaxX - 1), sDresser.MinZ + 1, DefDatabase<ThingDef>.GetNamed("Dresser"), stuff);
			var bedX = s.RandX;
			s.Spawn(bedX, s.MinZ, DefDatabase<ThingDef>.GetNamed("EndTable"), stuff);
			var bed = (Building_Bed)s.Spawn(bedX, s.MinZ + 1, ThingDefOf.Bed, stuff);
			bed.SetFactionDirect(s.map.ParentFaction);
			var pawn = GenCity.SpawnInhabitant(s.Chance(pawnInBedroomChance) ? s.pos : s.MapBounds.RandomCell, s.map, null, randomWorkSpot: true);
			bed.TryAssignPawn(pawn);
		}
	}

	public class RoomDecorator_PrisonCell : RoomDecorator {
		public List<ThingDef> bedOptions = new List<ThingDef>();
		public float prisonerChance = 0.25F;

		public RoomDecorator_PrisonCell() {
			lightChance = 0.2F;
		}

		public override void Decorate(Stencil s) {
			var sBed = s.Expand(-1);
			var thing = bedOptions.RandomElement();
			var stuff = GenCity.RandomStuff(thing, s.map);
			var bed = (Building_Bed)sBed.Spawn(sBed.RandX, sBed.RandZ, thing, stuff);
			bed.ForPrisoners = true;
			if(s.Chance(prisonerChance)) {
				var pawn = GenCity.SpawnInhabitant(s.pos, s.map);
				pawn.guest.SetGuestStatus(s.map.ParentFaction, true);
				bed.TryAssignPawn(pawn);
			}
		}
	}

	public class RoomDecorator_HospitalBed : RoomDecorator {
		public RoomDecorator_HospitalBed() {
			lightChance = 1;
		}

		public override void Decorate(Stencil s) {
			var sBed = s.Expand(-1);
			var thing = ThingDefOf.Bed;
			var stuff = GenCity.RandomStuff(thing, s.map);
			var monitorDef = DefDatabase<ThingDef>.GetNamed("VitalsMonitor");
			var monitor = sBed.Spawn(sBed.RandX, sBed.MaxZ, monitorDef);
			var bed = (Building_Bed)sBed.Spawn(sBed.RandX, sBed.RandZ, thing, stuff);
			bed.Medical = true;
		}
	}
}