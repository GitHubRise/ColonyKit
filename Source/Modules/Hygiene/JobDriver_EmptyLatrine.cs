using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 清空旱厕：走到坑边挖 300–900 tick（越满越久），完工时把坑清空。
	/// 粪污按 DBH 的设定视为被运走，不再生成物品。
	/// </summary>
	public class JobDriver_EmptyLatrine : JobDriver
	{
		private Building_Latrine Latrine => job.targetA.Thing as Building_Latrine;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			Building_Latrine latrine = Latrine;
			float fill = latrine?.PitFillPercent ?? 1f;
			Toil empty = new Toil
			{
				defaultDuration = Mathf.RoundToInt(300f + 600f * fill),
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			empty.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			empty.socialMode = RandomSocialMode.Quiet;
			empty.AddFinishAction(() =>
			{
				if (job.targetA.Thing is Building_Latrine target)
				{
					target.Empty(Building_Latrine.PitCapacity);
				}
			});
			yield return empty;
		}
	}
}
