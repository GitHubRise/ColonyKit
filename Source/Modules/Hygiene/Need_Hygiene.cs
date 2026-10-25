using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Need_Hygiene 的逐行移植，后续可继续接入 Hediff/污染系统。</summary>
	public class Need_Hygiene : Need
	{
		private const float BadHygienePerInterval = 8.333333E-05f;

		private bool contaminated;
		public float FilthyRoomMulti = 1f;
		public float HeatMulti = 1.15f;
		public bool InRain;
		private int lastGainTick = -999;
		public float multi21 = 1f;
		public float RainMulti = 1.15f;
		private int ticksAtZero;
		public bool TooHot;

		private float HygieneRate => pawn.GetStatValue(HygieneDefOf.CK_HygieneRateMultiplier);

		public bool IsContaminated => contaminated;

		public HygieneCategory CurCategory
		{
			get
			{
				if (CurLevel <= 0f) return HygieneCategory.Filthy;
				if (CurLevel < 0.1f) return HygieneCategory.Grimy;
				if (CurLevel < 0.7f) return HygieneCategory.Fine;
				if (CurLevel < 0.9f) return HygieneCategory.Clean;
				return HygieneCategory.Squeaky;
			}
		}

		private bool Cleaning => Find.TickManager.TicksGame < lastGainTick + 10;

		public float FallRateMulti21
		{
			get
			{
				multi21 = 1f;
				if (!pawn.Spawned) return multi21;
				if (pawn.CurJob != null && (int)pawn.CurJob.locomotionUrgency > 2)
				{
					multi21 *= 2f;
				}
				TooHot = pawn.AmbientTemperature > pawn.ComfortableTemperatureRange().max;
				if (TooHot)
				{
					HeatMulti = GenMath.LerpDoubleClamped(25f, 50f, 1f, 1.25f, pawn.AmbientTemperature);
					multi21 *= HeatMulti;
				}
				InRain = false;
				if (!pawn.Position.Roofed(pawn.Map))
				{
					InRain = pawn.Map.weatherManager.CurWeatherPerceived.overlayClasses.Contains(typeof(WeatherOverlay_Rain));
				}
				if (InRain)
				{
					multi21 *= RainMulti;
				}
				return multi21;
			}
		}

		private float FallPerTick => 5E-06f * HygieneRate * FallRateMulti21;

		public override int GUIChangeArrow
		{
			get
			{
				if (IsFrozen) return 0;
				return Cleaning ? 1 : -1;
			}
		}

		protected override bool IsFrozen
		{
			get
			{
				if (base.IsFrozen) return true;
				return pawn.Deathresting;
			}
		}

		public Need_Hygiene(Pawn pawn) : base(pawn)
		{
			threshPercents = new List<float> { 0.1f, 0.7f, 0.9f };
		}

		public void clean(float val)
		{
			lastGainTick = Find.TickManager.TicksGame;
			CurLevel = Mathf.Min(CurLevel + val, 1f);
		}

		public void Contaminated()
		{
			contaminated = true;
		}

		public void CleanContamination()
		{
			contaminated = false;
		}

		public override void SetInitialLevel()
		{
			CurLevel = Rand.Range(0.5f, 0.8f);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref contaminated, "Contamination", defaultValue: false);
			Scribe_Values.Look(ref ticksAtZero, "ticksAtZero", 0);
		}

		public override void NeedInterval()
		{
			if (IsFrozen)
			{
				lastGainTick = Find.TickManager.TicksGame;
				return;
			}
			if (!Cleaning)
			{
				CurLevel -= FallPerTick * 150f;
				CurLevel = Mathf.Clamp01(CurLevel);
			}

			HediffDef badHygiene = HygieneDefOf.CK_BadHygiene;
			if (badHygiene != null)
			{
				HealthUtility.AdjustSeverity(pawn, badHygiene,
					CurCategory == HygieneCategory.Filthy ? BadHygienePerInterval : -0.25f);
			}

			if (IsContaminated)
			{
				ticksAtZero += 150;
			}
			else
			{
				ticksAtZero = 0;
			}
			if (ticksAtZero > 1000 && pawn.Spawned)
			{
				float mtb = ticksAtZero < 80000 ? 0.25f
					: ticksAtZero < 100000 ? 0.125f
					: ticksAtZero >= 120000 ? 0.0625f : 1f / 12f;
				if (ticksAtZero > 50000 && Rand.MTBEventOccurs(mtb, 120000f, 150f)
					&& pawn.health.hediffSet.HasHediff(badHygiene, mustBeVisible: true))
				{
					SanitationUtil.GiveNastyGutBug(pawn, ContaminationLevel.Untreated);
				}
			}
		}

		public override string GetTipString()
		{
			string tip = base.GetTipString();
			tip += "\n\n" + "CK.Hygiene.FilthyRoom".Translate(FilthyRoomMulti.ToString("0.00"));
			if (TooHot)
			{
				tip += "\n" + "CK.Hygiene.TooHot".Translate(HeatMulti.ToString("0.00"));
			}
			if (InRain)
			{
				tip += "\n" + "CK.Hygiene.InRain".Translate(RainMulti.ToString("0.00"));
			}
			tip += "\n" + "CK.Hygiene.TotalRate".Translate(multi21.ToString("0.00"));
			if (DebugSettings.godMode)
			{
				tip += "\nContaminated: " + IsContaminated;
			}
			return tip;
		}
	}
}
