using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>往洗衣机里装衣服（DBH WorkGiver_WashingLoad 的移植，Hauling）。</summary>
	public class WorkGiver_WashingLoad : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_WashingMachine"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WashingMachine washer)
				|| t.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			if (!washer.HasSpace)
			{
				JobFailReason.Is("CK.Hygiene.WashingNoSpace".Translate());
				return false;
			}
			if (FindClothes(pawn, washer) == null)
			{
				JobFailReason.Is("CK.Hygiene.NoDirtyClothes".Translate());
				return false;
			}
			return !t.IsBurning();
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Thing clothes = FindClothes(pawn, t as IStoreSettingsParent);
			if (clothes == null)
			{
				JobFailReason.Is("CK.Hygiene.NoDirtyClothes".Translate());
				return null;
			}
			Job job = JobMaker.MakeJob(
				DefDatabase<JobDef>.GetNamedSilentFail("CK_LoadWashing"), t, clothes);
			job.count = 1;
			return job;
		}

		private static bool Predicate(Thing x, Pawn pawn, IStoreSettingsParent owner)
		{
			return x is Apparel apparel
				&& apparel.WornByCorpse
				&& !x.IsForbidden(pawn)
				&& pawn.CanReserve(x)
				&& owner.GetStoreSettings().filter.Allows(x);
		}

		private static Thing FindClothes(Pawn pawn, IStoreSettingsParent owner)
		{
			return GenClosest.ClosestThingReachable(
				pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Apparel),
				PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
				x => Predicate(x, pawn, owner));
		}
	}
}
