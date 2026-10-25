using System.Collections.Generic;
using ColonyKit.Modules.Hygiene;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>疏通堵塞洁具：持续 600 tick 后清除 CompBlockage.blocked。</summary>
	public class JobDriver_NannyUnblock : JobDriver
	{
		private const TargetIndex FixtureInd = TargetIndex.A;
		private const int WorkTicks = 600;

		private int workDone;

		private Building Fixture => job.GetTarget(FixtureInd).Thing as Building;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(FixtureInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(FixtureInd);
			yield return Toils_Goto.GotoThing(FixtureInd, PathEndMode.Touch);

			Toil unblock = ToilMaker.MakeToil("CK_NannyUnblock");
			unblock.defaultCompleteMode = ToilCompleteMode.Never;
			unblock.FailOnDespawnedNullOrForbidden(FixtureInd);
			unblock.initAction = () => workDone = 0;
			unblock.tickIntervalAction = delta =>
			{
				Building fixture = Fixture;
				CompBlockage blockage = fixture?.TryGetComp<CompBlockage>();
				if (blockage == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				workDone += delta;
				if (workDone >= WorkTicks)
				{
					blockage.Notify_Repaired();
					ReadyForNextToil();
				}
			};
			unblock.WithProgressBar(
				FixtureInd,
				() => (float)workDone / WorkTicks,
				interpolateBetweenActorAndTarget: true);
			yield return unblock;
		}
	}
}
