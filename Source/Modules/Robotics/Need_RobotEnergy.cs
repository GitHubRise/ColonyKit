using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人电池：容量由设置页决定（默认 4000），满电约行动 12 小时，充电桩 4 小时充满。
	/// 0% 时停止工作并等待充电，不会死亡。
	/// </summary>
	public class Need_RobotEnergy : Need
	{
		/// <summary>每 150 tick 掉满电的 1/200，即 30000 tick（12 小时）耗尽。</summary>
		private const float DrainPerInterval = 1f / 200f;

		private bool capacityMigrated;

		public Need_RobotEnergy(Pawn pawn) : base(pawn)
		{
			threshPercents = new List<float> { 0.05f, 0.25f };
		}

		public override float MaxLevel =>
			Mathf.Max(1f, NannyBotUtility.Settings.batteryCapacity);

		public bool IsFull => CurLevel >= MaxLevel * 0.999f;

		public bool IsCharging =>
			pawn?.CurJob != null && pawn.CurJob.def == NannyBotDefOf.CK_NannyCharge;

		public override int GUIChangeArrow
		{
			get
			{
				if (IsFrozen)
				{
					return 0;
				}
				return IsCharging ? 1 : -1;
			}
		}

		public override void SetInitialLevel()
		{
			CurLevel = MaxLevel;
		}

		public void Charge(float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			CurLevel = Mathf.Clamp(CurLevel + amount, 0f, MaxLevel);
		}

		public override void NeedInterval()
		{
			if (IsFrozen || IsCharging)
			{
				return;
			}
			float drain = MaxLevel * DrainPerInterval;
			float passive = PassiveRechargePerInterval();
			CurLevel = Mathf.Clamp(CurLevel - drain + passive, 0f, MaxLevel);
		}

		/// <summary>Mk-V 在露天时按光照和风速被动吸收太阳能/风能；充电桩充电期间不叠加。</summary>
		private float PassiveRechargePerInterval()
		{
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Map == null || !pawn.Spawned
				|| pawn.Map.roofGrid.Roofed(pawn.Position))
			{
				return 0f;
			}

			RoboticsSettings settings = NannyBotUtility.Settings;
			Map map = pawn.Map;
			float gain = 0f;
			float conversion = Mathf.Clamp(settings.mkvPowerConversionRate / 100f, 0.1f, 3f);
			const float PassiveShareOfStation = 0.4f;
			const float FullChargeTicks = 10000f;

			if (settings.mkvSolarCharging && settings.mkvSolarEfficiency > 0f)
			{
				float light = map.glowGrid.GroundGlowAt(pawn.Position);
				float factor = ThresholdFactor(light, settings.mkvLightThreshold);
				gain += MaxLevel * (PassiveShareOfStation / FullChargeTicks) * 150f
					* conversion * (settings.mkvSolarEfficiency / 100f) * factor;
			}
			if (settings.mkvWindCharging && settings.mkvWindEfficiency > 0f)
			{
				float wind = Mathf.Clamp01(map.weatherManager.CurWindSpeedFactor);
				float factor = ThresholdFactor(wind, settings.mkvWindThreshold);
				gain += MaxLevel * (PassiveShareOfStation / FullChargeTicks) * 150f
					* conversion * (settings.mkvWindEfficiency / 100f) * factor;
			}
			return gain;
		}

		private static float ThresholdFactor(float value, float threshold)
		{
			threshold = Mathf.Clamp01(threshold);
			if (value <= threshold)
			{
				return 0f;
			}
			return Mathf.Clamp01((value - threshold) / Mathf.Max(0.01f, 1f - threshold));
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref capacityMigrated, "ck_robotics_capacityMigrated", false);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && !capacityMigrated)
			{
				// 旧版电量是 0–1；第一次升级到容量制时按百分比换算。
				if (MaxLevel > 1f && CurLevel <= 1.01f)
				{
					CurLevel *= MaxLevel;
				}
				capacityMigrated = true;
			}
		}

		public override string GetTipString()
		{
			string text = base.GetTipString();
			text += "\n" + "CK.Robotics.EnergyTip".Translate(
				CurLevel.ToString("0"),
				MaxLevel.ToString("0"));
			return text;
		}
	}
}
