using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 取水点的自动取水 Job：拿水桶去同网水井打满，再回取水点倒进水箱；
	/// 每倒成一桶记一次「取水次数」。没有水泵时在水井边慢慢打。
	/// </summary>
	public class JobDriver_FillWaterPoint : JobDriver
	{
		private const TargetIndex WellInd = TargetIndex.A;

		private const TargetIndex PointInd = TargetIndex.B;

		private const TargetIndex BucketInd = TargetIndex.C;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(WellInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			if (!pawn.Reserve(job.GetTarget(PointInd), job, 1, -1, null, errorOnFailed))
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
			this.FailOnDespawnedNullOrForbidden(WellInd);
			this.FailOnDespawnedNullOrForbidden(PointInd);

			Toil pickup = ToilMaker.MakeToil("CK_FillWaterPointPickup");
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

			Toil fill = ToilMaker.MakeToil("CK_FillWaterPointFill");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.FailOnDespawnedNullOrForbidden(WellInd);
			fill.FailOnDespawnedNullOrForbidden(PointInd);
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
				Building_WaterPoint point = job.GetTarget(PointInd).Thing as Building_WaterPoint;
				if (bucket == null || point == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (point.Space <= 0.1f)
				{
					// 水箱在打水途中被网络泵灌满了，这一趟不用再打。
					ReadyForNextToil();
					return;
				}
				float amount = Mathf.Min(JobDriver_FillBasin.FillRate(fromWell: true, point.Pumped) * delta, bucket.Space);
				if (amount <= 0.0001f)
				{
					ReadyForNextToil();
					return;
				}
				bucket.AddWater(amount, ContaminationLevel.Untreated);
				if (bucket.IsFull)
				{
					ReadyForNextToil();
				}
			};
			fill.WithProgressBar(
				WellInd,
				() =>
				{
					CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
					return bucket == null || bucket.Props.Capacity <= 0f
						? 1f
						: bucket.Water / bucket.Props.Capacity;
				},
				interpolateBetweenActorAndTarget: true);

			Toil pour = ToilMaker.MakeToil("CK_FillWaterPointPour");
			pour.defaultCompleteMode = ToilCompleteMode.Instant;
			pour.FailOnDespawnedNullOrForbidden(PointInd);
			pour.initAction = () =>
			{
				Building_WaterPoint point = job.GetTarget(PointInd).Thing as Building_WaterPoint;
				CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
				if (point == null || bucket == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float poured = point.AddWater(bucket.Water, bucket.Contamination);
				bucket.Drain(poured);
				if (poured < Building_WaterPoint.MinDrawLiters)
				{
					// 水箱只剩一点空间，倒进去的不到半桶，这一趟不算一次取水。
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				point.NotifyBucketDelivered();
			};

			Toil gotoWell = Toils_Goto.GotoThing(WellInd, PathEndMode.Touch);
			yield return Toils_Jump.JumpIf(gotoWell, () => WaterContainerUtility.CarriedBucket(pawn) != null);
			yield return Toils_Goto.GotoThing(BucketInd, PathEndMode.ClosestTouch);
			yield return pickup;
			yield return Toils_Jump.Jump(gotoWell);
			yield return gotoWell;
			yield return fill;
			yield return Toils_Goto.GotoThing(PointInd, PathEndMode.Touch);
			yield return pour;
		}
	}
}
