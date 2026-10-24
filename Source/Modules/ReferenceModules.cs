using Verse;

namespace ColonyKit.Modules
{
	/// <summary>淋浴模块占位：后续从 DBH 的淋浴实现移植。</summary>
	public sealed class HygieneModule : ColonyKitModule
	{
		public override string Id => "hygiene";

		public override string DisplayNameKey => "ColonyKit.Module.Hygiene.Name";

		public override string DescriptionKey => "ColonyKit.Module.Hygiene.Desc";

		public override int InitOrder => 100;

		public override void OnDefsLoaded()
		{
			ModLog.Info(
				"Hygiene",
				"module loaded (0.0.1 stub); source reference: dbh shower; " +
				$"DBH active={ModsConfig.IsActive("dubwise.dubsbadhygiene")}");
		}
	}

	/// <summary>立体库/物品堆叠模块占位：后续从 StackXXL 的仓库实现移植。</summary>
	public sealed class StorageModule : ColonyKitModule
	{
		public override string Id => "storage";

		public override string DisplayNameKey => "ColonyKit.Module.Storage.Name";

		public override string DescriptionKey => "ColonyKit.Module.Storage.Desc";

		public override int InitOrder => 110;

		public override void OnDefsLoaded()
		{
			ModLog.Info(
				"Storage",
				"module loaded (0.0.1 stub); source reference: stackxxl warehouse; " +
				$"StackXXL active={ModsConfig.IsActive("indeed.stackxxl")}");
		}
	}

	/// <summary>RJW 集成模块占位：后续只做兼容补丁，不复制 RJW 主体。</summary>
	public sealed class RjwModule : ColonyKitModule
	{
		public override string Id => "rjw";

		public override string DisplayNameKey => "ColonyKit.Module.Rjw.Name";

		public override string DescriptionKey => "ColonyKit.Module.Rjw.Desc";

		public override int InitOrder => 130;

		public override void OnDefsLoaded()
		{
			ModLog.Info(
				"RJW",
				"module loaded (0.0.1 stub); source reference: rjw; " +
				$"RJW active={ModsConfig.IsActive("rim.job.world")}");
		}
	}
}
