using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class WorkGiver_EmptySepticTank : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_SepticTank"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return t?.TryGetComp<CompSewageHandler>() is CompSewageHandler comp
				&& comp.sewageBuffer > 1f
				&& pawn.CanReserve(t)
				&& !t.IsForbidden(pawn);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(
				DefDatabase<JobDef>.GetNamedSilentFail("CK_EmptySepticTank"), t);
		}
	}
}
