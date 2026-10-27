using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 充电：走到充电桩后持续充能，充满或断电结束。
	/// 4 小时充满（10000 tick）；充电时必须通电。
	/// </summary>
	public class JobDriver_NannyCharge : JobDriver
	{
		private const TargetIndex StationInd = TargetIndex.A;
		private const float ChargePerTick = 1f / 10000f;

		private Building Station => job.GetTarget(StationInd).Thing as Building;

		private Need_RobotEnergy Energy => pawn.needs?.TryGetNeed<Need_RobotEnergy>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(StationInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(StationInd);

			yield return Toils_Goto.GotoThing(StationInd, PathEndMode.InteractionCell);

			Toil charge = ToilMaker.MakeToil("CK_NannyCharge");
			charge.defaultCompleteMode = ToilCompleteMode.Never;
			charge.FailOnDespawnedNullOrForbidden(StationInd);
			CompNannyBot bot = pawn.TryGetComp<CompNannyBot>();
			charge.initAction = () =>
			{
				if (bot != null && NannyBotUtility.IsMkV(pawn))
				{
					bot.lowPowerStandby = true;
				}
			};
			charge.tickIntervalAction = delta =>
			{
				Building station = Station;
				Need_RobotEnergy energy = Energy;
				if (station == null || energy == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}

				CompPowerTrader power = station.TryGetComp<CompPowerTrader>();
				if (power == null || !power.PowerOn)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}

				station.TryGetComp<CompNannyBotStation>()?.SetCharging(true);
				RoboticsSettings settings = NannyBotUtility.Settings;
				bool isMkV = NannyBotUtility.IsMkV(pawn);
				float efficiency = UnityEngine.Mathf.Clamp(
					(isMkV ? settings.mkvChargeEfficiency : settings.chargeEfficiency) / 100f, 0.1f, 10f);
				float conversion = UnityEngine.Mathf.Clamp(
					(isMkV ? settings.mkvPowerConversionRate : settings.powerConversionRate) / 100f, 0.1f, 10f);
				float powerFactor = UnityEngine.Mathf.Clamp(
					(isMkV ? settings.mkvChargePower : settings.chargePower) / 1000f, 0.1f, 10f);
				if (isMkV)
				{
					conversion *= 1.2f;
				}
				energy.Charge(delta * energy.MaxLevel * ChargePerTick * efficiency * conversion * powerFactor);
				if (energy.IsFull)
				{
					ReadyForNextToil();
				}
			};
			charge.AddFinishAction(() =>
			{
				Station?.TryGetComp<CompNannyBotStation>()?.SetCharging(false);
				if (bot != null)
				{
					bot.lowPowerStandby = false;
				}
			});
			charge.WithProgressBar(StationInd, () => Energy?.CurLevelPercentage ?? 0f);
			yield return charge;
		}
	}
}
