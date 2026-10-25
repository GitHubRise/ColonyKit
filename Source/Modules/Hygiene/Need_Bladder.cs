using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public enum BowelCategory : byte
	{
		Empty,
		needBathroom,
		Busting
	}

	/// <summary>DBH Need_Bladder 的移植。</summary>
	public class Need_Bladder : Need
	{
		private bool givenUp;
		private int lastGainTick = -999;
		public int ticksAtZero;

		private float BladderRate => pawn.GetStatValue(HygieneDefOf.CK_BladderRateMultiplier);

		public BowelCategory CurCategory
		{
			get
			{
				if (CurLevel <= 0f) return BowelCategory.Busting;
				if (CurLevel < 0.15f) return BowelCategory.needBathroom;
				return BowelCategory.Empty;
			}
		}

		private float FallPerTick
		{
			get
			{
				float value = 1E-05f;
				Need_Food food = pawn?.needs?.food;
				if (food != null && food.CurLevelPercentage <= 0f)
				{
					value *= 0.15f;
				}
				return value * BladderRate * HygieneSettings.Current.bladderRateMultiplier;
			}
		}

		private bool Urinating => Find.TickManager.TicksGame < lastGainTick + 10;

		public override int GUIChangeArrow => IsFrozen ? 0 : (Urinating ? 1 : -1);

		protected override bool IsFrozen
		{
			get
			{
				if (base.IsFrozen) return true;
				return pawn.Deathresting;
			}
		}

		public Need_Bladder(Pawn pawn) : base(pawn)
		{
			threshPercents = new List<float> { 0.15f };
		}

		public void dump()
		{
			lastGainTick = Find.TickManager.TicksGame;
			CurLevel = Mathf.Min(CurLevel + 0.003f, 1f);
		}

		public void crapPants()
		{
			if (pawn == null) return;
			Need_Hygiene hygiene = pawn.needs?.TryGetNeed<Need_Hygiene>();
			if (hygiene != null) hygiene.CurLevel = 0f;
			CurLevel = 1f;
			if (pawn.needs?.mood != null && pawn.health.capacities.CanBeAwake)
			{
				// 优先用 ColonyKit 自己的 def；若同时装了 DBH，则退回它的同名 def。
				ThoughtDef soiledSelf =
					DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_SoiledSelf")
					?? DefDatabase<ThoughtDef>.GetNamedSilentFail("SoiledSelf");
				if (soiledSelf != null)
				{
					pawn.needs.mood.thoughts.memories.TryGainMemory(soiledSelf);
				}
			}
			ThingDef filth = DefDatabase<ThingDef>.GetNamedSilentFail(
				Rand.Chance(0.5f) ? "FilthFaeces" : "FilthUrine");
			if (filth != null)
			{
				FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, filth);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksAtZero, "ticksAtZero", 0);
		}

		public override void SetInitialLevel()
		{
			CurLevel = DebugSettings.godMode ? 0.1f : 1f;
		}

		public bool GivingUpAndShittingOnTheFloorFunction() => givenUp;

		public override void NeedInterval()
		{
			if (IsFrozen) return;
			CurLevel -= FallPerTick * 150f;
			if (CurLevel < 0f) CurLevel = 0f;
			MentalState mental = pawn.MentalState;
			if (pawn.Spawned && mental != null
				&& (mental.def == MentalStateDefOf.PanicFlee
					|| mental.def == MentalStateDefOf.Wander_Psychotic))
			{
				if (CurLevel <= 0f || (CurLevel <= 0.4f && Rand.Chance(0.05f)))
				{
					crapPants();
				}
			}
			if (CurCategory >= BowelCategory.needBathroom && pawn.CurrentBed() != null
				&& pawn.health.capacities.CanBeAwake
				&& (HealthAIUtility.ShouldSeekMedicalRest(pawn)
					|| HealthAIUtility.ShouldSeekMedicalRestUrgent(pawn)
					|| pawn.Downed
					|| pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.3f
					|| pawn.CurJob?.playerForced == true))
			{
				CurLevel = 1f;
				return;
			}
			if (CurCategory >= BowelCategory.Busting)
			{
				ticksAtZero += 150;
			}
			else
			{
				givenUp = false;
				ticksAtZero = 0;
			}
			if (ticksAtZero > 1000 && pawn.Spawned)
			{
				float mtb = pawn.RaceProps.Animal ? 0.001f
					: ticksAtZero < 15000 ? 0.25f
					: ticksAtZero < 30000 ? 0.125f
					: ticksAtZero >= 45000 ? 0.0625f : 1f / 12f;
				if (Rand.MTBEventOccurs(mtb, 60000f, 150f))
				{
					givenUp = true;
				}
				if (givenUp && (!pawn.health.capacities.CanBeAwake || pawn.Downed))
				{
					crapPants();
				}
			}
		}
	}
}
