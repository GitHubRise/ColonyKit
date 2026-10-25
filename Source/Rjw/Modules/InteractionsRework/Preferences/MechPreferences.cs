#nullable disable

﻿using RimWorld;
using Verse;
using rjw.Modules.Shared;
using System.Collections.Generic;
using System.Linq;

namespace rjw.Modules.Interactions.Preferences
{
	public class MechPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return props.isMechImplant;
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			yield return new TagPreference(SexInteractionTag.MechImplant,Preference.Always);
		}
	}
}
