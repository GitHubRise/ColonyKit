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
	/// </remarks>
	public static class AutoScrollView
	{
		public static void Draw(
			Rect outRect,
			ref Vector2 scroll,
			ref float contentHeight,
			Action<Listing_Standard> drawContents,
			float bottomPadding = 8f)
		{
			float viewHeight = Mathf.Max(outRect.height, contentHeight + bottomPadding);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

			Widgets.BeginScrollView(outRect, ref scroll, viewRect);

			Listing_Standard listing = new Listing_Standard();
			listing.Begin(viewRect);
			drawContents?.Invoke(listing);
			listing.End();

			contentHeight = listing.CurHeight;

			Widgets.EndScrollView();
		}
	}
}
