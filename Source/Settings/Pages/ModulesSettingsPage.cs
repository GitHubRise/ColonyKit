using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「模块」页：按 <see cref="IColonyKitModule.CategoryKey"/> 组成树（内核 / 功能 / 诊断），
	/// 叶子是可开关的模块，整页放在一个「模块」参数框里。
	/// </summary>
	public sealed class ModulesSettingsPage : SettingsPage
	{
		private sealed class ModuleCategory
		{
			public string Key;

			public string Label;

			public readonly List<IColonyKitModule> Modules = new List<IColonyKitModule>();
		}

		public override string Id => "modules";

		public override string TitleKey => "ColonyKit.Settings.Tab.Modules";

		public override int Order => 5;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Modules.Hint".Loc());

			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Section.Modules".Loc());

			List<ModuleCategory> categories = BuildCategories();

			for (int categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
			{
				ModuleCategory category = categories[categoryIndex];
				bool lastCategory = categoryIndex == categories.Count - 1;
				SettingsWidgets.Label(listing, (lastCategory ? "  └─ " : "  ├─ ") + category.Label);

				string childIndent = lastCategory ? "     " : "  │  ";
				for (int moduleIndex = 0; moduleIndex < category.Modules.Count; moduleIndex++)
				{
					IColonyKitModule module = category.Modules[moduleIndex];
					bool lastModule = moduleIndex == category.Modules.Count - 1;
					string branch = childIndent + (lastModule ? "└─ " : "├─ ");

					bool enabled = ModuleRegistry.IsEnabled(module.Id);
					bool before = enabled;

					string tooltip = module.DescriptionKey.Loc().ToString();
					if (module.Locked)
					{
						tooltip += "\n" + "ColonyKit.Settings.Module.Locked".Loc();
					}

					enabled = SettingsWidgets.ToggleRow(
						listing,
						branch + module.DisplayNameKey.Loc(),
						ref enabled,
						tooltip,
						enabled: !module.Locked,
						tag: module.Locked ? "锁定" : null);

					if (enabled != before)
					{
						ModuleRegistry.SetEnabled(module.Id, enabled);
						// 让模块清掉/重建依赖开关的运行时状态（例如卫生需求）。
						module.ResetRuntimeState();
					}
				}
			}

			SettingsWidgets.FrameEnd(listing, frame);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Modules.RestartHint".Loc());
		}

		public override void Reset(ColonyKitSettings settings)
		{
			ModuleRegistry.ResetToDefaults();
		}

		private static List<ModuleCategory> BuildCategories()
		{
			var categories = new List<ModuleCategory>();
			foreach (IColonyKitModule module in ModuleRegistry.All)
			{
				string key = module.CategoryKey.NullOrEmpty()
					? "ColonyKit.Module.Category.Features"
					: module.CategoryKey;
				ModuleCategory category = categories.FirstOrDefault(item => item.Key == key);
				if (category == null)
				{
					category = new ModuleCategory
					{
						Key = key,
						Label = key.Loc()
					};
					categories.Add(category);
				}

				category.Modules.Add(module);
			}

			return categories;
		}
	}
}
