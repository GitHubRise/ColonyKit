#nullable disable

using System.Collections.Generic;
using Verse;

namespace rjw
{
	public class CompProperties_ThingBodyPart : CompProperties
	{
		public HediffDef_SexPart hediffDef;

		public bool guessHediffFromThingDef;

		public CompProperties_ThingBodyPart()
		{
			compClass = typeof(CompThingBodyPart);
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			foreach (string err in base.ConfigErrors(parentDef))
			{
				yield return err;
			}
			if (hediffDef == null && !guessHediffFromThingDef)
			{
				yield return "<hediffDef> must not be null.";
			}
		}

		// TODO: 别再根据 defName 猜测状态了！
		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);

			if (hediffDef == null && guessHediffFromThingDef)
			{
				hediffDef = (HediffDef_SexPart)HediffDef.Named(parentDef.defName);
			}
		}
	}
}
