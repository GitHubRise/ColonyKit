using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 在洗手池/浴缸/取水点喝水（DBH JobDriver_DrinkFromBasin 的移植）：
	/// 普通洁具走 <see cref="Building_Basin.TryUseWater"/>（取水点会覆盖成无限水或不耗热水），
	/// 水盆/动物水槽走自带的用量，每 tick 抽 0.04 L，喝到 99%。
	/// </summary>
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
			PathEndMode pathEnd = (job.targetA.Thing as Building_AssignableFixture)?.InteractionPathMode
				?? PathEndMode.InteractionCell;
			yield return Toils_Goto.GotoThing(TargetIndex.A, pathEnd);
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
				if (job.targetA.Thing is Building_WaterTub tub)
				{
					// 水盆/动物水槽自带水：每 60 tick 消耗一次水量，空了就结束。
					if (pawn.IsHashIntervalTick(60))
					{
						if (!tub.TryUseWater(out ContaminationLevel tubContam))
						{
							EndJobWith(JobCondition.Incompletable);
							return;
						}
						if (tubContam > waterQuality)
						{
							waterQuality = tubContam;
						}
					}
					pawn.needs?.TryGetNeed<Need_Thirst>()?.Drink();
					return;
				}
				if (job.targetA.Thing is Building_Basin basin)
				{
					// 洗手池/水龙头/免费取水点：走各自的 TryUseWater。
					if (!basin.TryUseWater(out ContaminationLevel basinContam))
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					if (basinContam > waterQuality)
					{
						waterQuality = basinContam;
					}
					pawn.needs?.TryGetNeed<Need_Thirst>()?.Drink();
					return;
				}
				// 其他接了水管的洁具（保留旧路径）。
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
