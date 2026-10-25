using RimWorld;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>
	/// 蓝图模块：捕获已建好的建筑格局、按原材质/当前可用材料复刻、模板持久化。
	/// </summary>
	/// <remarks>
	/// 蓝图库由 <see cref="BlueprintGameComponent"/> 持有；本模块只负责生命周期与开关。
	/// 关闭模块后主按钮隐藏、设计器不显示，但已保存的蓝图库仍留在存档里（软停用）。
	/// </remarks>
	public sealed class BlueprintModule : ColonyKitModule
	{
		public const string ModuleId = "blueprint";

		public override string Id => ModuleId;

		public override string DisplayNameKey => "ColonyKit.Module.Blueprint.Name";

		public override string DescriptionKey => "ColonyKit.Module.Blueprint.Desc";

		public override int InitOrder => 120;

		public override void OnDefsLoaded()
		{
			BlueprintMaterialPolicy.ResetCache();

			bool defsReady = DefDatabase<MainButtonDef>.GetNamedSilentFail("CK_Blueprints") != null
				&& DefDatabase<DrawStyleCategoryDef>.GetNamedSilentFail("CK_BlueprintCapture") != null
				&& DefDatabase<KeyBindingDef>.GetNamedSilentFail("CK_BlueprintMirror") != null;

			if (!defsReady)
			{
				ModLog.Warn("Blueprint", "defs missing; blueprint UI may not work.");
			}
			else
			{
				ModLog.Info("Blueprint", "module loaded; capture/place designators ready.");
			}
		}

		public override void OnGameLoaded(Game game)
		{
			BlueprintLibrary library = BlueprintGameComponent.CurrentLibrary;
			ModLog.Info("Blueprint", $"library ready; records={library?.records.Count ?? 0}.");
		}

		public override void ResetRuntimeState()
		{
			BlueprintMaterialPolicy.ResetCache();
		}
	}
}
