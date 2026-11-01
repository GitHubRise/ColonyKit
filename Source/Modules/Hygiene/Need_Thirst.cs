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
	/// 见底后累积脱水 hediff，最终致死；喝水按「每升解渴」折算（默认 0.65）。
	/// 动物（<see cref="RaceProps.Animal"/>）默认只按 50% 速度口渴，免得野生动物频繁渴死。
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
				float rate = stat == null ? 1f : pawn.GetStatValue(stat);
				if (pawn.RaceProps.Animal)
				{
					rate *= HygieneSettings.Current.animalThirstRate;
				}
				return rate;
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

		/// <summary>
		/// 基准口渴速度：设置里的「口渴耗尽时间」按初始 80% → 空计，
		/// 所以每 tick 掉 1 / (小时数 × 2500)；Hydrated 阶段跑满这一速率。
		/// </summary>
		private static float BaseFallPerTick
		{
			get
			{
				float hours = Mathf.Clamp(
					HygieneSettings.Current.thirstHoursToEmpty,
					HygieneSettings.MinThirstHours,
					HygieneSettings.MaxThirstHours);
				return 1f / (hours * 2500f);
			}
		}

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
			float rate = BaseFallPerTick * ThirstRate;
			switch (cat)
			{
			case ThirstCategory.Hydrated:
				return rate;
			case ThirstCategory.Thirsty:
				return rate * 0.5f;
			case ThirstCategory.Parched:
				return rate * 0.15f;
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
