using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 清洗便盆（DBH JobDriver_cleanBedpan 的移植，做法不同）：
	/// 用过的便盆搬到最近的水源/水盆边（TargetB），洗 300 tick 后 <see cref="CompBedPan.Clean"/>，
	/// 洗完就放在水边地上。DBH 是就地洗掉整个便盆，CK 改成可重复使用的物品。
	/// </summary>
	public class JobDriver_CleanBedpan : JobDriver
	{
		private const TargetIndex BedpanInd = TargetIndex.A;

		private const TargetIndex SourceInd = TargetIndex.B;

		private const int CleanTicks = 300;

		public Thing Bedpan => job.GetTarget(BedpanInd).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(BedpanInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			// TargetB 可能是地表水的岸边可站格（没有 Thing），只有水源是建筑时才检查它是否还在。
			bool sourceHasThing = job.GetTarget(SourceInd).HasThing;
			if (sourceHasThing)
			{
				this.FailOnDespawnedNullOrForbidden(SourceInd);
			}

			yield return Toils_Reserve.Reserve(BedpanInd);
			yield return Toils_Goto.GotoThing(BedpanInd, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(BedpanInd)
				.FailOnSomeonePhysicallyInteracting(BedpanInd);
			yield return Toils_Haul.StartCarryThing(BedpanInd)
				.FailOnDestroyedNullOrForbidden(BedpanInd);

			Toil gotoSource = sourceHasThing
				? Toils_Goto.GotoThing(SourceInd, PathEndMode.Touch)
				: Toils_Goto.GotoCell(SourceInd, PathEndMode.OnCell);
			yield return gotoSource;

			Toil wash = ToilMaker.MakeToil("CK_CleanBedpan");
			wash.defaultDuration = CleanTicks;
			wash.defaultCompleteMode = ToilCompleteMode.Delay;
			wash.socialMode = RandomSocialMode.Quiet;
			wash.initAction = () =>
			{
				// 走到水边却两手空空（中途被卸下/抢走）就别装模作样洗 300 tick。
				if (pawn.carryTracker?.CarriedThing == null)
				{
					EndJobWith(JobCondition.Incompletable);
				}
			};
			wash.AddFinishAction(() =>
			{
				if (pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing cleaned))
				{
					cleaned.TryGetComp<CompBedPan>()?.Clean();
				}
				pawn.records?.Increment(RecordDefOf.MessesCleaned);
			});
			// 便盆这时在手里（已 DeSpawn），特效挂在 TargetB 的水源上。
			wash.WithEffect(EffecterDefOf.Clean, SourceInd);
			wash.WithProgressBarToilDelay(SourceInd);
			wash.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
			yield return wash;
		}
	}
}
