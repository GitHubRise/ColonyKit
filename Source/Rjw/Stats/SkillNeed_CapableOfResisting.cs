#nullable disable

﻿using RimWorld;
using Verse;

namespace rjw
{
	// 用于 Vulnerability StatDef 的计算
	public class SkillNeed_CapableOfResisting : SkillNeed_BaseBonus
	{
		public override float ValueFor(Pawn pawn)
		{
			if (pawn.skills == null)
			{
				return 1f;
			}

			int level = NegateSkillBonus(pawn) ? 0 : pawn.skills.GetSkill(skill).Level;
			return ValueAtLevel(level);
		}

		protected virtual bool NegateSkillBonus(Pawn pawn)
		{
			// 移除以下小人的近战加成：倒地、睡眠/休息/躺卧、穿着束臂具
			return pawn.Downed
				|| pawn.GetPosture() != PawnPosture.Standing
				// RJW 侧的简单校验，但理想情况下此方法应直接在 RJW-EX 中重写
				|| (xxx.RjwExIsActive && pawn.health.hediffSet.HasHediff(xxx.RjwEx_Armbinder));
		}
	}
}
