#nullable disable

using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 用于判断该小人能否进行恋尸行为。
	/// </summary>
	public class ThinkNode_ConditionalNecro : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			//ModLog.Message("ThinkNode_ConditionalNecro " + p);

			if (!RJWSettings.necrophilia_enabled)
				return false;

			// 动物不进行恋尸。至少目前如此。
			// 如果我们真的想加，启用起来很容易。
			if (xxx.is_animal(p))
				return false;

			// 被指定为强暴目标时没有自由意志。
			if (!RJWSettings.designated_freewill)
				if ((p.IsDesignatedComfort() || p.IsDesignatedBreeding()))
					return false;


			return true;
		}
	}
}
