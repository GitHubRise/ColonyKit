#nullable disable

﻿using Verse;
using System.Collections.Generic;
using System.Linq;

namespace rjw.Modules.Interactions.Preferences
{
	public class BigBreastsPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return xxx.is_human(pawn) && xxx.is_human(partner);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			var partnerBreasts = LewdPartHelper.GetBreasts(partner);

			if (partnerBreasts.NullOrEmpty())
			{
				yield break;
			}

			var partnerBigBreasts = partnerBreasts.Any(b => b.SexPart.AsHediff.Severity >= 0.8);
			var partnerSmallBreasts = partnerBreasts.All(b => b.SexPart.AsHediff.Severity < 0.4);
			var partnerNoBreasts = partnerBreasts.All(b => b.SexPart.AsHediff.Severity <= 0.01);

			if (partnerNoBreasts && partner.gender == Gender.Male)
			{
				yield return new PartnerPartPreference(LewdablePartFamily.Breasts, Preference.LastResort);
			}
			else if (partnerBigBreasts)
			{
				yield return new PartnerPartPreference(LewdablePartFamily.Breasts, Preference.Like);
			}
			else if (partnerSmallBreasts)
			{
				yield return new PartnerPartPreference(LewdablePartFamily.Breasts, Preference.Hate);
			}

		}
	}
}
