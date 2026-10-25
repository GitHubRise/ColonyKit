#nullable disable

using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 用于判断非动物小人是否有资格从事兽交工作
	/// </summary>
	public class ThinkNode_ConditionalBestiality : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			//if (p.Faction != null && p.Faction.IsPlayer)
			//	ModLog.Message("ThinkNode_ConditionalBestiality " + xxx.get_pawnname(p) + " is animal: " + xxx.is_animal(p));

			// 动物不进行兽交，动物与动物之间由繁殖工作处理。
			if (xxx.is_animal(p))
				return false;

			// 兽交已关闭
			if (!RJWSettings.bestiality_enabled)
				return false;

			// 被指定为强暴目标时没有自由意志。
			if (!RJWSettings.designated_freewill)
				if ((p.IsDesignatedComfort() || p.IsDesignatedBreeding()))
					return false;

			return true;
		}
	}
}
