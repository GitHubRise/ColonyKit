using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>往堆肥厂里搬粪渣（DBH WorkGiver_FillComposter 的移植，Hauling 工作类型）。</summary>
	public class WorkGiver_FillComposter : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_BiosolidsComposter"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_Composter composter) || composter.Fermented || composter.SpaceLeft <= 0)
			{
				return false;
			}
			CompProperties_TemperatureRuinable temp = composter.def.GetCompProperties<CompProperties_TemperatureRuinable>();
			if (temp != null)
			{
				float ambient = composter.AmbientTemperature;
				if (ambient < temp.minSafeTemperature + 2f || ambient > temp.maxSafeTemperature - 2f)
				{
					JobFailReason.Is("BadTemperature".Translate().ToLower());
					return false;
				}
			}
			if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null
				|| t.IsBurning())
			{
				return false;
			}
			if (FindMaterial(pawn, composter) == null)
			{
				JobFailReason.Is("CK.Agriculture.NoSludge".Translate());
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_Composter composter = (Building_Composter)t;
			Thing material = FindMaterial(pawn, composter);
			return material == null
				? null
				: JobMaker.MakeJob(DefDatabase<JobDef>.GetNamedSilentFail("CK_LoadComposter"), t, material);
		}

		private static Thing FindMaterial(Pawn pawn, Building_Composter composter)
		{
			ThingDef materialDef = composter.MaterialDef;
			if (materialDef == null)
			{
				return null;
			}
			return GenClosest.ClosestThingReachable(
				pawn.Position, pawn.Map, ThingRequest.ForDef(materialDef), PathEndMode.ClosestTouch,
				TraverseParms.For(pawn), 9999f,
				x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
		}
	}
}
