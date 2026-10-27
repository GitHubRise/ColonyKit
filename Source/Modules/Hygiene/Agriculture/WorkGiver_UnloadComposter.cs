using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>把堆肥厂里的有机肥取出来（DBH WorkGiver_UnloadComposter 的移植）。</summary>
	public class WorkGiver_UnloadComposter : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_BiosolidsComposter"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_Composter composter) || !composter.Fermented)
			{
				return false;
			}
			if (t.IsBurning() || t.IsForbidden(pawn))
			{
				return false;
			}
			return pawn.CanReserve(t, 1, -1, null, forced);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamedSilentFail("CK_UnloadComposter"), t);
		}
	}
}
