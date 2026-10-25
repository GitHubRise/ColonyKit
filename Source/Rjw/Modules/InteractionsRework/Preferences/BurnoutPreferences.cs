#nullable disable

﻿using Verse;
using System.Collections.Generic;

namespace rjw.Modules.Interactions.Preferences
{
	public class BurnoutPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return xxx.is_human(pawn) && !IsWhoring(pawn, partner, props);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			Need_Sex need = pawn.needs?.TryGetNeed<Need_Sex>();
			if (need != null && need.CurLevel >= need.thresh_neutral())
			{
				yield return new SelfPartPreference(LewdablePartFamily.Vagina, Preference.Dislike);
				yield return new SelfPartPreference(LewdablePartFamily.Anus, Preference.Dislike);
				yield return new SelfPartPreference(LewdablePartFamily.FemaleOvipositor, Preference.Dislike);
				yield return new SelfPartPreference(LewdablePartFamily.Penis, Preference.Hate);
				yield return new SelfPartPreference(LewdablePartFamily.MaleOvipositor, Preference.Hate);
			}
		}
	}
}
