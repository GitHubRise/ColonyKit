using Verse;

namespace ColonyKit.Modules
{
	/// <summary>RJW 本体模块：RJW 1.6 由随包构建的独立 RJW.dll 提供，这里只做加载检测。</summary>
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
