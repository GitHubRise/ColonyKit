using ColonyKit.Modules.Localization;
using Verse;

namespace ColonyKit.Modules
{
	/// <summary>
	/// 开发者菜单汉化模块：加载外置翻译表，拦截 DebugActionNode.LabelNow。
	/// 只替换显示文本，不碰 Def、存档与玩法。
	/// </summary>
	public sealed class LocalizationModule : ColonyKitModule
	{
		public const string IdValue = "localization";

		public static bool Conflict { get; private set; }

		public override string Id => IdValue;

		public override string DisplayNameKey => "ColonyKit.Module.Localization.Name";

		public override string DescriptionKey => "ColonyKit.Module.Localization.Desc";

		public override int InitOrder => 120;

		public static bool ShouldTranslate
		{
			get
			{
				LocalizationSettings settings = ColonyKitMod.Settings?.GetModuleSettings<LocalizationSettings>();
				return ModuleRegistry.IsEnabled(IdValue)
					&& settings != null
					&& settings.enabled;
			}
		}

		public override void OnDefsLoaded()
		{
			Conflict = ModsConfig.IsActive("local.devmenuchinese");
			if (Conflict)
			{
				ModLog.Warn(
					"Localization",
					"DevMenuChinese (local.devmenuchinese) is also active; both patches will run. " +
					"ColonyKit still translates all menus, but consider disabling one to avoid mixed translations.");
			}

			// 即使原 DevMenuChinese 已启用也加载本表：两个 Postfix 都做幂等翻译，
			// 本模块的 34 个菜单组可以补上原 Mod 方案未覆盖的底部条目。
			DevLabelTranslations.Load();
			ModLog.Info(
				"Localization",
				"self-check: " +
				$"tooltip=\"{DevLabelTranslations.TranslateTooltip("Open the debug log.")}\" " +
				$"quality=\"{DevLabelTranslations.Translate("Good")}\"");
		}

		/// <summary>设置页「重新加载」按钮调用；不改文件，只重读翻译表。</summary>
		public static void Reload()
		{
			DevLabelTranslations.Load();
		}

		public override void ResetRuntimeState()
		{
			// 翻译表是只读缓存；不在这里清，避免读档时反复解析 800+ 词条。
		}
	}
}
