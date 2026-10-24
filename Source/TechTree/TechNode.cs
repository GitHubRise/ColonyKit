using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 研究树里的一个节点：包一层 <see cref="ResearchProjectDef"/>，
	/// 加上布局结果（层、行、屏幕矩形）和树内前置引用。
	/// </summary>
	/// <remarks>
	/// 节点本身不持久化：每次打开窗口 / 前置变化时由 <see cref="TechTree.Build"/> 重建。
	/// </remarks>
	public sealed class TechNode
	{
		public TechNode(ResearchProjectDef def)
		{
			Def = def;
		}

		/// <summary>对应的研究项目。</summary>
		public ResearchProjectDef Def { get; }

		/// <summary>布局层（列）：没有前置为 0，之后逐层 +1。</summary>
		public int Tier { get; internal set; }

		/// <summary>同层内的行号。</summary>
		public int Row { get; internal set; }

		/// <summary>画布坐标（未加滚动偏移）。</summary>
		public Rect Rect { get; internal set; }

		/// <summary>树内的前置节点；跨 tab 或不在当前树里的前置只出现在 Tooltip。</summary>
		public List<TechNode> Prerequisites { get; } = new List<TechNode>();
	}
}
