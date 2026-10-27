using System;
using UnityEngine;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 自适应滚动视图：按上一帧内容高度撑开 viewRect，内容超过视口时自动出现滚动条。
	/// </summary>
	/// <remarks>
	/// Immediate Mode GUI 没法在画之前知道内容高度，所以用「上一帧高度」估算；
	/// 第一帧可能没有滚动条，第二帧校正。这是原版设置页的常规做法。
	///
	/// 关键约束：listing 必须 <c>maxOneColumn = true</c>。
	/// <c>Listing</c> 默认会在 <c>curY + 行高 &gt; listingRect.height</c> 时自动分列
	/// （<c>NewColumnIfNeeded</c>），而这里 listingRect 就是上一帧的 viewRect：
	/// 内容一超过视口就换到第二列，<c>curY</c> 归零，第二列又画在视口右侧被裁掉，
	/// 于是测出来的高度永远不超过视口 —— 滚动条永远不会出现，后面的参数也点不到。
	/// </remarks>
	public static class AutoScrollView
	{
		/// <summary>竖直滚动条占用的宽度；有滚动条时列宽要提前让出来，避免内容被压在滚动条下面。</summary>
		private const float ScrollbarWidth = 16f;

		public static void Draw(
			Rect outRect,
			ref Vector2 scroll,
			ref float contentHeight,
			Action<Listing_Standard> drawContents,
			float bottomPadding = 8f)
		{
			float viewHeight = Mathf.Max(outRect.height, contentHeight + bottomPadding);
			Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, outRect.width - ScrollbarWidth), viewHeight);

			Widgets.BeginScrollView(outRect, ref scroll, viewRect);

			// 单列是硬要求：否则内容超出上一帧高度时 Listing 会换列，高度永远测不出来。
			Listing_Standard listing = new Listing_Standard
			{
				maxOneColumn = true
			};
			listing.Begin(viewRect);
			drawContents?.Invoke(listing);
			listing.End();

			// 取 MaxColumnHeightSeen 而不是 CurHeight：页面若自己分栏，curY 只剩最后一列的高度。
			contentHeight = listing.MaxColumnHeightSeen;

			Widgets.EndScrollView();
		}
	}
}
