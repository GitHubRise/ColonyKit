using RimWorld;
using Verse;

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

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.Sewage".Translate(sewageBuffer.ToString("F0"), Props.capacity.ToString("F0")).ToString()
				+ (Blocked ? "\n" + "CK.Hygiene.SewageBlocked".Translate().ToString() : string.Empty);
		}
	}
}
