#nullable enable

using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace rjw.Modules.Attraction
{
	public class FloatMenuOptionProvider_AttractionDebug : FloatMenuOptionProvider
	{
		protected override bool Drafted => true;
		protected override bool Undrafted => true;
		protected override bool Multiselect => false;

		protected override bool AppliesInt(FloatMenuContext context)
		{
			if (RJWSettings.DebugAttraction)
			{
				return Prefs.DevMode && context.FirstSelectedPawn.HasComp<CompRJW>();
			}
			return false;
		}

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
		{
			if (!clickedPawn.HasComp<CompRJW>())
			{
				yield break;
			}

			var purposes = (AttractionPurpose[])Enum.GetValues(typeof(AttractionPurpose));

			yield return BuildExplainOption(purposes, context.FirstSelectedPawn, clickedPawn);
			yield return BuildExplainOption(purposes, clickedPawn, context.FirstSelectedPawn);
		}

		private FloatMenuOption BuildExplainOption(AttractionPurpose[] purposes, Pawn observer, Pawn target)
		{
			IEnumerable<FloatMenuOption> purposeList = BuildPurposeOptions(purposes, observer, target);
			FloatMenuOption explain = new(
				"AttractionDebug_Explain".Translate(observer, target),
				() => FloatMenuUtility.MakeMenu(purposeList, opt => opt.Label, opt => opt.action),
				MenuOptionPriority.VeryLow,
				null,
				null);
			return explain;
		}

		private IEnumerable<FloatMenuOption> BuildPurposeOptions(AttractionPurpose[] purposes, Pawn observer, Pawn target)
		{
			for (int i = 0; i < purposes.Length; i++)
			{
				AttractionPurpose purpose = purposes[i];
				yield return new(
					GenText.SplitCamelCase(purposes[i].ToString()),
					() => {
						AttractionUtility.ExplainAttraction(observer, target, purpose);
					}
				);
			}
		}
	}
}