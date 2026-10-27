using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>用装备的脸盆在水井打水：持续 300 tick，把脸盆加满。</summary>
	public class JobDriver_FillBasin : JobDriver
	{
		private const int WorkTicks = 300;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			int workDone = 0;
			Toil fill = ToilMaker.MakeToil("CK_FillBasin");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			fill.initAction = () => workDone = 0;
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater basin = Basin(pawn);
				if (basin == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				CompPipe pipe = (job.targetA.Thing as ThingWithComps)?.GetComp<CompPipe>();
				if (pipe?.pipeNet == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				basin.Water = System.Math.Min(basin.Props.Capacity, basin.Water + 0.1f * delta);
				workDone += delta;
				if (basin.IsFull || workDone >= WorkTicks)
				{
					ReadyForNextToil();
				}
			};
			fill.WithProgressBar(
				TargetIndex.A,
				() => (float)workDone / WorkTicks,
				interpolateBetweenActorAndTarget: true);
			yield return fill;
		}

		internal static CompBasinWater Basin(Pawn pawn)
		{
			return pawn?.equipment?.Primary?.TryGetComp<CompBasinWater>();
		}
	}
}
