#nullable disable

﻿using Verse;
using rjw.Modules.Shared;
using System.Collections.Generic;
using System.Linq;

namespace rjw.Modules.Interactions.Preferences
{
	public class InsectPreferences : InteractionPreferences
	{

		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return xxx.is_insect(pawn);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			if (pawn.gender == Gender.Male)
			{
				// 雄性产卵器用于受精。
				yield return new TagPreference(SexInteractionTag.EggFertilization, Preference.Neutral)
				.IfDissatisfied(new SexPreference(Preference.LastResort));
			}
			else if (pawn.gender == Gender.Female)
			{
				// 雌性产卵器用于产卵。
				yield return new TagPreference(SexInteractionTag.EggLaying, Preference.Neutral)
				.IfDissatisfied(new SexPreference(Preference.LastResort));
			}
		}
	}
}
