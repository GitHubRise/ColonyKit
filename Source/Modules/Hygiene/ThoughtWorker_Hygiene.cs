using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH ThoughtWorker_Hygiene：按卫生等级切换四个阶段。</summary>
	public class ThoughtWorker_Hygiene : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			Need_Hygiene need = p.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null)
			{
				return ThoughtState.Inactive;
			}
			switch (need.CurCategory)
			{
				case HygieneCategory.Filthy:
					return ThoughtState.ActiveAtStage(0);
				case HygieneCategory.Grimy:
					return ThoughtState.ActiveAtStage(1);
				case HygieneCategory.Clean:
					return ThoughtState.ActiveAtStage(2);
				case HygieneCategory.Squeaky:
					return ThoughtState.ActiveAtStage(3);
				default:
					return ThoughtState.Inactive;
			}
		}
	}
}
