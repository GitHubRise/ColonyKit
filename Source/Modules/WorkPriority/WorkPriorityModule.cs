using Verse;

namespace ColonyKit.Modules.WorkPriority
{
	/// <summary>
	/// 工作优先级模块：把原版「工作」页的优先级上限从 4 提到 9，
	/// 并给新初始化的小人一套默认优先级（灭火 1、就医 2、其余 5）。
	/// </summary>
	/// <remarks>
	/// 只改「数字能到几」和「首次初始化给什么」，不改工作本身的判定：
	/// 工作页、列头 Shift 点击、颜色、提示文本都跟着走。
	/// </remarks>
	public sealed class WorkPriorityModule : ColonyKitModule
	{
		public const string ModuleId = "work-priority";

		/// <summary>原版上限：1 最高、4 最低。</summary>
		public const int VanillaMaxPriority = 4;

		/// <summary>本模块上限。</summary>
		public const int MaxPriorityValue = 9;

		/// <summary>除灭火/就医外的默认优先级（9 级里的中间值）。</summary>
		public const int DefaultPriorityValue = 5;

		public const string FirefighterDefName = "Firefighter";

		public const string PatientDefName = "Patient";

		public const int FirefighterPriority = 1;

		public const int PatientPriority = 2;

		public override string Id => ModuleId;

		public override string DisplayNameKey => "ColonyKit.Module.WorkPriority.Name";

		public override string DescriptionKey => "ColonyKit.Module.WorkPriority.Desc";

		public override int InitOrder => 115;

		/// <summary>运行时上限：模块关闭时退回原版 4。</summary>
		public static int MaxPriority =>
			ModuleRegistry.IsEnabled(ModuleId) ? MaxPriorityValue : VanillaMaxPriority;

		public static bool IsActive => ModuleRegistry.IsEnabled(ModuleId);

		public override void OnDefsLoaded()
		{
			WorkPriorityDefaults.FixConceptHelpText();
		}

		public override void OnGameLoaded(Game game)
		{
			WorkPrioritySelftest.RunIfRequested(game);
		}
	}
}
