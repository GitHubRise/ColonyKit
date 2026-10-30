using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 取水点的自动取水 Job：拿水桶去同网水井（或附近小池塘/河边）打满，
	/// 再回取水点倒进水箱；每倒成一桶记一次「取水次数」。
	/// 水井/地表水都没有水泵，按手动速度；水井同网有水泵时更快。
	/// </summary>
	public class JobDriver_FillWaterPoint : JobDriver
	{
		private const TargetIndex SourceInd = TargetIndex.A;

		private const TargetIndex PointInd = TargetIndex.B;

		private const TargetIndex BucketInd = TargetIndex.C;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(SourceInd), job, 1, -1, null, errorOnFailed))
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
			// 地表水的 TargetA 是岸边可站格（没有 Thing），只对水井做消失检查。
			bool sourceHasThing = job.GetTarget(SourceInd).HasThing;
			if (sourceHasThing)
			{
				this.FailOnDespawnedNullOrForbidden(SourceInd);
			}
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
			if (sourceHasThing)
			{
				fill.FailOnDespawnedNullOrForbidden(SourceInd);
			}
			fill.FailOnDespawnedNullOrForbidden(PointInd);
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
				Building_WaterPoint point = job.GetTarget(PointInd).Thing as Building_WaterPoint;
				LocalTargetInfo source = job.GetTarget(SourceInd);
				if (bucket == null || point == null || !source.IsValid)
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

				float rate;
				ContaminationLevel quality = ContaminationLevel.Untreated;
				if (source.HasThing)
				{
					rate = JobDriver_FillBasin.FillRate(fromWell: true, point.Pumped);
				}
				else
				{
					IntVec3 waterCell = WaterSourceUtility.AdjacentWaterCell(pawn.Map, source.Cell);
					if (!waterCell.IsValid)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					rate = JobDriver_FillBasin.SurfaceFillRate;
					quality = WaterSourceUtility.SurfaceQuality(pawn.Map, waterCell);
				}

				float amount = Mathf.Min(rate * delta, bucket.Space);
				if (amount <= 0.0001f)
				{
					ReadyForNextToil();
					return;
				}
				bucket.AddWater(amount, quality);
				if (bucket.IsFull)
				{
					ReadyForNextToil();
				}
			};
			fill.WithProgressBar(
				SourceInd,
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
				CompBasinWater bucket = WaterContainerUtility.CarriedBucketAny(pawn);
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

			Toil gotoSource = job.GetTarget(SourceInd).HasThing
				? Toils_Goto.GotoThing(SourceInd, PathEndMode.Touch)
				: Toils_Goto.GotoCell(SourceInd, PathEndMode.OnCell);
			yield return Toils_Jump.JumpIf(gotoSource, () => WaterContainerUtility.CarriedBucket(pawn) != null);
			yield return Toils_Goto.GotoThing(BucketInd, PathEndMode.ClosestTouch);
			yield return pickup;
			yield return Toils_Jump.Jump(gotoSource);
			yield return gotoSource;
			yield return fill;
			yield return Toils_Goto.GotoThing(PointInd, PathEndMode.Touch);
			yield return pour;
		}
	}
}
