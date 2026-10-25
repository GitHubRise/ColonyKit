#nullable disable

using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 强暴者，触发随机强暴的几率
	/// </summary>
	public class ThinkNode_ConditionalRapist : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn p)
		{
			if (!RJWSettings.rape_enabled)
				return false;
			
			if (xxx.is_animal(p))
				return false;

			if (!xxx.is_rapist(p))
				return false;

			// 被指定为强暴目标时没有自由意志。
			if (!RJWSettings.designated_freewill)
				if ((p.IsDesignatedComfort() || p.IsDesignatedBreeding()))
					return false;

			if (!xxx.IsSingleOrPartnersNotHere(p))
			{
				return false;
			}
			else
				return true;
		}
	}
}
