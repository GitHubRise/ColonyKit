#nullable disable

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw
{
	public class Cocoon : HediffWithComps
	{
		public int tickNext;

		public override void PostMake()
		{
			Severity = 1.0f;
			SetNextTick();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref tickNext, "tickNext", 1000, true);
		}

		public override void Tick()
		{
			if (Find.TickManager.TicksGame >= tickNext)
			{
				//ModLog.Message("Cocoon::Tick() " + base.xxx.get_pawnname(pawn));
				HealWounds();
				SatisfyHunger();
				SatisfyThirst();
				NoSleep();
				SetNextTick();
			}
		}

		public void HealWounds()
		{
			IEnumerable<Hediff> enumerable = from hd in pawn.health.hediffSet.hediffs
											 where !hd.IsTended() && hd.TendableNow()
											 select hd;
			if (enumerable != null)
			{
				foreach (Hediff item in enumerable)
				{
					HediffWithComps val = item as HediffWithComps;
					if (val != null)
						if (val.Bleeding)
						{
							//ModLog.Message("TrySealWounds " + xxx.get_pawnname(pawn) + ", Bleeding " + item.Label);
							//HediffComp_TendDuration val2 = HediffUtility.TryGetComp<HediffComp_TendDuration>(val);
							val.Heal(0.5f);
							//val2.tendQuality = 1f;
							//val2.tendTicksLeft = 10000;
							//pawn.health.Notify_HediffChanged(item);
						}
						// 治疗感染
						// 治疗危及生命的慢性病
						else if ((!val.def.chronic && val.def.lethalSeverity > 0f) || (val.CurStage?.lifeThreatening ?? false))
						{
							//ModLog.Message("TryHeal " + xxx.get_pawnname(pawn) + ", infection(?) " + item.Label);
							HediffComp_TendDuration val2 = HediffUtility.TryGetComp<HediffComp_TendDuration>(val);
							val2.tendQuality = 1f;
							val2.tendTicksLeft = 10000;
							pawn.health.Notify_HediffChanged(item);
						}
				}
			}
		}

		public void SatisfyHunger()
		{
			Need_Food need = pawn.needs.food;
			if (need == null)
			{
				return;
			}
			//pawn.PositionHeld.IsInPrisonCell(pawn.Map)
			//ModLog.Message("Cocoon::SatisfyHunger() " + xxx.get_pawnname(pawn) + " IsInPrisonCell " + pawn.PositionHeld.IsInPrisonCell(pawn.Map));
			//ModLog.Message("Cocoon::SatisfyHunger() " + xxx.get_pawnname(pawn) + " GetRoom " + pawn.PositionHeld.GetRoom(pawn.Map));
			//ModLog.Message("Cocoon::SatisfyHunger() " + xxx.get_pawnname(pawn) + " GetRoom " + pawn.PositionHeld.GetZone(pawn.Map));

			if (need.CurLevel < 0.15f)
			{
				//ModLog.Message("Cocoon::SatisfyHunger() " + xxx.get_pawnname(pawn) + " need to eat");
				float nutrition_amount = need.MaxLevel / 5f;
				need.CurLevel += nutrition_amount;
			}
		}

		public void SatisfyThirst()
		{
			if (!xxx.DubsBadHygieneIsActive)
				return;

			Need need = pawn.needs.AllNeeds.Find(x => x.def == xxx.DBHThirst);
			if (need == null)
			{
				return;
			}

			if (need.CurLevel < 0.15f)
			{
				//ModLog.Message("Cocoon::SatisfyThirst() " + xxx.get_pawnname(pawn) + " need to drink");
				float nutrition_amount = need.MaxLevel / 5f;
				pawn.needs.TryGetNeed(need.def).CurLevel += nutrition_amount;
			}
		}

		public void NoSleep()
		{
			Need_Rest need = pawn.needs.rest;
			if (need == null)
			{
				return;
			}
			if (need.CurLevel < 0.10f)
			{
				need.CurLevel = 0.20f;
			}
		}

		public void SetNextTick()
		{
			//实际每 16.6 秒执行一次 tick
			tickNext = Find.TickManager.TicksGame + 1000;
			//ModLog.Message("Cocoon::SetNextTick() " + tickNext);
		}
	}
}
