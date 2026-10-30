using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>免费取水点清单的工作：有未完成的取水/维持任务、且找得到空桶时派活。</summary>
	public class WorkGiver_FillWaterSpot : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterSpot"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WaterSpot spot) || !spot.NeedsWork)
			{
				return false;
			}
			if (t.IsForbidden(pawn)
				|| t.IsBurning()
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			return WaterContainerUtility.FindBucketForJob(pawn) != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WaterSpot spot))
			{
				return null;
			}
			Thing bucket = WaterContainerUtility.FindBucketForJob(pawn);
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_FillSpotBucket");
			if (bucket == null || def == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def);
			job.targetA = spot;
			if (bucket.Spawned)
			{
				job.targetB = bucket;
			}
			return job;
		}
	}
}
