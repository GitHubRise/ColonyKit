using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_BasinWater : CompProperties
	{
		public float Capacity = 30f;

		public CompProperties_BasinWater()
		{
			compClass = typeof(CompBasinWater);
		}
	}

	/// <summary>便携脸盆里的水量（升）。脸盆作为武器携带，右键水井打水、右键水箱倒水。</summary>
	public class CompBasinWater : ThingComp
	{
		public float Water;

		public CompProperties_BasinWater Props => (CompProperties_BasinWater)props;

		public bool IsFull => Water >= Props.Capacity - 0.01f;

		public bool IsEmpty => Water <= 0.01f;

		public float Space => Props.Capacity - Water;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref Water, "Water", 0f);
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.BasinWater".Translate(Water.ToString("F0"), Props.Capacity.ToString("F0"));
		}
	}
}
