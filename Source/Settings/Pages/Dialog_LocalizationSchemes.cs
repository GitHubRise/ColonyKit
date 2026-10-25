using ColonyKit.Modules;
using ColonyKit.Modules.Localization;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit
{
	/// <summary>翻译方案编辑窗口：新建/删除/全选/全不选/恢复默认 + 菜单勾选。</summary>
	public sealed class Dialog_LocalizationSchemes : Window
	{
		private const float RowHeight = 26f;

		private readonly LocalizationSettings settings;

		public Dialog_LocalizationSchemes(LocalizationSettings settings)
		{
			this.settings = settings;
			// 确保 schemes 已初始化，避免空列表时按钮取模/索引失败。
			_ = settings.Current;
			doCloseX = true;
			absorbInputAroundWindow = true;
			closeOnCancel = true;
			draggable = true;
			resizeable = false;
			onlyOneOfTypeAllowed = true;
		}

		public override Vector2 InitialSize => new Vector2(780f, 860f);

		public override void DoWindowContents(Rect inRect)
		{
			Listing_Standard listing = new Listing_Standard();
			listing.Begin(inRect);

			listing.Label("翻译方案（最多 " + LocalizationSettings.MaxSchemes + " 种）");
			listing.Gap(2f);

			Rect dropdownRect = listing.GetRect(30f);
			if (Widgets.ButtonText(new Rect(dropdownRect.x, dropdownRect.y, 30f, 28f), "◀"))
			{
				settings.active = (settings.active - 1 + settings.schemes.Count) % settings.schemes.Count;
			}

			Widgets.Label(
				new Rect(dropdownRect.x + 36f, dropdownRect.y + 4f, dropdownRect.width - 72f, 24f),
				"当前方案：" + settings.Current.name);

			if (Widgets.ButtonText(new Rect(dropdownRect.xMax - 30f, dropdownRect.y, 30f, 28f), "▶"))
			{
				settings.active = (settings.active + 1) % settings.schemes.Count;
			}
			listing.Gap(6f);

			Rect buttonRow = listing.GetRect(30f);
			float buttonWidth = (buttonRow.width - 24f) / 5f;

			if (Widgets.ButtonText(new Rect(buttonRow.x, buttonRow.y, buttonWidth, 28f), "新建方案"))
			{
				if (settings.schemes.Count >= LocalizationSettings.MaxSchemes)
				{
					Messages.Message(
						"最多只能有 " + LocalizationSettings.MaxSchemes + " 个方案。",
						MessageTypeDefOf.RejectInput,
						false);
				}
				else
				{
					settings.schemes.Add(new TranslationScheme { name = "方案 " + (settings.schemes.Count + 1) });
					settings.active = settings.schemes.Count - 1;
				}
			}

			if (Widgets.ButtonText(
				new Rect(buttonRow.x + buttonWidth + 6f, buttonRow.y, buttonWidth, 28f),
				"删除方案"))
			{
				if (settings.schemes.Count > 1)
				{
					settings.schemes.RemoveAt(settings.active);
					settings.active = Mathf.Clamp(settings.active, 0, settings.schemes.Count - 1);
				}
			}

			if (Widgets.ButtonText(
				new Rect(buttonRow.x + (buttonWidth + 6f) * 2f, buttonRow.y, buttonWidth, 28f),
				"全选"))
			{
				settings.Current.minimal = true;
				settings.Current.menus.Clear();
				foreach (string menu in LocalizationSettings.MenuIds)
				{
					settings.Current.menus.Add(menu);
				}
			}

			if (Widgets.ButtonText(
				new Rect(buttonRow.x + (buttonWidth + 6f) * 3f, buttonRow.y, buttonWidth, 28f),
				"全不选"))
			{
				settings.Current.minimal = false;
				settings.Current.menus.Clear();
			}

			if (Widgets.ButtonText(
				new Rect(buttonRow.x + (buttonWidth + 6f) * 4f, buttonRow.y, buttonWidth, 28f),
				"恢复默认"))
			{
				settings.schemes = LocalizationSettings.DefaultSchemes();
				settings.active = 0;
			}

			listing.Gap(6f);
			listing.Label("方案名称：");
			Rect nameRect = listing.GetRect(28f);
			settings.Current.name = Widgets.TextField(nameRect, settings.Current.name);
			if (string.IsNullOrEmpty(settings.Current.name))
			{
				settings.Current.name = "未命名";
			}

			listing.Gap(4f);
			listing.GapLine();
			listing.Label("ColonyKit.Settings.Section.TranslationTable".Loc());
			listing.Gap(2f);

			if (LocalizationModule.Conflict)
			{
				listing.Label("ColonyKit.Settings.Localization.Conflict".Loc());
			}
			else if (DevLabelTranslations.SourcePath == null)
			{
				listing.Label("ColonyKit.Settings.Localization.NotLoaded".Loc());
			}
			else
			{
				listing.Label(
					"ColonyKit.Settings.Localization.Status".Loc(
						DevLabelTranslations.Count,
						DevLabelTranslations.MenuCount,
						DevLabelTranslations.EnabledCount));
				listing.Label(
					"ColonyKit.Settings.Localization.Source".Loc(DevLabelTranslations.SourcePath));
			}

			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Localization.Reload".Loc()))
			{
				LocalizationModule.Reload();
				Messages.Message(
					"ColonyKit.Settings.Localization.Reloaded".Loc(
						DevLabelTranslations.Count,
						DevLabelTranslations.MenuCount),
					MessageTypeDefOf.NeutralEvent,
					false);
			}

			listing.Gap(4f);
			listing.Label("勾选要翻译的菜单（该菜单及子菜单内所有已知词条都会翻译）：");
			listing.Gap(2f);

			float gridTop = listing.CurHeight;
			float columnWidth = (inRect.width - 16f) / 2f;

			for (int i = 0; i < LocalizationSettings.MenuIds.Length; i++)
			{
				int column = i % 2;
				int row = i / 2;
				Rect rect = new Rect(
					inRect.x + column * columnWidth,
					inRect.y + gridTop + row * RowHeight,
					columnWidth - 8f,
					24f);

				string menu = LocalizationSettings.MenuIds[i];
				string label = i < LocalizationSettings.MenuLabels.Length
					? LocalizationSettings.MenuLabels[i]
					: menu;

				bool on = settings.Current.menus.Contains(menu);
				bool before = on;
				Widgets.CheckboxLabeled(rect, label, ref on);
				if (on != before)
				{
					if (on)
					{
						settings.Current.menus.Add(menu);
					}
					else
					{
						settings.Current.menus.Remove(menu);
					}
				}
			}

			int gridRows = (LocalizationSettings.MenuIds.Length + 1) / 2;
			listing.Gap(gridRows * RowHeight + 6f);

			listing.GapLine();
			listing.Label(
				"当前方案：" + settings.Current.name
				+ "  |  生效词条：" + DevLabelTranslations.EnabledCount
				+ " / " + DevLabelTranslations.Count);
		}
	}
}
