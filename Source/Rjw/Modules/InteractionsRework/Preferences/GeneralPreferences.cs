#nullable disable

using Verse;
using System.Collections.Generic;
using System.Linq;
using rjw.Modules.Shared.Extensions;
using System;

namespace rjw.Modules.Interactions.Preferences
{
	public class GeneralPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			// 人类的异性恋规范偏好
			// -> 女性不插入男性。男性从不被女性插入。女性通常不进行肛门自慰。

			if (!xxx.is_human(pawn) || !xxx.is_heterosexual(pawn))
				return false;

			if (pawn.gender == partner.gender && pawn != partner)
				return false;

			if (pawn.gender == Gender.Female)
				return true;

			if (pawn.gender == Gender.Male)
			{
				// 伪娘检测，男性只有乳头阶段的胸部。
				var breasts = LewdPartHelper.GetBreasts(pawn);
				if (breasts.NullOrEmpty() || breasts.All(b => b.SexPart.AsHediff.Severity <= 0.01))
					return true;
			}

			// 用补丁检查其他子模组的条件（比如他们有爱肛怪癖之类的）

			return false;
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			if (pawn.gender == Gender.Male)
			{
				yield return new PenetratedPreference(Preference.LastResort);
			}
			if (pawn.gender == Gender.Female)
			{
				if (pawn != partner)
					yield return new PenetratingPreference(Preference.LastResort);
				else
					yield return new SelfPartPreference(LewdablePartFamily.Anus, Preference.Hate);
			}
		}
	}
}
