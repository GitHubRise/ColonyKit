using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_SewageHandler : CompProperties
	{
		public float capacity = 10000f;
		public int priority;

		public CompProperties_SewageHandler()
		{
			compClass = typeof(CompSewageHandler);
		}
	}

	/// <summary>DBH 污水缓冲子集：化粪池/污水处理器使用。</summary>
	public class CompSewageHandler : ThingComp
	{
		public bool Blocked;
		public float sewageBuffer;
		private CompPowerTrader power;
		private CompRefuelable fuel;
		private CompBreakdownable breakdown;

		public CompProperties_SewageHandler Props => (CompProperties_SewageHandler)props;

		public bool WorkingNow
		{
			get
			{
				if (power != null && !power.PowerOn) return false;
				if (fuel != null && !fuel.HasFuel) return false;
				if (breakdown != null && breakdown.BrokenDown) return false;
				return true;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.GetComp<CompPowerTrader>();
			fuel = parent.GetComp<CompRefuelable>();
			breakdown = parent.TryGetComp<CompBreakdownable>();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref Blocked, "Blocked", false);
			Scribe_Values.Look(ref sewageBuffer, "sewageBuffer", 0f);
		}

		public float Empty(float amount)
		{
			float drained = Mathf.Min(sewageBuffer, amount);
			sewageBuffer -= drained;
			if (sewageBuffer < 0.01f)
			{
				sewageBuffer = 0f;
				Blocked = false;
			}
			return drained;
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			if (!Blocked && sewageBuffer >= Props.capacity)
			{
				Blocked = true;
				SpillSewage();
			}
		}

		private void SpillSewage()
		{
			if (parent?.Map == null)
			{
				return;
			}
			// 和 Need_Bladder 一致：优先本包定义的污渍，未定义时退回 DBH 原名。
			ThingDef filth = DefDatabase<ThingDef>.GetNamedSilentFail("CK_FilthFaeces")
				?? DefDatabase<ThingDef>.GetNamedSilentFail("FilthFaeces");
			SoundDef spillSound = DefDatabase<SoundDef>.GetNamedSilentFail("CK_SewageStart");
			if (spillSound != null)
			{
				spillSound.PlayOneShot(parent);
			}
			if (filth == null)
			{
				return;
			}
			foreach (IntVec3 cell in parent.OccupiedRect())
			{
				FilthMaker.TryMakeFilth(cell, parent.Map, filth);
				parent.Map.GetComponent<MapComponent_SewagePollution>()?.AddPollution(cell, 5f);
			}
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.Sewage".Translate(sewageBuffer.ToString("F0"), Props.capacity.ToString("F0")).ToString()
				+ (Blocked ? "\n" + "CK.Hygiene.SewageBlocked".Translate().ToString() : string.Empty);
		}
	}
}
