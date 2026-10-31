using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 疏通堵塞的洁具（DBH JobDriver_FixBlockage 的精简版）：
	/// 走到洁具旁边，用皮搋子捣 300 tick，完工时把 <see cref="CompBlockage"/> 复位。
	/// </summary>
	public class JobDriver_FixBlockage : JobDriver
	{
		private const TargetIndex FixtureInd = TargetIndex.A;

		private const int UnclogTicks = 300;

		private Thing Fixture => job.GetTarget(FixtureInd).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(FixtureInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(FixtureInd);
			yield return Toils_Goto.GotoThing(FixtureInd, PathEndMode.Touch)
				.FailOnDespawnedOrNull(FixtureInd);

			Toil unclog = Toils_General.Wait(UnclogTicks);
			unclog.FailOnDespawnedOrNull(FixtureInd);
			unclog.FailOnCannotTouch(FixtureInd, PathEndMode.Touch);
			unclog.WithProgressBarToilDelay(FixtureInd);
			unclog.PlaySustainerOrSound(
				() => DefDatabase<SoundDef>.GetNamedSilentFail("CK_Plunging"));
			yield return unclog;

			yield return new Toil
			{
				initAction = () =>
				{
					Fixture?.TryGetComp<CompBlockage>()?.Notify_Repaired();
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
