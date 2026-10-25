using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_Blockage : CompProperties
	{
		public CompProperties_Blockage()
		{
			compClass = typeof(CompBlockage);
		}
	}

	/// <summary>DBH CompBlockage 的淋浴子集：堵塞状态、随机堵塞、修复。</summary>
	public class CompBlockage : ThingComp
	{
		public bool blocked;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref blocked, "blocked", false, true);
		}

		public void CheckForBlockage()
		{
			if (!blocked && Rand.MTBEventOccurs(5f, 60000f, 1000f))
			{
				blocked = true;
			}
		}

		public void Notify_Repaired()
		{
			blocked = false;
		}

		public void DoBreakdown()
		{
			blocked = true;
		}

		public override string CompInspectStringExtra()
		{
			return blocked ? "CK.Hygiene.Blocked".Translate() : null;
		}
	}
}
