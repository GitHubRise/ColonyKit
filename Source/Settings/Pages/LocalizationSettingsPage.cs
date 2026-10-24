using ColonyKit.Modules;
using ColonyKit.Modules.Localization;
using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「汉化」设置页：开发者菜单汉化开关、翻译表状态、翻译方案编辑入口。
	/// 只在有开发者模式时才有意义，但页面始终可见，方便玩家确认加载状态。
	/// </summary>
	public sealed class LocalizationSettingsPage : SettingsPage
	{
		public override string Id => "localization";

		public override string TitleKey => "ColonyKit.Settings.Tab.Localization";

		public override int Order => 35;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			LocalizationSettings data = settings.localization;

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Localization".Loc());

			bool enabled = data.enabled;
			SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Localization.Enabled".Loc(),
				ref enabled,
				"ColonyKit.Settings.Localization.Enabled.Desc".Loc());
			data.enabled = enabled;

			if (LocalizationModule.Conflict)
			{
				SettingsWidgets.Label(listing, "ColonyKit.Settings.Localization.Conflict".Loc());
			}			else if (DevLabelTranslations.SourcePath == null)
			{
				SettingsWidgets.Label(listing, "ColonyKit.Settings.Localization.NotLoaded".Loc());
			}
			else
			{
				SettingsWidgets.Label(
					listing,
					"ColonyKit.Settings.Localization.Status".Loc(
						DevLabelTranslations.Count,
						DevLabelTranslations.MenuCount,
						DevLabelTranslations.EnabledCount));
				SettingsWidgets.Label(
					listing,
					"ColonyKit.Settings.Localization.Source".Loc(DevLabelTranslations.SourcePath));
			}

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.TranslationScheme".Loc());
			SettingsWidgets.Label(
				listing,
				"ColonyKit.Settings.Localization.CurrentScheme".Loc(data.Current.name));

			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Localization.EditSchemes".Loc()))
			{
				Find.WindowStack.Add(new Dialog_LocalizationSchemes(data));
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

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Notes".Loc());
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Localization.Hint".Loc());
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.localization.Reset();
			LocalizationModule.Reload();
		}
	}
}
