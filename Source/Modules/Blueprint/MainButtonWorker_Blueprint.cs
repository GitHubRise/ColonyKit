using RimWorld;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>蓝图主按钮：模块被软停用时立即隐藏入口。</summary>
	public class MainButtonWorker_Blueprint : MainButtonWorker_ToggleTab
	{
		public override bool Visible =>
			ModuleRegistry.IsEnabled(BlueprintModule.ModuleId) && base.Visible;
	}
}
