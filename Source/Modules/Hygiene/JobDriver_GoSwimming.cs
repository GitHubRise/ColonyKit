using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>游泳（DBH JobDriver_GoSwimming 的移植）：走到池边 → 下水游一段 → 上岸。</summary>
	public class JobDriver_GoSwimming : JobDriver
	{
		private const TargetIndex PoolInd = TargetIndex.A;

		private const TargetIndex CellInd = TargetIndex.B;

		private Building_Pool Pool => job.GetTarget(PoolInd).Thing as Building_Pool;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, job.def.joyMaxParticipants, 0, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(PoolInd);
			yield return Toils_Misc.FindRandomAdjacentReachableCell(PoolInd, CellInd);
			yield return Toils_Goto.GotoCell(CellInd, PathEndMode.OnCell);
			Toil swim = new Toil();
			swim.defaultCompleteMode = ToilCompleteMode.Delay;
			swim.defaultDuration = job.def.joyDuration;
			swim.initAction = () =>
			{
				Pool?.occupants.Add(pawn);
				pawn.pather.StopDead();
			};
			swim.tickAction = () =>
			{
				Building_Pool pool = Pool;
				if (pool == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				pawn.GainComfortFromCellIfPossible(1);
				SanitationUtil.Hydrotherapy(pawn, pool, healscars: true);
				pawn.needs?.TryGetNeed<Need_Hygiene>()?.clean(pool.cleanedPerTick);
				if (!pawn.IsPrisoner)
				{
					JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.None, 1f, pool);
				}
			};
			swim.AddFinishAction(() =>
			{
				JoyUtility.TryGainRecRoomThought(pawn);
				Pool?.occupants.Remove(pawn);
			});
			yield return swim;
			yield return Toils_Goto.GotoCell(CellInd, PathEndMode.OnCell);
		}
	}
}
