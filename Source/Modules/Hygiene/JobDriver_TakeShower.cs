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
				defaultDuration = 600,
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
				Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
				if (need != null)
				{
					need.Clean(0.002f);
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
				ThoughtDef thought = ThoughtDef.Named(cold ? "CK_ShowerCold" : "CK_ShowerHot");
				if (thought != null)
				{
					pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
				}
			});
			yield return wash;
		}
	}
}
