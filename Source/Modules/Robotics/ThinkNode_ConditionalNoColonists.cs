using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 地图上没有任何能行动的自己人（殖民者全倒下、死光或者全都跟着远行队走了）时成立。
	/// Mk-V 是最后一支守卫力量，这时要主动把护卫职责接过来。
	/// </summary>
	public class ThinkNode_ConditionalNoColonists : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			Map map = pawn?.Map;
			if (map?.mapPawns == null)
			{
				return false;
			}
			foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
			{
				if (colonist != null && !colonist.Dead && !colonist.Downed)
				{
					return false;
				}
			}
			return true;
		}
	}
}
