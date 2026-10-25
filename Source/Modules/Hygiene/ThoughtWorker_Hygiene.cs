using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>卫生值低于阈值时激活肮脏念头。</summary>
	public class ThoughtWorker_Hygiene : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			Need_Hygiene need = p.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null)
			{
				return false;
			}
			if (need.CurLevel < 0.1f)
			{
				return ThoughtState.ActiveAtStage(0);
			}
			if (need.CurLevel < 0.4f)
			{
				return ThoughtState.ActiveAtStage(1);
			}
			return false;
		}
	}
}
