#nullable disable

﻿using Verse;
using rjw.Modules.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace rjw.Modules.Interactions.Preferences
{
	public class AnimalPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return !xxx.is_insect(pawn) && xxx.is_animal(pawn);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			if (xxx.is_animal(partner))
			{
				if (pawn.def.defName == partner.def.defName)
				{
					// 同物种，它们知道如何交配，总是阴道式
					yield return new TypePreference(xxx.rjwSextype.Vaginal, Preference.Always)
						.IfDissatisfied(new SexPreference(Preference.LastResort));
				}
				else
				{
					// 不同物种，总是插入式，但优先尝试阴道式。
					yield return new PenetrativePreference(Preference.Neutral)
						.IfDissatisfied(new SexPreference(Preference.LastResort));
					yield return new TypePreference(xxx.rjwSextype.Vaginal, Preference.Love);

					// 从不口交。
					yield return new SelfPartPreference(LewdablePartFamily.Mouth, Preference.LastResort);
					yield return new PartnerPartPreference(LewdablePartFamily.Mouth, Preference.LastResort);
				}
			}
			else
			{
				// 类人
				yield return new PenetratedPreference(Preference.Neutral)
					.IfSatisfied(new TypePreference(xxx.rjwSextype.Vaginal, Preference.Love));
				yield return new PenetratingPreference(Preference.Neutral)
					.IfSatisfied(new TypePreference(xxx.rjwSextype.Oral, Preference.Hate));
			}
		}
	}
}
