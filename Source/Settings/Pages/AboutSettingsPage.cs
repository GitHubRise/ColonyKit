using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 「关于」页：只读信息。既给玩家看版本，也顺手验证
	/// 「XML → DemoConfigDef → DefOf → UI」这条链路。
	/// </summary>
	public sealed class AboutSettingsPage : SettingsPage
	{
		public override string Id => "about";

		public override string TitleKey => "ModTemplate.Settings.Tab.About";

		public override int Order => 90;

		// 只读页，不提供“重置本页”；控制器会隐藏对应按钮。
		public override bool CanReset => false;

		public override void DoContents(Listing_Standard listing, ModTemplateSettings settings)
		{
			ModContentPack content = ModTemplateMod.Instance?.Content;

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Metadata".Translate());

			SettingsWidgets.Label(listing, "ModTemplate.Settings.About.Name".Translate(content?.Name ?? "?"));
			SettingsWidgets.Label(listing, "ModTemplate.Settings.About.PackageId".Translate(
				content?.PackageIdPlayerFacing ?? "?"));
			SettingsWidgets.Label(listing, "ModTemplate.Settings.About.Version".Translate(
				content?.ModMetaData?.ModVersion ?? "?"));

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.DemoDef".Translate());

			// 这两个值来自 Defs/DemoConfig.xml；note 可能被可选补丁改写。
			DemoConfigDef config = ModTemplateDefOf.Default;
			SettingsWidgets.Label(listing, "ModTemplate.Settings.About.Greeting".Translate(config?.greeting ?? "<missing>"));

			string note = config != null && !config.note.NullOrEmpty() ? config.note : "-";
			SettingsWidgets.Label(listing, "ModTemplate.Settings.About.Note".Translate(note));

			listing.Gap();
			SettingsWidgets.Label(listing, "ModTemplate.Settings.About.Hint".Translate());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.TechTree".Translate());
			SettingsWidgets.Label(listing, "ModTemplate.Settings.TechTree.Hint".Translate());

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.TechTree.Open".Translate()))
			{
				if (!ModuleRegistry.IsEnabled("tech-tree"))
				{
					NotificationService.Message(
						"ModTemplate.Module.Disabled".Translate(),
						MessageTypeDefOf.RejectInput);
				}
				else
				{
					Find.WindowStack.Add(new TechTreeWindow());
				}
			}
		}
	}
}
