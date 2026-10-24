using System.Collections.Generic;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 前置条件查询工具：只读遍历，不修改 Def。
	/// </summary>
	public static class TechPrerequisites
	{
		/// <summary>
		/// 依次枚举 <c>prerequisites</c> 与（可选）<c>hiddenPrerequisites</c>。
		/// 隐藏前置仍然拦截研究，只是原版 UI 不画连线——本工具默认包含它们。
		/// </summary>
		public static IEnumerable<ResearchProjectDef> Enumerate(
			ResearchProjectDef project,
			bool includeHidden = true)
		{
			if (project == null)
			{
				yield break;
			}

			if (project.prerequisites != null)
			{
				foreach (ResearchProjectDef prerequisite in project.prerequisites)
				{
					if (prerequisite != null)
					{
						yield return prerequisite;
					}
				}
			}

			if (includeHidden && project.hiddenPrerequisites != null)
			{
				foreach (ResearchProjectDef prerequisite in project.hiddenPrerequisites)
				{
					if (prerequisite != null)
					{
						yield return prerequisite;
					}
				}
			}
		}

		/// <summary>
		/// <paramref name="from"/> 是否（传递地）依赖 <paramref name="target"/>。
		/// 用于新增前置时做环检测，<paramref name="seen"/> 防重复遍历。
		/// </summary>
		public static bool DependsOn(
			ResearchProjectDef from,
			ResearchProjectDef target,
			HashSet<ResearchProjectDef> seen)
		{
			if (from == null)
			{
				return false;
			}
			if (from == target)
			{
				return true;
			}
			if (!seen.Add(from))
			{
				return false;
			}

			foreach (ResearchProjectDef prerequisite in Enumerate(from))
			{
				if (DependsOn(prerequisite, target, seen))
				{
					return true;
				}
			}

			return false;
		}
	}
}
