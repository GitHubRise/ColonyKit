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
			CurLevel = Mathf.Max(0f, CurLevel - MaxLevel * DrainPerInterval);
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
