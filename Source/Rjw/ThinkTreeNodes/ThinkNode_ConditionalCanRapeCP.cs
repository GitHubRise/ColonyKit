#nullable disable

using Verse;
using Verse.AI;
using RimWorld;

namespace rjw
{
	public class ThinkNode_ConditionalCanRapeCP : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			//ModLog.Message("ThinkNode_ConditionalCanRapeCP " + pawn);

			if (!RJWSettings.rape_enabled)
				return false;

			// 敌对者不能使用 CP。
			if (p.HostileTo(Faction.OfPlayer))
				return false;

			// 被指定的小人不得强暴其他 CP。
			if (!RJWSettings.designated_freewill)
				if ((p.IsDesignatedComfort() || p.IsDesignatedBreeding()))
					return false;

			// 若设置已禁用，动物不能强暴 CP。
			//if (!(RJWSettings.bestiality_enabled && RJWSettings.animal_CP_rape) && xxx.is_animal(p) )
			//	return false;

			// 动物不能强暴 CP
			if (xxx.is_animal(p))
				return false;

			// 若设置已禁用，殖民者（人类）不能强暴 CP。
			if (!RJWSettings.colonist_CP_rape && p.IsColonist && xxx.is_human(p))
				return false;

			// 若设置已禁用，访客（人类）不能强暴 CP。
			if (!RJWSettings.visitor_CP_rape && p.Faction?.IsPlayer == false && xxx.is_human(p))
				return false;

			// 若设置已禁用，访客（动物/商队）不能强暴 CP。
			if (!RJWSettings.visitor_CP_rape && p.Faction?.IsPlayer == false && p.Faction != Faction.OfInsects && xxx.is_animal(p))
				return false;

			// 野生动物、昆虫不能强暴 CP。
			//if ((p.Faction == null || p.Faction == Faction.OfInsects) && xxx.is_animal(p))
			//	return false;

			return true;
		}
	}
}
