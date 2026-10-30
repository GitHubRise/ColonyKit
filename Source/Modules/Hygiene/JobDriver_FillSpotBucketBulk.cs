using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 批量取水：一次带最多 5 只空桶到取水点，按顺序把每只桶打满，最后完成一次账单。
	/// 单桶仍走 JobDriver_FillSpotBucket。
	/// </summary>
	public class JobDriver_FillSpotBucketBulk : JobDriver
	{
		private const TargetIndex SpotInd = TargetIndex.A;

		private const TargetIndex BucketInd = TargetIndex.B;

		private const float Rate = 0.15f;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(SpotInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			if (job.targetQueueB != null)
			{
				foreach (LocalTargetInfo target in job.targetQueueB)
				{
					if (target.HasThing
						&& !pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
					{
						ModLog.Warn("Hygiene", $"bulk reserve failed: {target.Thing}");
						return false;
					}
				}
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(SpotInd);

			int queueCount = job.targetQueueB?.Count ?? 0;
			for (int i = 0; i < queueCount; i++)
			{
				int index = i;
				Toil select = ToilMaker.MakeToil("CK_FillSpotBucketBulkSelect");
				select.initAction = () =>
				{
					if (job.targetQueueB != null && index < job.targetQueueB.Count)
					{
						job.targetB = job.targetQueueB[index];
					}
				};
				yield return select;
				yield return Toils_Goto.GotoThing(BucketInd, PathEndMode.ClosestTouch);
				Toil pickup = ToilMaker.MakeToil("CK_FillSpotBucketBulkPickup");
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
				yield return pickup;
			}

			yield return Toils_Goto.GotoThing(SpotInd, PathEndMode.Touch);

			Toil fill = ToilMaker.MakeToil("CK_FillSpotBucketBulkFill");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.FailOnDespawnedNullOrForbidden(SpotInd);
			fill.initAction = () =>
			{
				if (!HasAnyBucket())
				{
					ModLog.Warn("Hygiene", "bulk fill has no buckets");
					EndJobWith(JobCondition.Incompletable);
				}
			};
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater bucket = WaterContainerUtility.CarriedBucket(pawn);
				if (bucket == null)
				{
					if (AllBucketsFull())
					{
						CompleteBill();
					}
					else
					{
						EndJobWith(JobCondition.Incompletable);
					}
					return;
				}
				float amount = Mathf.Min(Rate * delta, bucket.Space);
				if (amount > 0.0001f)
				{
					bucket.AddWater(amount, ContaminationLevel.Untreated);
				}
				if (AllBucketsFull())
				{
					CompleteBill();
				}
			};
			fill.WithProgressBar(
				SpotInd,
				() =>
				{
					float water = 0f;
					float capacity = 0f;
					if (pawn.inventory?.innerContainer != null)
					{
						foreach (Thing thing in pawn.inventory.innerContainer)
						{
							CompBasinWater container = thing.TryGetComp<CompBasinWater>();
							if (container != null && WaterContainerUtility.IsBucket(container))
							{
								water += container.Water;
								capacity += container.Props.Capacity;
							}
						}
					}
					return capacity <= 0f ? 0f : water / capacity;
				},
				interpolateBetweenActorAndTarget: true);

			yield return fill;
		}

		private bool HasAnyBucket()
		{
			if (pawn.inventory?.innerContainer == null)
			{
				return false;
			}
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container != null && WaterContainerUtility.IsBucket(container))
				{
					return true;
				}
			}
			return false;
		}

		private bool AllBucketsFull()
		{
			bool any = false;
			if (pawn.inventory?.innerContainer == null)
			{
				return false;
			}
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null || !WaterContainerUtility.IsBucket(container))
				{
					continue;
				}
				any = true;
				if (!container.IsFull)
				{
					return false;
				}
			}
			return any;
		}

		private void CompleteBill()
		{
			if (job.bill != null)
			{
				job.bill.Notify_IterationCompleted(pawn, null);
			}
			ReadyForNextToil();
		}
	}
}
