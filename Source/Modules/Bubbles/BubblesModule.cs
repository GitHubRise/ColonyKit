using RimWorld;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 「气泡对话」模块：把 Interaction Bubbles 的气泡绘制并入 ColonyKit。
	/// </summary>
	/// <remarks>
	/// 气泡本体是运行时数据（不进存档），所以模块没有任何 Def；
	/// <see cref="OnGameLoaded"/> / <see cref="ResetRuntimeState"/> 只负责清空上一局残留的
	/// 气泡字典，避免读档后还挂着旧 Pawn 的引用。
	/// </remarks>
	public sealed class BubblesModule : ColonyKitModule
	{
		public const string ModuleId = "bubbles";

		public override string Id => ModuleId;

		public override string DisplayNameKey => "ColonyKit.Module.Bubbles.Name";

		public override string DescriptionKey => "ColonyKit.Module.Bubbles.Desc";

		public override int InitOrder => 115;

		public override void OnDefsLoaded()
		{
			BubblesFeature.RefreshConflict();

			// 截图式 UI 自动化场景：只在 -modtemplate-ui-test 启动时由
			// UiAutomationService 逐个打开并截图，平时不影响游戏。
			UiAutomationService.Register(new UiScenario(
				"04-bubbles-settings-zh",
				() => OpenSettingsPage(ModLanguage.ChineseSimplified)));

			UiAutomationService.Register(new UiScenario(
				"05-bubbles-settings-en",
				() => OpenSettingsPage(ModLanguage.English)));

			// 截完英文再把语言恢复默认；这一屏同时验证关闭窗口时会把中文写回设置。
			UiAutomationService.Register(new UiScenario(
				"06-bubbles-settings-restore",
				() => OpenSettingsPage(ModLanguage.ChineseSimplified)));

			if (BubblesFeature.ExternalModActive)
			{
				ModLog.Warn(
					"Bubbles",
					"external Interaction Bubbles (Jaxe.Bubbles) is active; built-in bubbles stay off "
					+ "to avoid drawing twice. Disable the standalone mod to use this module.");
			}
			else
			{
				ModLog.Info("Bubbles", "built-in interaction bubbles ready");
			}
		}

		public override void OnGameLoaded(Game game)
		{
			Bubbler.Clear();
		}

		public override void ResetRuntimeState()
		{
			// 模块关闭、读档、换图都走这里：气泡是纯运行时数据，直接清空。
			Bubbler.Clear();
		}

		/// <summary>打开本模块的设置页；UI 自动化截图用，返回 null 表示当前无法打开。</summary>
		private static Window OpenSettingsPage(ModLanguage language)
		{
			ColonyKitSettings settings = ColonyKitMod.Settings;
			if (settings?.general != null)
			{
				settings.general.language = language;
			}

			ColonyKitMod mod = ColonyKitMod.Instance;
			if (mod == null)
			{
				return null;
			}

			mod.SelectSettingsPage("bubbles");
			return new Dialog_ModSettings(mod)
			{
				forcePause = false
			};
		}
	}
}
