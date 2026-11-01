using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>走到组装舱旁边，按下「开始组装」——Mk-V 自己制造同类的那一步。</summary>
	public class JobDriver_MkVAssemble : JobDriver
	{
		private const TargetIndex BayIndex = TargetIndex.A;

		private CompMkVAssemblyBay BayComp =>
			(job?.GetTarget(BayIndex).Thing as Building)?.TryGetComp<CompMkVAssemblyBay>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(BayIndex);
			yield return Toils_Goto.GotoThing(BayIndex, PathEndMode.InteractionCell);

			Toil start = ToilMaker.MakeToil("MkVAssemble");
			start.initAction = () =>
			{
				CompMkVAssemblyBay comp = BayComp;
				if (comp == null || !comp.CanStart(out _))
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				comp.StartAssembly();
			};
			start.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return start;
		}
	}
}
