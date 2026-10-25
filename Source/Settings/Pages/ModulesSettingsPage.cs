using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「模块」页：列出 <see cref="ModuleRegistry"/> 里的模块，允许关闭可选模块。
	/// </summary>
	/// <remarks>
	/// 锁定模块（core）显示为禁用态；改动立即写入设置，部分补丁/静态注册需要重启才完全生效。
	/// </remarks>
	public sealed class ModulesSettingsPage : SettingsPage
	{
		public override string Id => "modules";

		public override string TitleKey => "ColonyKit.Settings.Tab.Modules";

		public override int Order => 5;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Modules".Loc());
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Modules.Hint".Loc());

			foreach (IColonyKitModule module in ModuleRegistry.All)
			{
				bool enabled = ModuleRegistry.IsEnabled(module.Id);
				bool before = enabled;

				string tooltip = module.DescriptionKey.Loc().ToString();
				if (module.Locked)
				{
					tooltip += "\n" + "ColonyKit.Settings.Module.Locked".Loc();
				}

				SettingsWidgets.Checkbox(
					listing,
					module.DisplayNameKey.Loc(),
					ref enabled,
					tooltip,
					enabled: !module.Locked);

				if (enabled != before)
				{
					ModuleRegistry.SetEnabled(module.Id, enabled);
					// 让模块清掉/重建依赖开关的运行时状态（例如卫生需求）。
					module.ResetRuntimeState();
				}
			}

			listing.Gap();
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Modules.RestartHint".Loc());
		}

		public override void Reset(ColonyKitSettings settings)
		{
			ModuleRegistry.ResetToDefaults();
		}
	}
}
