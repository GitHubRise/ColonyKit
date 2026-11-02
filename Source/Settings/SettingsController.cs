using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonyKit
{
	/// <summary>
	/// 设置窗口的统一调度器：标签栏、当前页、滚动、重置，全部在这里处理。
	/// </summary>
	/// <remarks>
	/// 页面只实现 <see cref="SettingsPage.DoContents"/>，不知道自己被画在哪个 Rect、
	/// 也不需要处理滚动条。这样新增一页只需要：
	///   1. 加一个 <see cref="SettingsPage"/> 子类；
	///   2. 在构造函数的页面列表里注册；
	///   3. 如果这页有自己的数据，再加一个 <c>IExposable</c> 数据类并在
	///      <see cref="ColonyKitSettings.ExposeData"/> 里 <c>Scribe_Deep</c>。
	///
	/// 每个页的滚动位置和内容高度按 <see cref="SettingsPage.Id"/> 缓存，
	/// 切页再切回来不会跳回顶部。
	/// </remarks>
	public sealed class SettingsController
	{
		private const float TabHeight = 32f;
		private const float TabGap = 6f;
		private const float SubTabHeight = 28f;
		private const float SubTabGap = 5f;
		private const float ToolbarHeight = 26f;
		private const float ContentGap = 8f;
		private const float ButtonWidth = 110f;
		private const float ButtonGap = 10f;
		private const float SearchGap = 8f;

		private readonly ColonyKitSettings settings;
		private readonly List<SettingsPage> pages;
		private readonly Dictionary<string, Vector2> scrollPositions = new Dictionary<string, Vector2>();
		private readonly Dictionary<string, float> pageHeights = new Dictionary<string, float>();
		private readonly Dictionary<string, int> subTabIndexes = new Dictionary<string, int>();

		private int selectedIndex;

		public SettingsController(ColonyKitSettings settings)
		{
			this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

			// 页面由程序集自动发现：新增 SettingsPage 子类即可，不需要改本文件。
			pages = DiscoverPages();

			if (pages.Count == 0)
			{
				throw new InvalidOperationException("SettingsController 至少需要一个可见的设置页。");
			}
		}

		private static List<SettingsPage> DiscoverPages()
		{
			var discovered = new List<SettingsPage>();

			foreach (Type type in typeof(SettingsController).Assembly.GetTypes())
			{
				if (type == null
					|| !type.IsClass
					|| type.IsAbstract
					|| !typeof(SettingsPage).IsAssignableFrom(type)
					|| type.GetConstructor(Type.EmptyTypes) == null)
				{
					continue;
				}

				try
				{
					if (Activator.CreateInstance(type) is SettingsPage page && page.Visible)
					{
						discovered.Add(page);
					}
				}
				catch (Exception exception)
				{
					ModLog.Error("Settings", $"failed to create settings page '{type.FullName}'", exception);
				}
			}

			// 同一 Id 只保留第一个，避免重复注册导致滚动位置/深链接冲突。
			List<SettingsPage> result = discovered
				.GroupBy(page => page.Id)
				.Select(group => group.First())
				.OrderBy(page => page.Order)
				.ToList();

			ModLog.Info(
				"Settings",
				"discovered pages: " + string.Join(", ", result.Select(page => page.Id)));

			return result;
		}

		/// <summary>
		/// 选中指定 Id 的页面；供深链接/自动化测试使用。找不到返回 false。
		/// </summary>
		public bool SelectPage(string pageId)
		{
			if (pageId.NullOrEmpty())
			{
				return false;
			}

			int pageIndex = pages.FindIndex(page => page.Id == pageId);
			if (pageIndex < 0)
			{
				return false;
			}

			selectedIndex = pageIndex;
			return true;
		}

		/// <summary>
		/// 画整个设置窗口。由 <see cref="ColonyKitMod.DoSettingsWindowContents"/> 在主线程调用。
		/// </summary>
		public void Draw(Rect inRect)
		{
			Rect tabsRect = new Rect(inRect.x, inRect.y, inRect.width, TabHeight);
			DrawTabs(tabsRect);

			SettingsPage page = pages[selectedIndex];
			IReadOnlyList<string> subTabs = page.SubTabKeys;
			float usedHeight = TabHeight + 4f;

			if (subTabs != null && subTabs.Count > 0)
			{
				Rect subRect = new Rect(inRect.x, inRect.y + usedHeight, inRect.width, SubTabHeight);
				DrawSubTabs(subRect, page, subTabs);
				usedHeight += SubTabHeight + 4f;
			}

			Rect toolbarRect = new Rect(inRect.x, inRect.y + usedHeight, inRect.width, ToolbarHeight);
			DrawToolbar(toolbarRect);

			usedHeight += ToolbarHeight + ContentGap;
			Rect contentRect = new Rect(
				inRect.x,
				inRect.y + usedHeight,
				inRect.width,
				Mathf.Max(0f, inRect.height - usedHeight));

			if (contentRect.height <= 1f)
			{
				return;
			}

			DrawPage(contentRect);
		}

		/// <summary>第二级页签：宽度按数量均分，点击切换并回到顶部。</summary>
		private void DrawSubTabs(Rect inRect, SettingsPage page, IReadOnlyList<string> subTabs)
		{
			int current = GetSubTab(page);
			float tabWidth = (inRect.width - (subTabs.Count - 1) * SubTabGap) / subTabs.Count;

			for (int i = 0; i < subTabs.Count; i++)
			{
				Rect tabRect = new Rect(
					inRect.x + i * (tabWidth + SubTabGap),
					inRect.y,
					tabWidth,
					inRect.height);

				bool selected = i == current;
				Widgets.DrawOptionBackground(tabRect, selected);
				if (!selected && Mouse.IsOver(tabRect))
				{
					Widgets.DrawHighlight(tabRect);
				}

				if (Widgets.ButtonInvisible(tabRect, false))
				{
					subTabIndexes[page.Id] = i;
					scrollPositions[page.Id] = Vector2.zero;
					pageHeights.Remove(page.Id);
					SoundDefOf.Click.PlayOneShotOnCamera();
				}

				TextAnchor previousAnchor = Text.Anchor;
				GameFont previousFont = Text.Font;
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Color previousColor = GUI.color;
				GUI.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.72f);
				Widgets.Label(tabRect, subTabs[i].Loc());
				GUI.color = previousColor;
				Text.Anchor = previousAnchor;
				Text.Font = previousFont;
			}
		}

		private int GetSubTab(SettingsPage page)
		{
			IReadOnlyList<string> subTabs = page.SubTabKeys;
			if (subTabs == null || subTabs.Count == 0)
			{
				return 0;
			}

			if (!subTabIndexes.TryGetValue(page.Id, out int index)
				|| index < 0
				|| index >= subTabs.Count)
			{
				return 0;
			}

			return index;
		}

		private void DrawTabs(Rect inRect)
		{
			float tabWidth = (inRect.width - (pages.Count - 1) * TabGap) / pages.Count;

			for (int i = 0; i < pages.Count; i++)
			{
				Rect tabRect = new Rect(
					inRect.x + i * (tabWidth + TabGap),
					inRect.y,
					tabWidth,
					inRect.height);

				bool selected = i == selectedIndex;
				Widgets.DrawOptionBackground(tabRect, selected);
				if (Mouse.IsOver(tabRect))
				{
					Widgets.DrawHighlight(tabRect);
				}

				// 用不可见按钮接管点击，自己画背景和文字，选中态才受控。
				if (Widgets.ButtonInvisible(tabRect, false))
				{
					selectedIndex = i;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}

				TextAnchor previousAnchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(tabRect, pages[i].TitleKey.Loc());
				Text.Anchor = previousAnchor;
			}
		}

		private void DrawToolbar(Rect inRect)
		{
			// 左侧检索框；中间提示；右侧两个按钮。宽度不够时优先保证按钮。
			float searchWidth = Mathf.Clamp(inRect.width * 0.28f, 140f, 260f);
			Rect searchRect = new Rect(inRect.x, inRect.y, searchWidth, inRect.height);

			string typed = Widgets.TextField(searchRect, SettingsSearch.Query ?? "");
			if (!string.Equals(typed, SettingsSearch.Query, StringComparison.Ordinal))
			{
				SettingsSearch.Query = typed ?? "";
				pageHeights.Clear();
				scrollPositions.Clear();
			}

			// TextField 没有 placeholder，空查询时盖一层灰字提示。
			if (SettingsSearch.Query.NullOrEmpty())
			{
				Color previousColor = GUI.color;
				TextAnchor previousAnchor = Text.Anchor;
				GUI.color = new Color(1f, 1f, 1f, 0.35f);
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(searchRect.ContractedBy(4f), "ColonyKit.Settings.Search.Hint".Loc());
				Text.Anchor = previousAnchor;
				GUI.color = previousColor;
			}

			float reserved = (ButtonWidth + ButtonGap) * 2f;
			float hintX = searchRect.xMax + SearchGap;
			float hintWidth = Mathf.Max(0f, inRect.xMax - reserved - hintX);

			if (hintWidth > 0f)
			{
				TextAnchor previousAnchor = Text.Anchor;
				Color previousColor = GUI.color;
				Text.Anchor = TextAnchor.MiddleLeft;
				// 这是补充说明而不是设置项，跟随页面里的说明文字一起压灰。
				GUI.color = SettingsWidgets.DescriptionColor;
				Widgets.Label(new Rect(hintX, inRect.y, hintWidth, inRect.height), "ColonyKit.Settings.Hint".Loc());
				GUI.color = previousColor;
				Text.Anchor = previousAnchor;
			}

			Rect resetAllRect = new Rect(inRect.xMax - ButtonWidth, inRect.y, ButtonWidth, inRect.height);
			Rect resetPageRect = new Rect(
				resetAllRect.x - ButtonWidth - ButtonGap,
				inRect.y,
				ButtonWidth,
				inRect.height);

			SettingsPage page = pages[selectedIndex];

			if (page.CanReset && Widgets.ButtonText(resetPageRect, "ColonyKit.Settings.ResetPage".Loc()))
			{
				page.Reset(settings);
			}

			if (Widgets.ButtonText(resetAllRect, "ColonyKit.Settings.ResetAll".Loc()))
			{
				// 重置全部是破坏性操作，走原版确认框，避免误点。
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
					"ColonyKit.Settings.ResetAll.Confirm".Loc(),
					ResetAllPages,
					destructive: true));
			}
		}

		private void DrawPage(Rect contentRect)
		{
			SettingsPage page = pages[selectedIndex];

			if (!scrollPositions.TryGetValue(page.Id, out Vector2 scroll))
			{
				scroll = Vector2.zero;
			}
			if (!pageHeights.TryGetValue(page.Id, out float height))
			{
				height = 0f;
			}

			SettingsSearch.BeginPage();
			SettingsWidgets.BeginPage();

			int subTab = GetSubTab(page);

			// AutoScrollView 用上一帧高度估算 viewRect：内容超过视口时自动出现滚动条。
			AutoScrollView.Draw(contentRect, ref scroll, ref height, listing =>
			{
				page.DoSubTabContents(subTab, listing, settings);

				if (SettingsSearch.Active && SettingsSearch.PageMatches == 0)
				{
					listing.Label("ColonyKit.Settings.Search.NoResults".Loc());
				}
			});

			pageHeights[page.Id] = height;
			scrollPositions[page.Id] = scroll;
		}

		private void ResetAllPages()
		{
			foreach (SettingsPage page in pages)
			{
				if (page.CanReset)
				{
					page.Reset(settings);
				}
			}
		}
	}
}
