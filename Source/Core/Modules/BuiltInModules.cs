using Verse;

namespace ColonyKit
{
	/// <summary>内核模块：始终启用，负责其他模块的注册顺序基准。</summary>
	public sealed class CoreModule : ColonyKitModule
	{
		public override string Id => "core";

		public override string DisplayNameKey => "ColonyKit.Module.Core.Name";

		public override string DescriptionKey => "ColonyKit.Module.Core.Desc";

		public override int InitOrder => 0;

		public override bool Locked => true;
	}

	/// <summary>右键小人菜单模块。</summary>
	public sealed class PawnMenuModule : ColonyKitModule
	{
		public override string Id => "pawn-menu";

		public override string DisplayNameKey => "ColonyKit.Module.PawnMenu.Name";

		public override string DescriptionKey => "ColonyKit.Module.PawnMenu.Desc";

		public override int InitOrder => 10;
	}

	/// <summary>通知模块：游戏加载时清空节流表。</summary>
	public sealed class NotificationModule : ColonyKitModule
	{
		public override string Id => "notifications";

		public override string DisplayNameKey => "ColonyKit.Module.Notifications.Name";

		public override string DescriptionKey => "ColonyKit.Module.Notifications.Desc";

		public override int InitOrder => 20;

		public override void ResetRuntimeState()
		{
			NotificationService.ResetThrottle();
		}
	}

	/// <summary>建筑建造模块。</summary>
	public sealed class BuildModule : ColonyKitModule
	{
		public override string Id => "build-control";

		public override string DisplayNameKey => "ColonyKit.Module.Build.Name";

		public override string DescriptionKey => "ColonyKit.Module.Build.Desc";

		public override int InitOrder => 30;
	}

	/// <summary>科技树模块。</summary>
	public sealed class TechTreeModule : ColonyKitModule
	{
		public override string Id => "tech-tree";

		public override string DisplayNameKey => "ColonyKit.Module.TechTree.Name";

		public override string DescriptionKey => "ColonyKit.Module.TechTree.Desc";

		public override int InitOrder => 40;
	}

	/// <summary>诊断模块：游戏加载时清空日志去重/节流/最近日志；始终启用，保证支持信息可用。</summary>
	public sealed class DiagnosticsModule : ColonyKitModule
	{
		public override string Id => "diagnostics";

		public override string DisplayNameKey => "ColonyKit.Module.Diagnostics.Name";

		public override string DescriptionKey => "ColonyKit.Module.Diagnostics.Desc";

		public override int InitOrder => 90;

		public override bool Locked => true;

		public override void ResetRuntimeState()
		{
			ModLog.ResetRuntimeState();
		}
	}
}
