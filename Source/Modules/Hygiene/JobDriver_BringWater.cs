using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 送水：医生/看守拿上瓶装水（凉白开/纯净水/矿泉水），到病人或囚徒身边把水交给它；
	/// 对方能自己喝就放进物品栏，渴了自己喝；倒地/卧床的就开一瓶当场喂掉。
	/// 没有瓶装水时由 <see cref="BringWaterUtility.TryMakeJob"/> 退回 <c>CK_WaterPatient</c> 喂水。
	/// </summary>
	public class JobDriver_BringWater : JobDriver
	{
		private const TargetIndex TargetInd = TargetIndex.A;

		private const TargetIndex BottleInd = TargetIndex.B;

		private const int HandOverTicks = 90;

		private const int FeedTicks = 240;

		private Pawn TargetPawn => job.GetTarget(TargetInd).Thing as Pawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(TargetInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			LocalTargetInfo bottle = job.GetTarget(BottleInd);
			// 水已经在自己物品栏里时没有可预约的地图目标，直接进拿水环节。
			if (bottle.HasThing && bottle.Thing.Spawned)
			{
				return pawn.Reserve(bottle, job, 1, -1, null, errorOnFailed);
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetInd);

			Thing bottle = job.GetTarget(BottleInd).Thing;
			if (bottle != null && bottle.Spawned)
			{
				yield return Toils_Goto.GotoThing(BottleInd, PathEndMode.ClosestTouch);
			}
			yield return TakeBottles();
			yield return Toils_Goto.GotoThing(TargetInd, PathEndMode.Touch);
			yield return GiveWater();
		}

		/// <summary>从地上那叠水或自己物品栏里取出 job.count 瓶，放进自己物品栏。</summary>
		private Toil TakeBottles()
		{
			Toil take = ToilMaker.MakeToil("CK_BringWaterTake");
			take.initAction = () =>
			{
				Thing source = job.GetTarget(BottleInd).Thing;
				if (source == null || source.Destroyed || source.stackCount <= 0)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				int count = Mathf.Max(1, job.count);
				if (source.ParentHolder is Pawn_InventoryTracker holder && holder.pawn == pawn)
				{
					// 水本来就在送水人身上，不用搬。
					job.count = Mathf.Min(count, source.stackCount);
					return;
				}
				Thing part = source.SplitOff(Mathf.Min(count, source.stackCount));
				if (part == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (part.Spawned)
				{
					part.DeSpawn();
				}
				if (pawn.inventory?.innerContainer == null || !pawn.inventory.innerContainer.TryAdd(part))
				{
					GenPlace.TryPlaceThing(part, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				job.count = part.stackCount;
			};
			return take;
		}

		/// <summary>能自己喝的交接物品栏，动不了的开一瓶当场喂。</summary>
		private Toil GiveWater()
		{
			Pawn target = TargetPawn;
			bool selfDrink = BringWaterUtility.CanDrinkOnOwn(target);
			Toil give = ToilMaker.MakeToil("CK_BringWaterGive");
			give.defaultCompleteMode = ToilCompleteMode.Delay;
			give.defaultDuration = selfDrink ? HandOverTicks : FeedTicks;
			give.socialMode = RandomSocialMode.Quiet;
			give.FailOnDespawnedNullOrForbidden(TargetInd);
			give.FailOn(() => TargetPawn == null || TargetPawn.Dead);
			if (selfDrink)
			{
				give.initAction = () => BringWaterUtility.HandOverBottles(pawn, target, job.count);
			}
			else
			{
				give.WithProgressBar(
					TargetInd,
					() => (job.GetTarget(TargetInd).Thing as Pawn)?.needs?.TryGetNeed<Need_Thirst>()?.CurLevel ?? 1f,
					interpolateBetweenActorAndTarget: true);
				give.AddFinishAction(() => BringWaterUtility.FeedBottle(pawn, target));
			}
			return give;
		}
	}
}
