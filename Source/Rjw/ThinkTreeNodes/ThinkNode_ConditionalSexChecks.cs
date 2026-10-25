#nullable disable

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 用于判断该小人能否进行性行为。
	/// 这应作为与性相关的思考树的第一个条件。
	/// </summary>
	public class ThinkNode_ConditionalSexChecks : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			//ModLog.Message("ThinkNode_ConditionalSexChecks " + xxx.get_pawnname(p));
			//if (p.Faction != null && p.Faction.IsPlayer)
			//	ModLog.Message("ThinkNode_ConditionalSexChecks " + xxx.get_pawnname(p) + " is animal: " + xxx.is_animal(p));

			// 倒地、征召和清醒状态在核心的 ThinkNode_ConditionalCanDoConstantThinkTreeJobNow 中检查。
			if (p.Map == null)
				return false;

			// 小人（动物）处于迷雾中，不进行性行为，以节省 tps
			if (p.Fogged())
				return false;

			// 小人是英雄，英雄思考树已禁用
			if (p.IsDesignatedHero() && !RJWSettings.RPG_hero_control_AI_sex)
				return false;

			// 设置检查。
			if (!xxx.can_do_loving(p))
				return false;
			else if (xxx.is_animal(p) && !RJWSettings.bestiality_enabled && !RJWSettings.animal_on_animal_enabled)
				return false;

			// 若尚未准备好进行随意性行为则跳过
			if (!SexUtility.ReadyForHookup(p))
				return false;

			// 状态检查。试图离开地图时不做爱。
			if (p.mindState?.duty?.def != null)
				if (p.mindState.duty.def == DutyDefOf.SleepForever)
					return false;
				else if (p.mindState.duty.def == VanillaDutyDefOf.EnterTransporter)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.EnterTransporterAndDefendSelf)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.ExitMapBest)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.ExitMapBestAndDefendSelf)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.ExitMapNearDutyTarget)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.ExitMapRandom)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.PrepareCaravan_CollectAnimals)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.PrepareCaravan_GatherAnimals)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.PrepareCaravan_GatherDownedPawns)
					return false;
				else if (p.mindState.duty.def == DutyDefOf.PrepareCaravan_GatherItems)
					return false;

			// 饥饿或重伤时不做爱。
			return ((!p.needs?.food?.Starving) ?? true && (xxx.is_healthy_enough(p) || !xxx.is_human(p)));
		}
	}
}
