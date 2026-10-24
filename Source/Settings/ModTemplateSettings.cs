using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 设置根对象：只负责持久化和持有各页数据，不画 UI。
	/// 界面由 <see cref="SettingsController"/> 统一调度，数据由各页自己读写。
	/// </summary>
	/// <remarks>
	/// 每个页对应一个 <c>IExposable</c> 数据对象，用 <c>Scribe_Deep</c> 嵌套保存：
	/// <code>
	/// &lt;ModSettings Class="ModTemplate.ModTemplateSettings"&gt;
	///   &lt;general Class="ModTemplate.GeneralSettings"&gt;...&lt;/general&gt;
	///   &lt;logging Class="ModTemplate.LoggingSettings"&gt;...&lt;/logging&gt;
	/// &lt;/ModSettings&gt;
	/// </code>
	/// 新增一页时：加一个数据类、加一个 <see cref="SettingsPage"/>、在
	/// <see cref="SettingsController"/> 的页面列表里注册，并在本类补一行 <c>Scribe_Deep</c>。
	/// </remarks>
	public class ModTemplateSettings : ModSettings
	{
		public GeneralSettings general = new GeneralSettings();
		public LoggingSettings logging = new LoggingSettings();
		public NotificationSettings notifications = new NotificationSettings();
		public ModuleSettings modules = new ModuleSettings();

		/// <summary>设置 schema 版本；0 表示旧扁平结构。迁移后写入当前版本。</summary>
		public int schemaVersion;

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Values.Look(ref schemaVersion, "schemaVersion", 0);

			// Scribe_Deep 在读档时会创建新实例并替换字段，所以这里不能用其他对象长期持有引用。
			// 页面每次绘制都通过 ModTemplateSettings 取数据，天然规避了“旧引用失效”的问题。
			Scribe_Deep.Look(ref general, "general");
			Scribe_Deep.Look(ref logging, "logging");
			Scribe_Deep.Look(ref notifications, "notifications");
			Scribe_Deep.Look(ref modules, "modules");

			// 老配置文件里没有这些节点时，用默认对象补上，避免后续空引用。
			if (general == null) general = new GeneralSettings();
			if (logging == null) logging = new LoggingSettings();
			if (notifications == null) notifications = new NotificationSettings();
			if (modules == null) modules = new ModuleSettings();

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				SettingsMigration.Migrate(this);
			}
		}

		/// <summary>把非法值夹回合法范围；返回是否发生了修改。</summary>
		public bool Validate()
		{
			bool changed = false;
			changed |= general.Validate();
			changed |= logging.Validate();
			changed |= notifications.Validate();
			changed |= modules.Validate();

			if (changed)
			{
				Log.Warning("[ModTemplate][Settings] invalid values corrected.");
			}

			return changed;
		}
	}
}
