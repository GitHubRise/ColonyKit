using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 免费取水点清单的工作：取 BillStack 里第一条 ShouldDoNow 的账单。
	/// 「取水（矿泉水）」不需要容器；「取水（水桶）」1 桶；「批量取水」最多 5 桶。
	/// </summary>
	public class WorkGiver_FillWaterSpot : WorkGiver_Scanner
	{
		private const int BulkBucketCount = 5;

		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterSpot"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WaterSpot spot) || !spot.BillStack.AnyShouldDoNow)
			{
				return false;
			}
			Bill bill = spot.BillStack.FirstShouldDoNow;
			if (bill == null || !bill.PawnAllowedToStartAnew(pawn))
			{
				return false;
			}
			if (t.IsForbidden(pawn)
				|| t.IsBurning()
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			if (bill.recipe.defName == "CK_DrawWaterBottle")
			{
				return true;
			}
			return WaterContainerUtility.FindBucketForJob(pawn) != null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_WaterSpot spot))
			{
				return null;
			}
			Bill bill = spot.BillStack.FirstShouldDoNow;
			if (bill == null || !bill.PawnAllowedToStartAnew(pawn))
			{
				return null;
			}
			if (bill.recipe.defName == "CK_DrawWaterBottle")
			{
				JobDef bottleDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_FillWaterBottle");
				if (bottleDef == null)
				{
					return null;
				}
				Job bottleJob = JobMaker.MakeJob(bottleDef);
				bottleJob.bill = bill;
				bottleJob.targetA = spot;
				return bottleJob;
			}
			bool bulk = bill.recipe.defName == "CK_DrawWaterBulk";
			List<Thing> buckets = WaterContainerUtility.FindBucketsForJob(
				pawn, bulk ? BulkBucketCount : 1);
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail(
				bulk ? "CK_FillSpotBucketBulk" : "CK_FillSpotBucket");
			if (buckets.Count == 0 || def == null)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(def);
			job.bill = bill;
			job.targetA = spot;
			if (bulk)
			{
				job.count = buckets.Count;
				// 背包里的桶不用再走过去拾取；地上的桶放进队列。
				job.targetQueueB = buckets
					.Where(b => b.Spawned)
					.Select(b => new LocalTargetInfo(b))
					.ToList();
			}
			else if (buckets[0].Spawned)
			{
				job.targetB = buckets[0];
			}
			return job;
		}
	}
}
