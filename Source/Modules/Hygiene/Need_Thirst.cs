using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public enum ThirstCategory
	{
		Hydrated,
		Thirsty,
		Parched
	}

	/// <summary>
	/// 口渴（DBH Need_Thirst 的移植）：和饥饿一样按天掉，温度越高掉得越快；
	/// 见底后累积脱水 hediff，最终致死；喝一次 +0.6%。
	/// </summary>
	public class Need_Thirst : Need
	{
		private int lastGainTick = -999;

		private static StatDef ThirstRateStatDef => DefDatabase<StatDef>.GetNamedSilentFail("CK_ThirstRateMultiplier");

		private static HediffDef DehydrationDef => DefDatabase<HediffDef>.GetNamedSilentFail("CK_Dehydration");

		private float ThirstRate
		{
			get
			{
				StatDef stat = ThirstRateStatDef;
				return stat == null ? 1f : pawn.GetStatValue(stat);
			}
		}

		public ThirstCategory CurCategory
		{
			get
			{
				if (CurLevel <= 0f)
				{
					return ThirstCategory.Parched;
				}
				if (CurLevel < 0.2f)
				{
					return ThirstCategory.Thirsty;
				}
				return ThirstCategory.Hydrated;
			}
		}

		/// <summary>高温加倍口渴速度：25℃ 是 1 倍，50℃ 是 3 倍。</summary>
		private float TemperatureFactor =>
			GenMath.LerpDoubleClamped(25f, 50f, 1f, 3f, pawn.AmbientTemperature);

		private float FallPerTick => FallPerTickAssumingCategory(CurCategory) * TemperatureFactor;

		private bool Drinking => Find.TickManager.TicksGame < lastGainTick + 10;

		private float DehydrationSeverityPerInterval =>
			0.0011333333f * Mathf.Lerp(0.8f, 1.2f, Rand.ValueSeeded(pawn.thingIDNumber ^ 0x26EF7A)) * 1.1f;

		public override int GUIChangeArrow
		{
			get
			{
				if (IsFrozen)
				{
					return 0;
				}
				return Drinking ? 1 : -1;
			}
		}

		protected override bool IsFrozen => base.IsFrozen || pawn.Deathresting;

		public Need_Thirst(Pawn pawn) : base(pawn)
		{
			threshPercents = new List<float> { 0.2f };
		}

		private float FallPerTickAssumingCategory(ThirstCategory cat)
		{
			switch (cat)
			{
			case ThirstCategory.Hydrated:
				return 2.6666667E-05f * ThirstRate;
			case ThirstCategory.Thirsty:
				return 2.6666667E-05f * ThirstRate * 0.5f;
			case ThirstCategory.Parched:
				return 2.6666667E-05f * ThirstRate * 0.15f;
			default:
				return 999f;
			}
		}

		public void Drink()
		{
			Drink(0.006f);
		}

		public void Drink(float val)
		{
			lastGainTick = Find.TickManager.TicksGame;
			CurLevel = Mathf.Min(CurLevel + val, 1f);
		}

		public override void SetInitialLevel()
		{
			CurLevel = Rand.Range(0.8f, 1f);
		}

		public override void NeedInterval()
		{
			if (IsFrozen)
			{
				lastGainTick = Find.TickManager.TicksGame;
				return;
			}
			HediffDef dehydration = DehydrationDef;
			if (dehydration != null)
			{
				float delta = CurCategory == ThirstCategory.Parched
					? DehydrationSeverityPerInterval
					: -DehydrationSeverityPerInterval;
				HealthUtility.AdjustSeverity(pawn, dehydration, delta);
			}
			float fall = FallPerTick * 150f;
			CurLevel -= fall;
			if (fall < 0f)
			{
				lastGainTick = Find.TickManager.TicksGame + 150;
			}
			CurLevel = Mathf.Clamp01(CurLevel);
		}
	}
}
