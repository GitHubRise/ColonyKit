#nullable disable

﻿using Verse;
using System.Collections.Generic;

namespace rjw.Modules.Interactions.Preferences
{
	public class WhoringPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			// 确保妓女以某种方式使用其客户的生殖器。（受 GeneralPreferences 等影响）
			// 在客户无需使用其生殖器的情况下补丁为 false（也许他们真的很喜欢口交之类的）
			return IsFuckingWhore(pawn, partner, props);
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			yield return new SelfPartPreference(LewdablePartFamily.Penis, Preference.Neutral)
			.IfDissatisfied(new SelfPartPreference(LewdablePartFamily.Vagina, Preference.Neutral)
			.IfDissatisfied(new SelfPartPreference(LewdablePartFamily.Anus, Preference.Neutral)
			.IfDissatisfied(new SexPreference(Preference.LastResort))));
		}
	}
}
