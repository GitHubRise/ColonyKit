using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「常规」页：开关殖民者统计。数据在 <see cref="GeneralSettings"/>。
	/// 本页演示「同一页签下用 Section 分区」。
	/// </summary>
	public sealed class GeneralSettingsPage : SettingsPage
	{
		public override string Id => "general";

		public override string TitleKey => "ColonyKit.Settings.Tab.General";

		public override int Order => 10;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			GeneralSettings data = settings.general;

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Behavior".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.TrackSpawns".Loc(),
				ref data.trackSpawns,
				"ColonyKit.TrackSpawns.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Interface".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.EnablePawnMenu".Loc(),
				ref data.enablePawnMenu,
				"ColonyKit.EnablePawnMenu.Desc".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.ShowTechTreeButton".Loc(),
				ref data.showTechTreeButton,
				"ColonyKit.ShowTechTreeButton.Desc".Loc());

			SettingsWidgets.ControlModePicker(
				listing,
				"ColonyKit.Settings.ControlMode".Loc(),
				data.controlMode,
				mode => data.controlMode = mode);
			SettingsWidgets.Label(listing, "ColonyKit.Settings.ControlMode.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Language".Loc());
			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Language".Loc(),
				data.language,
				new[] { ModLanguage.Auto, ModLanguage.English, ModLanguage.ChineseSimplified },
				language => ("ColonyKit.Settings.Language." + language).Loc(),
				language => data.language = language);
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Language.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Notes".Loc());
			SettingsWidgets.Label(listing, "ColonyKit.Settings.General.Hint".Loc());
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.general.Reset();
		}
	}
}
