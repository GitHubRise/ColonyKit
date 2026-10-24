using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>科技树布局策略。</summary>
	public enum TechTreeLayoutMode
	{
		/// <summary>按前置深度分列、同层按原版 Y 坐标排序，结构最清楚（默认）。</summary>
		PrerequisiteTiers,

		/// <summary>沿用 Def 里手工摆的 <c>researchViewX/Y</c>，和原版研究页位置一致。</summary>
		VanillaCoordinates
	}

	/// <summary>
	/// 某个 <see cref="ResearchTabDef"/> 下的科技树：节点、前置边、布局尺寸。
	/// 只读视图；前置条件的修改走 <see cref="TechTreeService"/>。
	/// </summary>
	/// <remarks>
	/// 布局算法：
	///   1. 过滤该 tab 的可见项目；
	///   2. 按 <c>prerequisites</c> / <c>hiddenPrerequisites</c> 建边（只保留树内节点）；
	///   3. 递归求层：tier = max(前置 tier) + 1，遇到环记警告并按 0 处理；
	///   4. 同层按原版 <c>ResearchViewY</c> 排序，依次分配行；
	///   5. 层 → x、行 → y，或直接使用原版坐标。
	/// </remarks>
	public sealed class TechTree
	{
		public const float NodeWidth = 190f;
		public const float NodeHeight = 56f;

		private const float Margin = 24f;
		private const float HorizontalGap = 44f;
		private const float VerticalGap = 14f;

		private readonly List<TechNode> nodes = new List<TechNode>();
		private readonly Dictionary<ResearchProjectDef, TechNode> nodesByDef =
			new Dictionary<ResearchProjectDef, TechNode>();

		private TechTree(ResearchTabDef tab, TechTreeLayoutMode layoutMode)
		{
			Tab = tab;
			LayoutMode = layoutMode;
		}

		public ResearchTabDef Tab { get; }

		public TechTreeLayoutMode LayoutMode { get; }

		public IReadOnlyList<TechNode> Nodes => nodes;

		/// <summary>画布总尺寸（已含边距），供滚动视图使用。</summary>
		public Vector2 Size { get; private set; }

		public static TechTree Build(ResearchTabDef tab, TechTreeLayoutMode layoutMode)
		{
			TechTree tree = new TechTree(tab, layoutMode);
			tree.CreateNodes();
			tree.LinkPrerequisites();
			tree.ComputeLayout();
			return tree;
		}

		public TechNode NodeFor(ResearchProjectDef def)
		{
			if (def != null && nodesByDef.TryGetValue(def, out TechNode node))
			{
				return node;
			}
			return null;
		}

		private void CreateNodes()
		{
			foreach (ResearchProjectDef def in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
			{
				if (def == null || def.tab != Tab || def.IsHidden)
				{
					continue;
				}

				TechNode node = new TechNode(def);
				nodes.Add(node);
				nodesByDef[def] = node;
			}
		}

		private void LinkPrerequisites()
		{
			foreach (TechNode node in nodes)
			{
				foreach (ResearchProjectDef prerequisite in TechPrerequisites.Enumerate(node.Def))
				{
					// 跨 tab 的前置没有节点可连，只保留在 Def 里供 Tooltip 展示。
					if (nodesByDef.TryGetValue(prerequisite, out TechNode prerequisiteNode)
						&& !node.Prerequisites.Contains(prerequisiteNode))
					{
						node.Prerequisites.Add(prerequisiteNode);
					}
				}
			}
		}

		private void ComputeLayout()
		{
			Dictionary<TechNode, int> tiers = new Dictionary<TechNode, int>();
			HashSet<TechNode> visiting = new HashSet<TechNode>();

			int TierOf(TechNode node)
			{
				if (tiers.TryGetValue(node, out int cached))
				{
					return cached;
				}
				if (!visiting.Add(node))
				{
					ModLog.Warn("TechTree", $"research cycle at {node.Def.defName}; treating tier as 0.");
					return 0;
				}

				int tier = 0;
				foreach (TechNode prerequisite in node.Prerequisites)
				{
					tier = Mathf.Max(tier, TierOf(prerequisite) + 1);
				}

				visiting.Remove(node);
				tiers[node] = tier;
				return tier;
			}

			foreach (TechNode node in nodes)
			{
				node.Tier = TierOf(node);
			}

			if (LayoutMode == TechTreeLayoutMode.PrerequisiteTiers)
			{
				foreach (IGrouping<int, TechNode> group in nodes.GroupBy(node => node.Tier).OrderBy(group => group.Key))
				{
					int row = 0;
					foreach (TechNode node in group
						.OrderBy(item => item.Def.ResearchViewY)
						.ThenBy(item => item.Def.defName))
					{
						node.Row = row++;
						node.Rect = new Rect(
							Margin + node.Tier * (NodeWidth + HorizontalGap),
							Margin + node.Row * (NodeHeight + VerticalGap),
							NodeWidth,
							NodeHeight);
					}
				}
			}
			else
			{
				foreach (TechNode node in nodes)
				{
					node.Rect = new Rect(
						Margin + node.Def.ResearchViewX * 190f,
						Margin + node.Def.ResearchViewY * 100f,
						NodeWidth,
						NodeHeight);
				}
			}

			float width = nodes.Count == 0 ? 400f : nodes.Max(node => node.Rect.xMax) + Margin;
			float height = nodes.Count == 0 ? 300f : nodes.Max(node => node.Rect.yMax) + Margin;
			Size = new Vector2(Mathf.Max(400f, width), Mathf.Max(300f, height));
		}
	}
}
