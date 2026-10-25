using Verse;

namespace ColonyKit.Modules
{
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
				"module loaded (0.0.2 stub); source reference: rjw; " +
				$"RJW active={ModsConfig.IsActive("rim.job.world")}");
		}
	}
}
