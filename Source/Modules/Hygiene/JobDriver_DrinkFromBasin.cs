using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>在洗手池/浴缸喝水（DBH JobDriver_DrinkFromBasin 的移植）：每 tick 抽 0.04 L，喝到 99%。</summary>
	public class JobDriver_DrinkFromBasin : JobDriver
	{
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
			this.FailOn(() =>
			{
				Building_AssignableFixture fixture = job.targetA.Thing as Building_AssignableFixture;
				return fixture != null && !fixture.Working().Accepted;
			});
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			Toil drink = new Toil
			{
				defaultDuration = 500,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			drink.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			drink.AddEndCondition(() =>
			{
				Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
				if (thirst == null)
				{
					return JobCondition.Incompletable;
				}
				return thirst.CurLevel < 0.99f ? JobCondition.Ongoing : JobCondition.Succeeded;
			});
			drink.tickAction = () =>
			{
				CompPipe pipe = job.targetA.Thing.TryGetComp<CompPipe>();
				if (pipe?.pipeNet != null)
				{
					if (!pipe.pipeNet.PullWater(0.04f, out ContaminationLevel contam))
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					if (contam > waterQuality)
					{
						waterQuality = contam;
					}
				}
				pawn.needs?.TryGetNeed<Need_Thirst>()?.Drink();
			};
			drink.AddFinishAction(() =>
			{
				SanitationUtil.ContaminationCheckDrinking(pawn, waterQuality);
			});
			yield return drink;
		}
	}
}
