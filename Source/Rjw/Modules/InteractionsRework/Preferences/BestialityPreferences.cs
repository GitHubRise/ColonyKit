#nullable disable

﻿using RimWorld;
using Verse;
using rjw.Modules.Shared;
using System.Collections.Generic;
using System.Linq;

namespace rjw.Modules.Interactions.Preferences
{
	public class BestialityPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return xxx.is_human(pawn) && xxx.is_animal(partner);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{

			if (xxx.is_zoophile(pawn))
			{
				yield return new PenetrativePreference(Preference.Love);
			}
			else
			{
				yield return new PenetrativePreference(Preference.Hate);
			}
		}
	}
}
