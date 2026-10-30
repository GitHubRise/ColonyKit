using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「取水点」的取水次数工作：有未完成的次数、水箱还有空间、同网有可达水井、
	/// 且能找到一只没满的水桶时，派小人去井边打水再回来倒进水箱。
	/// </summary>
	public class WorkGiver_FillWaterPoint : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterPoint"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WaterPoint point)
				|| !point.HasPendingDraws
				|| point.Space < Building_WaterPoint.MinDrawLiters)
			{
				return false;
			}
			if (t.IsForbidden(pawn)
				|| t.IsBurning()
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			LocalTargetInfo source = point.FindDrawSource(pawn, out _);
			if (!source.IsValid)
			{
				return false;
			}
			if (source.HasThing)
			{
				if (!pawn.CanReserveAndReach(source, PathEndMode.Touch, pawn.NormalMaxDanger()))
				{
					return false;
				}
			}
			else if (!pawn.CanReserveAndReach(source, PathEndMode.OnCell, pawn.NormalMaxDanger()))
			{
				return false;
			}
			return WaterContainerUtility.FindBucketForJob(pawn) != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WaterPoint point))
			{
				return null;
			}
			LocalTargetInfo source = point.FindDrawSource(pawn, out _);
			Thing bucket = WaterContainerUtility.FindBucketForJob(pawn);
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_FillWaterPoint");
			if (!source.IsValid || bucket == null || def == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def);
			job.targetA = source;
			job.targetB = point;
			if (bucket.Spawned)
			{
				job.targetC = bucket;
			}
			return job;
		}
	}
}
