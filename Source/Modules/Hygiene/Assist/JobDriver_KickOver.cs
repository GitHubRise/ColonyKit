using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 倒掉粪渣桶：走到桶边踢 60 tick，然后在原地把桶里的污水算进污染网格、
	/// 留下原污水污渍并销毁桶（DBH JobDriver_TipOverSewage 的移植）。
	/// </summary>
	public class JobDriver_KickOver : JobDriver
	{
		private const TargetIndex ItemInd = TargetIndex.A;

		private const int TipTicks = 60;

		/// <summary>每桶粪渣折合多少污染值；最多 20（单格上限）。</summary>
		private const float PollutionPerUnit = 0.2f;

		private Thing Item => job.GetTarget(ItemInd).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(ItemInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(ItemInd);
			yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch)
				.FailOnDespawnedOrNull(ItemInd);

			Toil tip = Toils_General.Wait(TipTicks);
			tip.FailOnDespawnedOrNull(ItemInd);
			tip.FailOnCannotTouch(ItemInd, PathEndMode.ClosestTouch);
			tip.WithProgressBarToilDelay(ItemInd);
			yield return tip;

			yield return new Toil
			{
				initAction = Dump,
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		private void Dump()
		{
			Thing item = Item;
			if (item == null || !item.Spawned)
			{
				return;
			}
			Map map = item.Map;
			IntVec3 cell = item.Position;
			int count = Mathf.Max(1, item.stackCount);

			DesignationDef def = DefDatabase<DesignationDef>.GetNamedSilentFail("CK_KickOverDes");
			if (def != null)
			{
				map.designationManager.DesignationOn(item, def)?.Delete();
			}
			item.Destroy();

			map.GetComponent<MapComponent_SewagePollution>()
				?.AddPollution(cell, Mathf.Min(20f, count * PollutionPerUnit));
			ThingDef filth = DefDatabase<ThingDef>.GetNamedSilentFail("CK_RawSewage");
			if (filth != null)
			{
				FilthMaker.TryMakeFilth(cell, map, filth);
			}
		}
	}
}
