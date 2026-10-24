using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ColonyKit
{
	/// <summary>研究项目在科技树里的状态，绘制时决定配色。</summary>
	public enum TechAvailability
	{
		Finished,
		Available,
		Locked
	}

	/// <summary>
	/// 科技树的统一管理接口：前置条件的增删查、环检测、状态查询。
	/// 所有写操作都直接改 <see cref="ResearchProjectDef"/>，因此原版研究窗口也会同步生效。
	/// </summary>
	/// <remarks>
	/// 约定：
	/// - 所有方法只在游戏内调用（依赖 <c>Find.ResearchManager</c>）。
	/// - 修改成功后触发 <see cref="Changed"/>，打开的 <see cref="TechTreeWindow"/> 会重建布局。
	/// - 拒绝把自己设为自己的前置，也拒绝形成环。
	/// </remarks>
	public static class TechTreeService
	{
		/// <summary>前置条件或研究状态发生变化时触发。UI 订阅它来重建。</summary>
		public static event Action Changed;

		/// <summary>读取一个项目的全部前置（默认包含隐藏前置），去重。</summary>
		public static IReadOnlyList<ResearchProjectDef> GetPrerequisites(
			ResearchProjectDef project,
			bool includeHidden = true)
		{
			return TechPrerequisites.Enumerate(project, includeHidden).Distinct().ToList();
		}

		/// <summary>
		/// 添加前置。会拒绝自引用、重复项和会形成环的添加。
		/// </summary>
		/// <param name="hidden">true 写入 <c>hiddenPrerequisites</c>（不画线但仍要求完成）。</param>
		public static bool AddPrerequisite(
			ResearchProjectDef project,
			ResearchProjectDef prerequisite,
			bool hidden = false)
		{
			if (project == null || prerequisite == null)
			{
				return false;
			}

			if (project == prerequisite)
			{
				ModLog.Warn("TechTree", $"{project.defName}: 不能把自身设为前置。");
				return false;
			}

			// 新增 project -> prerequisite 后，若 prerequisite 已经依赖 project，就成环。
			if (TechPrerequisites.DependsOn(prerequisite, project, new HashSet<ResearchProjectDef>()))
			{
				ModLog.Warn("TechTree", $"{project.defName}: 添加前置 {prerequisite.defName} 会形成环，已拒绝。");
				return false;
			}

			List<ResearchProjectDef> list = hidden
				? project.hiddenPrerequisites
				: project.prerequisites;

			if (list == null)
			{
				list = new List<ResearchProjectDef>();
				if (hidden)
				{
					project.hiddenPrerequisites = list;
				}
				else
				{
					project.prerequisites = list;
				}
			}

			if (list.Contains(prerequisite))
			{
				return false;
			}

			list.Add(prerequisite);
			RaiseChanged();
			return true;
		}

		/// <summary>移除前置。返回是否真的删掉了。</summary>
		public static bool RemovePrerequisite(
			ResearchProjectDef project,
			ResearchProjectDef prerequisite,
			bool hidden = false)
		{
			if (project == null || prerequisite == null)
			{
				return false;
			}

			List<ResearchProjectDef> list = hidden
				? project.hiddenPrerequisites
				: project.prerequisites;

			if (list == null || !list.Remove(prerequisite))
			{
				return false;
			}

			RaiseChanged();
			return true;
		}

		/// <summary>
		/// 整体替换某一类前置。先做环检测，全部通过才写入，避免半改状态。
		/// </summary>
		public static bool SetPrerequisites(
			ResearchProjectDef project,
			IEnumerable<ResearchProjectDef> prerequisites,
			bool hidden = false)
		{
			if (project == null)
			{
				return false;
			}

			List<ResearchProjectDef> next = (prerequisites ?? Enumerable.Empty<ResearchProjectDef>())
				.Where(item => item != null && item != project)
				.Distinct()
				.ToList();

			foreach (ResearchProjectDef prerequisite in next)
			{
				if (TechPrerequisites.DependsOn(prerequisite, project, new HashSet<ResearchProjectDef>()))
				{
					ModLog.Warn("TechTree", $"{project.defName}: 前置集合会形成环，已拒绝。");
					return false;
				}
			}

			if (hidden)
			{
				project.hiddenPrerequisites = next;
			}
			else
			{
				project.prerequisites = next;
			}

			RaiseChanged();
			return true;
		}

		/// <summary>研究状态：已完成 / 现在可研究 / 仍被锁。</summary>
		public static TechAvailability GetAvailability(ResearchProjectDef project)
		{
			if (project == null)
			{
				return TechAvailability.Locked;
			}
			if (project.IsFinished)
			{
				return TechAvailability.Finished;
			}
			return project.CanStartNow ? TechAvailability.Available : TechAvailability.Locked;
		}

		/// <summary>
		/// 给外部修改 Def 的代码用：改完前置后手动通知 UI 重建。
		/// </summary>
		public static void NotifyChanged()
		{
			RaiseChanged();
		}

		private static void RaiseChanged()
		{
			Changed?.Invoke();
		}
	}
}
