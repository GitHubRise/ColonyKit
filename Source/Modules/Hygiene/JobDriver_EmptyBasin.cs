using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>把脸盆里的水倒进水箱所在的水网。</summary>
	public class JobDriver_EmptyBasin : JobDriver
	{
		private const int WorkTicks = 120;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			int workDone = 0;
			Toil pour = ToilMaker.MakeToil("CK_EmptyBasin");
			pour.defaultCompleteMode = ToilCompleteMode.Never;
			pour.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			pour.initAction = () => workDone = 0;
			pour.tickIntervalAction = delta =>
			{
				CompBasinWater basin = JobDriver_FillBasin.Basin(pawn);
				if (basin == null || basin.IsEmpty)
				{
					ReadyForNextToil();
					return;
				}
				CompPipe pipe = (job.targetA.Thing as ThingWithComps)?.GetComp<CompPipe>();
				PlumbingNet net = pipe?.pipeNet;
				if (net == null || net.WaterTowers.Count == 0)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float amount = Mathf.Min(0.25f * delta, basin.Water);
				float remaining = net.PushWater(amount);
				float poured = amount - remaining;
				basin.Water = Mathf.Max(0f, basin.Water - poured);
				if (poured <= 0.0001f)
				{
					// 水箱已经满了，剩下的留在盆里。
					ReadyForNextToil();
					return;
				}
				workDone += delta;
				if (basin.IsEmpty || workDone >= WorkTicks)
				{
					ReadyForNextToil();
				}
			};
			pour.WithProgressBar(
				TargetIndex.A,
				() => (float)workDone / WorkTicks,
				interpolateBetweenActorAndTarget: true);
			yield return pour;
		}
	}
}
