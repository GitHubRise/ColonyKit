using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>把便携脸盆里的水倒进摆放式水盆：最多倒满 30 次用量，剩下的留在脸盆里。</summary>
	public class JobDriver_FillTubFromBasin : JobDriver
	{
		private const TargetIndex TubInd = TargetIndex.A;

		private const int Duration = 60;

		private Building_WaterTub Tub => job.GetTarget(TubInd).Thing as Building_WaterTub;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TubInd);
			yield return Toils_Goto.GotoThing(TubInd, PathEndMode.Touch);
			yield return Toils_General.Wait(Duration)
				.FailOnDestroyedNullOrForbidden(TubInd)
				.FailOnCannotTouch(TubInd, PathEndMode.Touch)
				.WithProgressBarToilDelay(TubInd);
			yield return new Toil
			{
				initAction = () =>
				{
					Building_WaterTub tub = Tub;
					CompBasinWater basin = JobDriver_FillBasin.CarriedBasinWithWater(pawn);
					if (tub == null || basin == null)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					float liters = Mathf.Min(basin.Water, Building_WaterTub.MaxUses - tub.usesRemaining);
					int added = tub.AddWater(Mathf.CeilToInt(liters), basin.Contamination);
					basin.Water = Mathf.Max(0f, basin.Water - added);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
