#nullable disable

using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace rjw
{
	public class SexFluidDef : Def
	{
		public ThingDef filth;

		public ThingDef consumable;

		public Color color = Color.white;

		public float consumableFluidRatio = 1;

		public bool ingestThroughAnyOrifice;

		public bool alwaysProduceFilth;

		public float baseNutrition;
		
		public float baseNutritionCost = 1;

		public bool quenchesThirst = true;

		public List<string> tags = new List<string>();

		public List<SexFluidIngestionDoer> ingestionDoers;

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string err in base.ConfigErrors())
			{
				yield return err;
			}
			if (filth != null && filth.filth == null)
			{
				yield return $"{nameof(filth)} must be a ThingDef with a <filth> field.";
			}
			if (consumable != null && baseNutrition != 0)
			{
				yield return $"{nameof(consumable)} is set, so {nameof(baseNutrition)} is meaningless.";
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();

			if (consumable != null)
			{
				descriptionHyperlinks ??= new();
				descriptionHyperlinks.AddDistinct(consumable);
			}
		}

	}
}
