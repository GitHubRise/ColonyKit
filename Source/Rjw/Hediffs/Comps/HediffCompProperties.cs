#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	public class HediffCompProperties_SexPart : HediffCompProperties
	{		
		public HediffCompProperties_SexPart()
		{
			compClass = typeof(HediffComp_SexPart);
		}
	}

	public class HediffCompProperties_Ovipositor : HediffCompProperties
	{

		// 目前未使用，改为搜索每个蛋定义的 parentDefs，应重构
		public List<HediffDef_InsectEgg> eggs;

		public IntRange eggIntervalTicks = new(GenDate.TicksPerDay, GenDate.TicksPerDay);

		public FloatRange eggCount = FloatRange.One;

		public HediffCompProperties_Ovipositor()
		{
			compClass = typeof(HediffComp_Ovipositor);
		}

		public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
		{
			foreach (string err in base.ConfigErrors(parentDef))
			{
				yield return err;
			}
			// if (eggs.NullOrEmpty())
			// {
			// 	yield return "One or more <eggs> is required";
			// }
		}
	}

	public class HediffCompProperties_GrowsWithOwner : HediffCompProperties
	{
		public bool evenIfTransplanted;

		public HediffCompProperties_GrowsWithOwner()
		{
			compClass = typeof(HediffComp_GrowsWithOwner);
		}
	}
}
