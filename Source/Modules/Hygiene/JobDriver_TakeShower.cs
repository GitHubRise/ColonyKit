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
				if (isCold)
				{
					cold = true;
				}
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
					ReadyForNextToil();
				}
			};
			wash.AddFinishAction(() =>
			{
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
