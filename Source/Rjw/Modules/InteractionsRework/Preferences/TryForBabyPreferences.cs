#nullable disable


using Verse;
using System.Collections.Generic;

namespace rjw.Modules.Interactions.Preferences
{
	public class TryForBabyPreferences : InteractionPreferences
	{
		public override bool IsActive(Pawn pawn, Pawn partner, SexProps props)
		{
			// 检查小人是否在与伴侣尝试要孩子
			if (pawn.relations == null || partner.relations == null)
				return false;

			if (pawn.relations.GetPregnancyApproachForPartner(partner) == RimWorld.PregnancyApproach.TryForBaby)
				return true;

			return false;
		}
		public override IEnumerable<SexPreference> GetPreferences(Pawn pawn, Pawn partner, SexProps props)
		{
			yield return new TypePreference(xxx.rjwSextype.Vaginal, Preference.Need);
		}
	}
}
