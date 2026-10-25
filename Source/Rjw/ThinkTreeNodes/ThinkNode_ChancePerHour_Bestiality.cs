#nullable disable

using System;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace rjw
{
	public class ThinkNode_ChancePerHour_Bestiality : ThinkNode_ChancePerHour
	{
		protected override float MtbHours(Pawn pawn)
		{
			float base_mtb = xxx.config.comfort_prisoner_rape_mtbh_mul; // 默认值为 4.0

			float desire_factor;
			{
				Need_Sex need_sex = pawn.needs.TryGetNeed<Need_Sex>();

				if (need_sex != null)
				{
					if (need_sex.CurLevel <= need_sex.thresh_frustrated())
						desire_factor = 0.40f;
					else if (need_sex.CurLevel <= need_sex.thresh_horny())
						desire_factor = 0.80f;
					else
						desire_factor = 1.00f;
				}
				else
					desire_factor = 1.00f;
			}

			float personality_factor;
			{
				personality_factor = 1.0f;

				if (xxx.is_nympho(pawn))
					personality_factor *= 0.5f;
				else if (xxx.is_prude(pawn) || pawn.story.traits.HasTrait(TraitDefOf.BodyPurist))
					personality_factor *= 2f;

				if (pawn.story.traits.HasTrait(TraitDefOf.Nudist))
					personality_factor *= 0.9f;

				// 没有兽交特质的小人应首先尝试寻找其他发泄途径。
				if (!xxx.is_zoophile(pawn))
					personality_factor *= 8f;

				// 若小人有爱人，则不太可能参与兽交……除非爱人也是动物（有模组支持这种情况，所以需要检查）。
				if (!xxx.IsSingleOrPartnersNotHere(pawn) && !xxx.is_animal(LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, false)) && !xxx.is_lecher(pawn) && !xxx.is_nympho(pawn))
					personality_factor *= 2.5f;

				// 以往很少有或从未与动物发生过关系的小人更不愿意参与兽交。
				if (pawn.records.GetValue(xxx.CountOfSexWithAnimals) < 3)
					personality_factor *= 3f;
				else if (pawn.records.GetValue(xxx.CountOfSexWithAnimals) > 10)
					personality_factor *= 0.8f;
			}

			float fun_factor;
			{
				if ((pawn.needs.joy != null) && (xxx.is_bloodlust(pawn)))
					fun_factor = Mathf.Clamp01(0.50f + pawn.needs.joy.CurLevel);
				else
					fun_factor = 1.00f;
			}

			return base_mtb * desire_factor * personality_factor * fun_factor;
		}

		public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
		{
			try
			{
				return base.TryIssueJobPackage(pawn, jobParams);
			}
			catch (NullReferenceException)
			{
				//--ModLog.Message("ThinkNode_ChancePerHour_Bestiality:TryIssueJobPackage - error message" + e.Message);
				//--ModLog.Message("ThinkNode_ChancePerHour_Bestiality:TryIssueJobPackage - error stacktrace" + e.StackTrace);
				return ThinkResult.NoJob; ;
			}
		}
	}
}
