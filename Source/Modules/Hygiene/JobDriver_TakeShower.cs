using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH 洗澡流程的最小移植：预约 → 到场 → 冲洗 → 完成。</summary>
	public class JobDriver_TakeShower : JobDriver
	{
		private bool cold;
		private bool completed;
		private ContaminationLevel waterQuality = ContaminationLevel.Treated;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterQuality, "waterQuality", ContaminationLevel.Treated);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			Toil wash = new Toil
			{
				defaultDuration = 1000,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			wash.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			wash.initAction = () =>
			{
				Building_Shower shower = job.targetA.Thing as Building_Shower;
				cold = shower == null || !shower.IsHot;
			};
			wash.tickAction = () =>
			{
				Building_Shower shower = job.targetA.Thing as Building_Shower;
				if (shower == null || !shower.TryUseWater(out bool isCold, out ContaminationLevel contam))
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				// 热水罐可能在洗澡中途耗尽；每 tick 跟随最新水温，
				// 不能只在开始冷的时候把 cold 置 true。
				cold = isCold;
				if (contam > waterQuality)
				{
					waterQuality = contam;
				}
				Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
				if (need != null)
				{
					need.clean(shower.CleanedPerTick);
				}
				pawn.GainComfortFromCellIfPossible(1);
				if (need != null && need.CurLevel >= 1f)
				{
					completed = true;
					ReadyForNextToil();
				}
			};
			wash.AddFinishAction(() =>
			{
				// finish action 在中断（断水/断电/征兆/精神崩溃）时也会执行；
				// 没洗完就不该清污渍、给心情或清污染标记。
				if (!completed)
				{
					return;
				}
				pawn.filth?.CarriedFilthListForReading?.Clear();
				Building_Shower shower = job.targetA.Thing as Building_Shower;
				if (shower != null)
				{
					PrivacyUtil.BathingPrivacyLOS(pawn);
					SanitationUtil.ApplyBathroomThought(pawn, shower);
					SanitationUtil.WaterTempCheck(pawn, cold, false);
					// 洗掉上一次留下的污染标记；如果这次用的水本身不干净，
					// 紧接着的 ContaminationCheckWater 会重新标记。
					pawn.needs?.TryGetNeed<Need_Hygiene>()?.CleanContamination();
					SanitationUtil.ContaminationCheckWater(pawn, waterQuality);
					SanitationUtil.CheckForBlockage(shower);
				}
			});
			yield return wash;
		}
	}
}
