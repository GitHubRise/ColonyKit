using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>洗完把衣服取出来（DBH WorkGiver_WashingUnload 的移植，Hauling）。</summary>
	public class WorkGiver_WashingUnload : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_WashingMachine"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			return t is Building_WashingMachine washer && !t.IsBurning() && washer.MustUnload;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(
				DefDatabase<JobDef>.GetNamedSilentFail("CK_UnloadWashing"), t, null);
		}
	}
}
