using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 研究树绘制：先画前置连线，再画节点；只负责画布内的内容，
	/// 滚动和窗口框架由 <see cref="TechTreeWindow"/> 处理。
	/// </summary>
	/// <remarks>
	/// 节点配色：
	///   已完成 深绿 / 现在可研究 土黄 / 仍锁定 深灰；
	///   当前研究项目额外描一圈黄色边框，底部有进度条。
	/// 前置连线从「前置节点右缘」连到「本节点左缘」，颜色随前置是否完成变化。
	/// </remarks>
	public static class TechTreeView
	{
		private static readonly Color EdgeColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
		private static readonly Color EdgeFinishedColor = new Color(0.35f, 0.6f, 0.35f, 0.9f);

		private static readonly Color FinishedColor = new Color(0.20f, 0.40f, 0.20f);
		private static readonly Color AvailableColor = new Color(0.45f, 0.40f, 0.16f);
		private static readonly Color LockedColor = new Color(0.24f, 0.24f, 0.24f);

		/// <summary>
		/// 在 <paramref name="outRect"/> 内绘制整棵树。
		/// <paramref name="onSelected"/> 在点击节点时回调（默认由窗口设为当前研究）。
		/// </summary>
		public static void Draw(
			Rect outRect,
			TechTree tree,
			ref Vector2 scroll,
			Action<ResearchProjectDef> onSelected)
		{
			if (tree == null)
			{
				return;
			}

			Rect viewRect = new Rect(0f, 0f, tree.Size.x, tree.Size.y);
			Widgets.BeginScrollView(outRect, ref scroll, viewRect);

			DrawEdges(tree);

			foreach (TechNode node in tree.Nodes)
			{
				DrawNode(node, onSelected);
			}

			Widgets.EndScrollView();
		}

		private static void DrawEdges(TechTree tree)
		{
			foreach (TechNode node in tree.Nodes)
			{
				foreach (TechNode prerequisite in node.Prerequisites)
				{
					Vector2 from = new Vector2(prerequisite.Rect.xMax, prerequisite.Rect.center.y);
					Vector2 to = new Vector2(node.Rect.xMin, node.Rect.center.y);

					Color color = prerequisite.Def.IsFinished ? EdgeFinishedColor : EdgeColor;
					Widgets.DrawLine(from, to, color, 2f);
				}
			}
		}

		private static void DrawNode(TechNode node, Action<ResearchProjectDef> onSelected)
		{
			ResearchProjectDef def = node.Def;
			TechAvailability availability = TechTreeService.GetAvailability(def);

			Color fill = availability switch
			{
				TechAvailability.Finished => FinishedColor,
				TechAvailability.Available => AvailableColor,
				_ => LockedColor
			};

			Widgets.DrawBoxSolid(node.Rect, fill);
			Widgets.DrawBox(node.Rect);

			if (Find.ResearchManager.IsCurrentProject(def))
			{
				Color previous = GUI.color;
				GUI.color = new Color(1f, 0.85f, 0.2f);
				Widgets.DrawBox(node.Rect.ExpandedBy(2f), 2);
				GUI.color = previous;
			}

			TextAnchor previousAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(node.Rect.ContractedBy(8f), def.LabelCap.ToString());
			Text.Anchor = previousAnchor;

			if (!def.IsFinished && def.ProgressPercent > 0f)
			{
				Rect bar = new Rect(
					node.Rect.x + 4f,
					node.Rect.yMax - 8f,
					(node.Rect.width - 8f) * def.ProgressPercent,
					4f);
				Widgets.DrawBoxSolid(bar, new Color(0.35f, 0.75f, 0.35f));
			}

			TooltipHandler.TipRegion(node.Rect, BuildTooltip(node));

			if (Widgets.ButtonInvisible(node.Rect))
			{
				onSelected?.Invoke(def);
			}
		}

		private static string BuildTooltip(TechNode node)
		{
			ResearchProjectDef def = node.Def;
			TechAvailability availability = TechTreeService.GetAvailability(def);

			string status = availability switch
			{
				TechAvailability.Finished => "ModTemplate.TechTree.Tooltip.Finished".Translate().ToString(),
				TechAvailability.Available => "ModTemplate.TechTree.Tooltip.Available".Translate().ToString(),
				_ => "ModTemplate.TechTree.Tooltip.Locked".Translate().ToString()
			};

			List<string> prerequisites = TechPrerequisites.Enumerate(def)
				.Select(item => item.LabelCap.ToString())
				.Distinct()
				.ToList();

			string prerequisiteText = prerequisites.Count == 0
				? "ModTemplate.TechTree.Tooltip.None".Translate().ToString()
				: string.Join("、", prerequisites);

			return def.LabelCap.ToString()
				+ "\n" + def.description
				+ "\n\n" + "ModTemplate.TechTree.Tooltip.Cost".Translate(def.Cost.ToString("F0")).ToString()
				+ "\n" + "ModTemplate.TechTree.Tooltip.Prerequisites".Translate(prerequisiteText).ToString()
				+ "\n" + status
				+ "\n\n" + "ModTemplate.TechTree.Tooltip.Click".Translate().ToString();
		}
	}
}
