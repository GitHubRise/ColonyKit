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
			bool externalRjw = ModsConfig.IsActive("rim.job.world");
			ModLog.Info("RJW", $"bundled RJW 1.6 loaded; external rim.job.world active={externalRjw}");
			if (externalRjw)
			{
				ModLog.Warn(
					"RJW",
					"external RimJobWorld is active together with the bundled RJW; " +
					"disable one to avoid duplicate Defs/patches.");
			}
		}
	}
}
