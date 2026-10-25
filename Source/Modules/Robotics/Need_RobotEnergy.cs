using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人电量：满电约行动 12 小时，充电桩 4 小时充满。
	/// 0% 时停止工作并等待充电，不会死亡。
	/// </summary>
	public class Need_RobotEnergy : Need
	{
		/// <summary>每 150 tick 掉 1/200，即 30000 tick（12 小时）耗尽。</summary>
		private const float DrainPerInterval = 1f / 200f;

		public Need_RobotEnergy(Pawn pawn) : base(pawn)
		{
			threshPercents = new List<float> { 0.05f, 0.25f };
		}

		public bool IsFull => CurLevel >= 0.999f;

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
			CurLevel = 1f;
		}

		public void Charge(float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			CurLevel = Mathf.Clamp01(CurLevel + amount);
		}

		public override void NeedInterval()
		{
			if (IsFrozen || IsCharging)
			{
				return;
			}
			CurLevel = Mathf.Max(0f, CurLevel - DrainPerInterval);
		}
	}
}
