using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>按免费取水点的清单，把一只空/半满水桶在取水点打满；完成后桶留给小人。</summary>
	public class JobDriver_FillSpotBucket : JobDriver
	{
		private const TargetIndex SpotInd = TargetIndex.A;

		private const TargetIndex BucketInd = TargetIndex.B;

		/// <summary>地表水手动打水速度。</summary>
		private const float Rate = 0.15f;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(SpotInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			if (job.GetTarget(BucketInd).HasThing)
			{
				return pawn.Reserve(job.GetTarget(BucketInd), job, 1, -1, null, errorOnFailed);
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(SpotInd);

			Toil pickup = ToilMaker.MakeToil("CK_FillSpotBucketPickup");
			pickup.initAction = () =>
			{
				Thing bucket = job.GetTarget(BucketInd).Thing;
				if (bucket == null || bucket.Destroyed || !bucket.Spawned)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				bucket.DeSpawn();
				if (!pawn.inventory.innerContainer.TryAdd(bucket))
				{
					GenPlace.TryPlaceThing(bucket, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					EndJobWith(JobCondition.Incompletable);
				}
			};

			Toil fill = ToilMaker.MakeToil("CK_FillSpotBucketFill");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.FailOnDespawnedNullOrForbidden(SpotInd);
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
				Building_WaterSpot spot = job.GetTarget(SpotInd).Thing as Building_WaterSpot;
				if (bucket == null || spot == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float amount = Mathf.Min(Rate * delta, bucket.Space);
				if (amount > 0.0001f)
				{
					bucket.AddWater(amount, ContaminationLevel.Untreated);
				}
				if (bucket.IsFull || amount <= 0.0001f)
				{
					if (job.bill != null)
					{
						job.bill.Notify_IterationCompleted(pawn, null);
					}
					ReadyForNextToil();
				}
			};
			fill.WithProgressBar(
				SpotInd,
				() =>
				{
					CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
					return bucket == null || bucket.Props.Capacity <= 0f
						? 1f
						: bucket.Water / bucket.Props.Capacity;
				},
				interpolateBetweenActorAndTarget: true);

			Toil gotoSpot = Toils_Goto.GotoThing(SpotInd, PathEndMode.Touch);
			yield return Toils_Jump.JumpIf(gotoSpot, () => WaterContainerUtility.CarriedBucket(pawn) != null);
			yield return Toils_Goto.GotoThing(BucketInd, PathEndMode.ClosestTouch);
			yield return pickup;
			yield return Toils_Jump.Jump(gotoSpot);
			yield return gotoSpot;
			yield return fill;
		}
	}
}
