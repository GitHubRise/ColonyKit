using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class JobDriver_EmptySepticTank : JobDriver
	{
		/// <summary>每次 tick 抽多少升，和 DBH 一致。</summary>
		private const float DrainedPerTick = 25f;

		/// <summary>本次已抽走的污水量，收尾时按它掉粪渣。</summary>
		private float drainedTotal;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil empty = new Toil
			{
				defaultDuration = 600,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			empty.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			empty.tickAction = () =>
			{
				CompSewageHandler comp = job.targetA.Thing.TryGetComp<CompSewageHandler>();
				if (comp == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				drainedTotal += comp.Empty(DrainedPerTick);
				if (comp.sewageBuffer <= 0.01f)
				{
					ReadyForNextToil();
				}
			};
			// 和旱厕一样把抽出来的污水变成粪渣：否则化粪池只会把污水凭空销毁，
			// 燃烧坑和堆肥机就只剩旱厕一条供料渠道。
			empty.AddFinishAction(SpawnSludge);
			yield return empty;
		}

		private void SpawnSludge()
		{
			Map map = pawn?.Map;
			if (map == null || drainedTotal < 1f)
			{
				return;
			}
			ThingDef sludgeDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_FecalSludge");
			if (sludgeDef == null)
			{
				return;
			}
			int remaining = Mathf.CeilToInt(drainedTotal);
			while (remaining > 0)
			{
				int count = Mathf.Min(remaining, sludgeDef.stackLimit);
				Thing sludge = ThingMaker.MakeThing(sludgeDef);
				sludge.stackCount = count;
				GenPlace.TryPlaceThing(sludge, pawn.Position, map, ThingPlaceMode.Near);
				remaining -= count;
			}
		}
	}
}
