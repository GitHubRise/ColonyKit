using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 「常规」页：开关殖民者统计。数据在 <see cref="GeneralSettings"/>。
	/// 本页演示「同一页签下用 Section 分区」。
	/// </summary>
	public sealed class GeneralSettingsPage : SettingsPage
	{
		public override string Id => "general";

		public override string TitleKey => "ModTemplate.Settings.Tab.General";

		public override int Order => 10;

		public override void DoContents(Listing_Standard listing, ModTemplateSettings settings)
		{
			GeneralSettings data = settings.general;

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Behavior".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.TrackSpawns".Loc(),
				ref data.trackSpawns,
				"ModTemplate.TrackSpawns.Desc".Loc());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Interface".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.EnablePawnMenu".Loc(),
				ref data.enablePawnMenu,
				"ModTemplate.EnablePawnMenu.Desc".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.ShowTechTreeButton".Loc(),
				ref data.showTechTreeButton,
				"ModTemplate.ShowTechTreeButton.Desc".Loc());

			SettingsWidgets.ControlModePicker(
				listing,
				"ModTemplate.Settings.ControlMode".Loc(),
				data.controlMode,
				mode => data.controlMode = mode);
			SettingsWidgets.Label(listing, "ModTemplate.Settings.ControlMode.Desc".Loc());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Language".Loc());
			SettingsWidgets.Dropdown(
				listing,
				"ModTemplate.Settings.Language".Loc(),
				data.language,
				new[] { ModLanguage.Auto, ModLanguage.English, ModLanguage.ChineseSimplified },
				language => ("ModTemplate.Settings.Language." + language).Loc(),
				language => data.language = language);
			SettingsWidgets.Label(listing, "ModTemplate.Settings.Language.Desc".Loc());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Notes".Loc());
			SettingsWidgets.Label(listing, "ModTemplate.Settings.General.Hint".Loc());
		}

		public override void Reset(ModTemplateSettings settings)
		{
			settings.general.Reset();
		}
	}
}
