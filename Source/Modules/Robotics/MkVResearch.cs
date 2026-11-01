using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人科技的自研顺序：先奔着 Mk-V 本体去——本体没完成就一路补它没完成的前置
	/// （前置本身没解锁时继续往前置的前置找），再轮到机器人研究页里的其他项目。
	/// 研究本身还是走原版研究台，这里只负责「把当前项目切到该做的那个」。
	/// </summary>
	internal static class MkVResearch
	{
		public const string RoboticsTabDefName = "CK_RoboticsTab";

		private const int MaxDepth = 6;

		/// <summary>下一个该研究的项目；全部研究完时返回 null。</summary>
		public static ResearchProjectDef NextProject()
		{
			ResearchProjectDef mkv = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("CK_RobotMkV");
			if (mkv != null && !mkv.IsFinished)
			{
				if (mkv.CanStartNow)
				{
					return mkv;
				}
				ResearchProjectDef needed = FirstStartablePrerequisite(mkv, 0);
				if (needed != null)
				{
					return needed;
				}
			}

			// Mk-V 已经研究完（或者前置链暂时没法开工）：机器人页里挑一个能开工、最便宜的。
			ResearchProjectDef best = null;
			foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
			{
				if (project == null || project.IsFinished || project.baseCost <= 0f
					|| !IsRoboticsProject(project) || !project.CanStartNow)
				{
					continue;
				}
				if (best == null || project.baseCost < best.baseCost)
				{
					best = project;
				}
			}
			return best;
		}

		/// <summary>给设置页/日志用的名字：机器人页里的写「机器人科技」，前置的写「机器人所需科技」。</summary>
		public static string LabelFor(ResearchProjectDef project)
		{
			if (project == null)
			{
				return "?";
			}
			return (IsRoboticsProject(project)
				? "CK.Robotics.Ai.Research.Robotics"
				: "CK.Robotics.Ai.Research.Prerequisite").Translate(project.LabelCap);
		}

		/// <summary>把殖民地当前研究切到这个项目；已经在研究它时什么都不做。</summary>
		public static bool Select(ResearchProjectDef project)
		{
			if (project == null || project.IsFinished || Find.ResearchManager == null)
			{
				return false;
			}
			if (!Find.ResearchManager.IsCurrentProject(project))
			{
				Find.ResearchManager.SetCurrentProject(project);
			}
			return true;
		}

		private static bool IsRoboticsProject(ResearchProjectDef project)
		{
			return project?.tab != null && project.tab.defName == RoboticsTabDefName;
		}

		/// <summary>沿前置链找第一个现在就能开工的项目。</summary>
		private static ResearchProjectDef FirstStartablePrerequisite(ResearchProjectDef project, int depth)
		{
			if (project?.prerequisites == null || depth > MaxDepth)
			{
				return null;
			}
			for (int i = 0; i < project.prerequisites.Count; i++)
			{
				ResearchProjectDef prerequisite = project.prerequisites[i];
				if (prerequisite == null || prerequisite.IsFinished)
				{
					continue;
				}
				if (prerequisite.CanStartNow)
				{
					return prerequisite;
				}
				ResearchProjectDef deeper = FirstStartablePrerequisite(prerequisite, depth + 1);
				if (deeper != null)
				{
					return deeper;
				}
			}
			return null;
		}
	}
}
