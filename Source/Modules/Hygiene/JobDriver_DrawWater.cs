using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 手动从水井打水：持续 600 tick，把水缓慢注入同一张水网的水箱；
	/// 水箱全满或水网不可用就提前结束。
	/// </summary>
	public class JobDriver_DrawWater : JobDriver
	{
		private const int WorkTicks = 600;

		private const float LitersPerTick = 0.05f;

		private int workDone;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			Toil draw = ToilMaker.MakeToil("CK_DrawWater");
			draw.defaultCompleteMode = ToilCompleteMode.Never;
			draw.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			draw.initAction = () => workDone = 0;
			draw.tickIntervalAction = delta =>
			{
				CompPipe pipe = (job.targetA.Thing as ThingWithComps)?.GetComp<CompPipe>();
				PlumbingNet net = pipe?.pipeNet;
				if (net == null || net.WaterTowers.Count == 0)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float remaining = net.PushWater(LitersPerTick * delta);
				if (remaining > 0.0001f)
				{
					// 所有水箱都满了，没必要继续。
					ReadyForNextToil();
					return;
				}
				workDone += delta;
				if (workDone >= WorkTicks)
				{
					ReadyForNextToil();
				}
			};
			draw.WithProgressBar(
				TargetIndex.A,
				() => (float)workDone / WorkTicks,
				interpolateBetweenActorAndTarget: true);
			yield return draw;
		}
	}
}
