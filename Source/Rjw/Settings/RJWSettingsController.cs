using UnityEngine;
using Verse;
using Multiplayer.API;
using RimWorld;
using Verse.Sound;
using System.Collections.Generic;
using rjw.RJWUI;

namespace rjw.Settings;

[StaticConstructorOnStartup]
public class RJWSettingsController : Mod
{
	private enum SettingsTab { Main, SexPreferences, Pregnancy, Hookups, Debug }

	private readonly Dictionary<SettingsTab, string> SettingsTabIconPaths;
	private readonly Dictionary<SettingsTab, Texture2D?> SettingsTabIcons = [];
	private SettingsTab selectedTab = SettingsTab.Main;
	private readonly QuickSearchWidget searchWidget = new();
	internal static QuickSearchFilter? ActiveFilter;
	internal static bool IsFiltering => ActiveFilter is { Active: true };
	private string? lastTabMatchFilterText;
	private LoadedLanguage? lastTabMatchLang;
	private readonly Dictionary<SettingsTab, bool> tabHasMatchCache = new();
	private static readonly SettingsTab[] AllTabs = (SettingsTab[])System.Enum.GetValues(typeof(SettingsTab));

	public RJWSettingsController(ModContentPack content) : base(content)
	{
		SettingsTabIconPaths = new()
		{
			{SettingsTab.Main, $"UI/Icons/Options/OptionsGeneral"},
			{SettingsTab.SexPreferences, $"UI/Icons/{(ModsConfig.BiotechActive ? "PregnancyApproaches/Normal" : "Options/OptionsGeneral")}"},
			{SettingsTab.Pregnancy, $"UI/Icons/{(ModsConfig.BiotechActive ? "PregnancyApproaches/TryForBaby" : "Options/OptionsGeneral")}"},
			{SettingsTab.Hookups, $"UI/Icons/{(ModsConfig.BiotechActive ? "PregnancyApproaches/Normal" : "Options/OptionsGeneral")}"},
			{SettingsTab.Debug, $"UI/Icons/Options/OptionsDev"}
		};
		var settings = GetSettings<RJWSettings>();
		if (RJWSettings.MigrateOldSettings(content.FolderName))
			settings.Write();
	}

	public override string SettingsCategory()
	{
		return "RJWSettingsCategory".Translate();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		if (MP.IsInMultiplayer)
			return;

		if (Event.current.type == EventType.Layout)
			return;

		ActiveFilter = searchWidget.filter;
		EnsureTabMatchCache();

		float headerH = QuickSearchWidget.WidgetHeight;
		Rect headerRect = new(inRect.x, inRect.y, inRect.width, headerH);
		float searchW = Mathf.Min(250f, inRect.width * 0.4f);
		Rect searchRect = new(headerRect.xMax - searchW, headerRect.y, searchW, headerH);
		Rect titleRect = new(headerRect.x, headerRect.y, headerRect.width - searchW - 10f, headerRect.height);
		using (new UiScope(color: Color.gray))
		{
			Widgets.LabelFit(titleRect, "RJWSettingsSubtitle".Translate());
		}
		searchWidget.OnGUI(searchRect, OnSearchChanged);

		Rect tabsRect = new(inRect.x, inRect.y + headerH, inRect.width, inRect.height - headerH);
		DrawTabs(tabsRect);

		Rect contentRect = new(inRect.x, inRect.y + headerH + 20f, inRect.width, inRect.height - headerH - 20f);

		switch (selectedTab)
		{
			case SettingsTab.Main:
				RJWSettings.DoWindowContents(contentRect);
				break;
			case SettingsTab.SexPreferences:
				RJWPreferenceSettings.DoWindowContents(contentRect);
				break;
			case SettingsTab.Pregnancy:
				RJWPregnancySettings.DoWindowContents(contentRect);
				break;
			case SettingsTab.Hookups:
				RJWHookupSettings.DoWindowContents(contentRect);
				break;
			case SettingsTab.Debug:
				RJWDebugSettings.DoWindowContents(contentRect);
				break;
		}
		// 实时刷新：所选标签页刚通过其 SearchSection 完成绘制，其
		// AnyMatched 比每次按键触发的索引更新更及时（可覆盖标签页内的开关操作）。
		if (IsFiltering)
		{
			bool? live = selectedTab switch
			{
				SettingsTab.Main => RJWSettings.LiveAnyMatch,
				SettingsTab.SexPreferences => RJWPreferenceSettings.LiveAnyMatch,
				SettingsTab.Pregnancy => RJWPregnancySettings.LiveAnyMatch,
				SettingsTab.Hookups => RJWHookupSettings.LiveAnyMatch,
				SettingsTab.Debug => RJWDebugSettings.LiveAnyMatch,
				_ => null,
			};
			if (live is bool hasLive)
			{
				tabHasMatchCache[selectedTab] = hasLive;
				searchWidget.noResultsMatched = !hasLive;
			}
		}
		if (IsFiltering && tabHasMatchCache.TryGetValue(selectedTab, out bool hasMatch) && !hasMatch)
		{
			Rect msgRect = new(contentRect.x, contentRect.y + 30f, contentRect.width, Text.LineHeight * 2f);
			using (new UiScope(anchor: TextAnchor.MiddleCenter))
			{
				Widgets.Label(msgRect, "RJWSettingsSearchNoResults".Translate());
			}
		}
	}

	private void OnSearchChanged()
	{
		RJWSettings.ResetScroll();
		RJWPreferenceSettings.ResetScroll();
		RJWPregnancySettings.ResetScroll();
		RJWHookupSettings.ResetScroll();
		RJWDebugSettings.ResetScroll();
		lastTabMatchFilterText = null;
	}

	private void EnsureTabMatchCache()
	{
		if (!IsFiltering)
			return;
		string text = searchWidget.filter.Text;
		LoadedLanguage lang = LanguageDatabase.activeLanguage;
		if (text == lastTabMatchFilterText && lang == lastTabMatchLang)
			return;
		SettingsTab[] tabs = AllTabs;
		for (int i = 0; i < tabs.Length; i++)
			tabHasMatchCache[tabs[i]] = HasAnyMatch(tabs[i]);

		if (tabHasMatchCache.TryGetValue(selectedTab, out bool curHasMatch) && !curHasMatch)
		{
			foreach (SettingsTab tab in tabs)
			{
				if (tabHasMatchCache.TryGetValue(tab, out bool hasMatch) && hasMatch)
				{
					selectedTab = tab;
					break;
				}
			}
		}

		lastTabMatchFilterText = text;
		lastTabMatchLang = lang;
		searchWidget.noResultsMatched = tabHasMatchCache.TryGetValue(selectedTab, out bool m) && !m;
	}

	private bool HasAnyMatch(SettingsTab tab)
	{
		// ActiveFilter 仅在首次绘制前为 null；此时视为“匹配所有内容”。
		QuickSearchFilter? filter = ActiveFilter;
		if (filter == null)
			return true;
		return tab switch
		{
			SettingsTab.Main => RJWSettings.HasAnyMatch(filter),
			SettingsTab.SexPreferences => RJWPreferenceSettings.HasAnyMatch(filter),
			SettingsTab.Pregnancy => RJWPregnancySettings.HasAnyMatch(filter),
			SettingsTab.Hookups => RJWHookupSettings.HasAnyMatch(filter),
			SettingsTab.Debug => RJWDebugSettings.HasAnyMatch(filter),
			_ => true,
		};
	}

	private void DrawTabs(Rect inRect)
	{
		EnsureTabMatchCache();
		SettingsTab[] tabs = AllTabs;

		float tabHeight = 32f;
		float tabY = inRect.y + 5f;
		float tabSpacing = 5f;
		float availableWidth = inRect.width - 10f;
		float tabWidth = (availableWidth - (tabs.Length - 1) * tabSpacing) / tabs.Length;
		float startX = inRect.x + 5f;

		for (int i = 0; i < tabs.Length; i++)
		{
			SettingsTab tab = tabs[i];
			bool isSelected = selectedTab == tab;
			bool enabled = !IsFiltering || !tabHasMatchCache.TryGetValue(tab, out bool hasMatch) || hasMatch;
			Rect tabRect = new(
				startX + i * (tabWidth + tabSpacing),
				tabY,
				tabWidth,
				tabHeight
			);

			if (!enabled)
			{
				Widgets.DrawOptionBackground(tabRect, false);
				Rect position = new(tabRect.x + 10f, tabRect.y + (tabRect.height - 20f) / 2f, 20f, 20f);
				GUI.DrawTexture(position, GetTabIcon(tab));
				using (new UiScope(anchor: TextAnchor.MiddleLeft, color: Color.grey))
				{
					Widgets.LabelFit(new Rect(position.xMax + 10f, tabRect.y, tabRect.width - (position.size.x + 10f), tabRect.height), GetTabLabel(tab));
				}
				TooltipHandler.TipRegion(tabRect, "RJWSettingsSearchNoResults".Translate());
				continue;
			}

			Widgets.DrawOptionBackground(tabRect, isSelected);

			if (Widgets.ButtonInvisible(tabRect, true))
			{
				selectedTab = tab;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}

			Rect position2 = new(tabRect.x + 10f, tabRect.y + (tabRect.height - 20f) / 2f, 20f, 20f);
			GUI.DrawTexture(position2, GetTabIcon(tab));
			using (new UiScope(anchor: TextAnchor.MiddleLeft))
			{
				Widgets.LabelFit(new Rect(position2.xMax + 10f, tabRect.y, tabRect.width - (position2.size.x + 10f), tabRect.height), GetTabLabel(tab));
			}
		}
	}

	private TaggedString GetTabLabel(SettingsTab tab)
	{
		return tab switch
		{
			SettingsTab.Main => "RJWSettingsMain".Translate(),
			SettingsTab.SexPreferences => "RJWSettingsPreference".Translate(),
			SettingsTab.Pregnancy => "RJWSettingsPregnancy".Translate(),
			SettingsTab.Hookups => "RJWSettingsHookup".Translate(),
			SettingsTab.Debug => "RJWSettingsDebug".Translate(),
			_ => tab.ToString(),
		};
	}

	private Texture2D GetTabIcon(SettingsTab tab)
	{
		if (!SettingsTabIcons.TryGetValue(tab, out Texture2D? icon))
		{
			icon = ContentFinder<Texture2D>.Get(SettingsTabIconPaths[tab]);
			SettingsTabIcons[tab] = icon;
		}
		return icon!;
	}
}
