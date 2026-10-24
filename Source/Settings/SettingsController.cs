using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ModTemplate
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
	///      <see cref="ModTemplateSettings.ExposeData"/> 里 <c>Scribe_Deep</c>。
	///
	/// 每个页的滚动位置和内容高度按 <see cref="SettingsPage.Id"/> 缓存，
	/// 切页再切回来不会跳回顶部。
	/// </remarks>
	public sealed class SettingsController
	{
		private const float TabHeight = 32f;
		private const float TabGap = 6f;
		private const float ToolbarHeight = 26f;
		private const float ContentGap = 8f;
		private const float ButtonWidth = 110f;
		private const float ButtonGap = 10f;
		private const float SearchGap = 8f;

		private readonly ModTemplateSettings settings;
		private readonly List<SettingsPage> pages;
		private readonly Dictionary<string, Vector2> scrollPositions = new Dictionary<string, Vector2>();
		private readonly Dictionary<string, float> pageHeights = new Dictionary<string, float>();

		private int selectedIndex;

		public SettingsController(ModTemplateSettings settings)
		{
			this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

			// 页面按 Order 排序；Visible 留给以后按 DLC / 其他 Mod 隐藏。
			pages = new List<SettingsPage>
			{
				new ModulesSettingsPage(),
				new GeneralSettingsPage(),
				new LoggingSettingsPage(),
				new NotificationSettingsPage(),
				new AboutSettingsPage()
			}
			.Where(page => page.Visible)
			.OrderBy(page => page.Order)
			.ToList();

			if (pages.Count == 0)
			{
				throw new InvalidOperationException("SettingsController 至少需要一个可见的设置页。");
			}
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
		/// 画整个设置窗口。由 <see cref="ModTemplateMod.DoSettingsWindowContents"/> 在主线程调用。
		/// </summary>
		public void Draw(Rect inRect)
		{
			Rect tabsRect = new Rect(inRect.x, inRect.y, inRect.width, TabHeight);
			DrawTabs(tabsRect);

			Rect toolbarRect = new Rect(inRect.x, tabsRect.yMax + 4f, inRect.width, ToolbarHeight);
			DrawToolbar(toolbarRect);

			float usedHeight = TabHeight + 4f + ToolbarHeight + ContentGap;
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
				Widgets.Label(searchRect.ContractedBy(4f), "ModTemplate.Settings.Search.Hint".Loc());
				Text.Anchor = previousAnchor;
				GUI.color = previousColor;
			}

			float reserved = (ButtonWidth + ButtonGap) * 2f;
			float hintX = searchRect.xMax + SearchGap;
			float hintWidth = Mathf.Max(0f, inRect.xMax - reserved - hintX);

			if (hintWidth > 0f)
			{
				TextAnchor previousAnchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(new Rect(hintX, inRect.y, hintWidth, inRect.height), "ModTemplate.Settings.Hint".Loc());
				Text.Anchor = previousAnchor;
			}

			Rect resetAllRect = new Rect(inRect.xMax - ButtonWidth, inRect.y, ButtonWidth, inRect.height);
			Rect resetPageRect = new Rect(
				resetAllRect.x - ButtonWidth - ButtonGap,
				inRect.y,
				ButtonWidth,
				inRect.height);

			SettingsPage page = pages[selectedIndex];

			if (page.CanReset && Widgets.ButtonText(resetPageRect, "ModTemplate.Settings.ResetPage".Loc()))
			{
				page.Reset(settings);
			}

			if (Widgets.ButtonText(resetAllRect, "ModTemplate.Settings.ResetAll".Loc()))
			{
				// 重置全部是破坏性操作，走原版确认框，避免误点。
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
					"ModTemplate.Settings.ResetAll.Confirm".Loc(),
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

			// AutoScrollView 用上一帧高度估算 viewRect：内容超过视口时自动出现滚动条。
			AutoScrollView.Draw(contentRect, ref scroll, ref height, listing =>
			{
				page.DoContents(listing, settings);

				if (SettingsSearch.Active && SettingsSearch.PageMatches == 0)
				{
					listing.Label("ModTemplate.Settings.Search.NoResults".Loc());
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
