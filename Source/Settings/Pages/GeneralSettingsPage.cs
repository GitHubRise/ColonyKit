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

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Behavior".Translate());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.TrackSpawns".Translate(),
				ref data.trackSpawns,
				"ModTemplate.TrackSpawns.Desc".Translate());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Interface".Translate());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.EnablePawnMenu".Translate(),
				ref data.enablePawnMenu,
				"ModTemplate.EnablePawnMenu.Desc".Translate());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.ShowTechTreeButton".Translate(),
				ref data.showTechTreeButton,
				"ModTemplate.ShowTechTreeButton.Desc".Translate());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Notes".Translate());
			listing.Label("ModTemplate.Settings.General.Hint".Translate());
		}

		public override void Reset(ModTemplateSettings settings)
		{
			settings.general.Reset();
		}
	}
}
