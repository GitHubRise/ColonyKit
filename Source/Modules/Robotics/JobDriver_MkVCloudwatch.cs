using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 闲时躺在户外看云；不依赖 Joy 需求。</summary>
	public class JobDriver_MkVCloudwatch : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

			Toil watch = ToilMaker.MakeToil("MkVCloudwatch");
			watch.initAction = () => pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
			watch.defaultCompleteMode = ToilCompleteMode.Delay;
			watch.defaultDuration = 900;
			watch.FailOn(() => !pawn.Spawned || !pawn.Position.InBounds(pawn.Map));
			watch.FailOn(() => pawn.Position.Roofed(pawn.Map));
			yield return watch;
		}
	}
}
