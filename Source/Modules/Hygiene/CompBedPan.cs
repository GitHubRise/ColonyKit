using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_BedPan : CompProperties
	{
		public CompProperties_BedPan()
		{
			compClass = typeof(CompBedPan);
		}
	}

	/// <summary>
	/// 便盆：卧床病人用完后 <see cref="used"/> 为 true，需要有人搬到水源清洗后才能再用。
	/// D 组的 Need_Bladder 卧床分支会找这里的 Comp：房间里有干净的便盆就直接用掉。
	/// </summary>
	public class CompBedPan : ThingComp
	{
		public bool used;

		public void Use()
		{
			used = true;
		}

		public void Clean()
		{
			used = false;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref used, "used", false);
		}

		public override string CompInspectStringExtra()
		{
			return used ? "CK.Hygiene.BedPanUsed".Translate() : "CK.Hygiene.BedPanClean".Translate();
		}
	}
}
