using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 排空水箱：走到储水设施旁边，以 50 L/s 的速度把水放到脚下并转成地面污染，
	/// 排空后删掉 <c>CK_DrainTankDes</c> 标记（DBH CompWaterStorage.drain 的渐进版）。
	/// </summary>
	public class JobDriver_DrainWater : JobDriver
	{
		private const TargetIndex TankInd = TargetIndex.A;

		private const float LitersPerSecond = 50f;

		private const float TicksPerSecond = 60f;

		/// <summary>每升水折合多少污染值；1000 L 大致弄脏 10 点。</summary>
		private const float PollutionPerLiter = 0.01f;

		private Thing Tank => job.GetTarget(TankInd).Thing;

		private CompWaterStorage Storage => Tank?.TryGetComp<CompWaterStorage>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TankInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TankInd);
			yield return Toils_Goto.GotoThing(TankInd, PathEndMode.Touch)
				.FailOnDespawnedOrNull(TankInd);

			Toil drain = ToilMaker.MakeToil("CK_DrainWater");
			drain.defaultCompleteMode = ToilCompleteMode.Never;
			drain.socialMode = RandomSocialMode.Quiet;
			drain.FailOnDespawnedOrNull(TankInd);
			drain.PlaySustainerOrSound(
				() => DefDatabase<SoundDef>.GetNamedSilentFail("CK_DrainHole"));
			drain.WithProgressBar(TankInd, () =>
			{
				CompWaterStorage storage = Storage;
				return storage == null ? 1f : 1f - storage.CapPercent;
			});
			drain.tickIntervalAction = delta =>
			{
				CompWaterStorage storage = Storage;
				if (storage == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float amount = Mathf.Min(storage.WaterStorage, LitersPerSecond / TicksPerSecond * delta);
				if (amount <= 0.0001f)
				{
					ReadyForNextToil();
					return;
				}
				storage.WaterStorage = Mathf.Max(0f, storage.WaterStorage - amount);
				pawn.Map?.GetComponent<MapComponent_SewagePollution>()
					?.AddPollution(pawn.Position, amount * PollutionPerLiter);
				if (storage.WaterStorage <= 0.01f)
				{
					storage.WaterStorage = 0f;
					ReadyForNextToil();
				}
			};
			yield return drain;
			yield return new Toil
			{
				initAction = FinishDrain,
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		private void FinishDrain()
		{
			Thing tank = Tank;
			if (tank?.Map != null)
			{
				DesignationDef def = DefDatabase<DesignationDef>.GetNamedSilentFail("CK_DrainTankDes");
				if (def != null)
				{
					tank.Map.designationManager.DesignationOn(tank, def)?.Delete();
				}
			}
			CompWaterStorage storage = Storage;
			if (storage != null)
			{
				storage.WaterStorage = 0f;
			}
		}
	}
}
