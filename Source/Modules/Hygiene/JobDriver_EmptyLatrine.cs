using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 清空旱厕：走到坑边挖 300–900 tick（越满越久），完工时按 DBH 的做法
	/// 把坑里的存量变成粪渣（CK_FecalSludge）掉在殖民者脚下，坑清零。
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
				if (!(job.targetA.Thing is Building_Latrine target))
				{
					return;
				}
				int amount = Mathf.CeilToInt(target.sewage);
				target.Empty(Building_Latrine.SewageLimit);
				if (amount <= 0 || pawn.Map == null)
				{
					return;
				}
				ThingDef sludgeDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_FecalSludge");
				if (sludgeDef == null)
				{
					return;
				}
				Thing sludge = ThingMaker.MakeThing(sludgeDef);
				sludge.stackCount = Mathf.Min(amount, sludgeDef.stackLimit);
				GenPlace.TryPlaceThing(sludge, pawn.Position, pawn.Map, ThingPlaceMode.Near);
			});
			yield return empty;
		}
	}
}
