#nullable disable

﻿using Verse;
using System.Collections.Generic;

namespace rjw.Modules.Interactions.Preferences
{
	public class RapePreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			return IsRaping(pawn, partner, props);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			// 别让你的受害者使用可以用于反击的部位
			var score = Preference.LastResort;
			yield return new PartnerPartPreference(LewdablePartFamily.Hand, score);
			yield return new PartnerPartPreference(LewdablePartFamily.Mouth, score);
			yield return new PartnerPartPreference(LewdablePartFamily.Beak, score);
			yield return new PartnerPartPreference(LewdablePartFamily.Tail, score);
			yield return new PartnerPartPreference(LewdablePartFamily.Tongue, score);
		}
	}
}
