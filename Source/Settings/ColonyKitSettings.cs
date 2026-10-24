using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 设置根对象：只负责持久化和持有各页数据，不画 UI。
	/// 界面由 <see cref="SettingsController"/> 统一调度，数据由各页自己读写。
	/// </summary>
	/// <remarks>
	/// 每个页对应一个 <c>IExposable</c> 数据对象，用 <c>Scribe_Deep</c> 嵌套保存：
	/// <code>
	/// &lt;ModSettings Class="ColonyKit.ColonyKitSettings"&gt;
	///   &lt;general Class="ColonyKit.GeneralSettings"&gt;...&lt;/general&gt;
	///   &lt;moduleSettings&gt;
	///     &lt;li Class="ColonyKit.Modules.Localization.LocalizationSettings"&gt;...&lt;/li&gt;
	///   &lt;/moduleSettings&gt;
	/// &lt;/ModSettings&gt;
	/// </code>
	/// 模块设置通过 <see cref="IColonyKitModuleSettings"/> 注册到 <see cref="moduleSettings"/>，
	/// 新增模块不再修改本类的字段列表。
	/// </remarks>
	public class ColonyKitSettings : ModSettings
	{
		public GeneralSettings general = new GeneralSettings();
		public LoggingSettings logging = new LoggingSettings();
		public NotificationSettings notifications = new NotificationSettings();
		public ModuleSettings modules = new ModuleSettings();

		/// <summary>各模块自带的设置对象；按实际类型做 Deep Scribe，接口元素会写入 Class 属性。</summary>
		public List<IColonyKitModuleSettings> moduleSettings = new List<IColonyKitModuleSettings>();

		/// <summary>设置 schema 版本；0 表示旧扁平结构。</summary>
		public int schemaVersion;

		/// <summary>取模块设置；没有就创建并加入列表。</summary>
		public T GetModuleSettings<T>() where T : class, IColonyKitModuleSettings, new()
		{
			if (moduleSettings == null)
			{
				moduleSettings = new List<IColonyKitModuleSettings>();
			}

			T found = moduleSettings.OfType<T>().FirstOrDefault();
			if (found == null)
			{
				found = new T();
				moduleSettings.Add(found);
			}
			return found;
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Values.Look(ref schemaVersion, "schemaVersion", 0);

			// Scribe_Deep 在读档时会创建新实例并替换字段，所以这里不能用其他对象长期持有引用。
			// 页面每次绘制都通过 ColonyKitSettings 取数据，天然规避了“旧引用失效”的问题。
			Scribe_Deep.Look(ref general, "general");
			Scribe_Deep.Look(ref logging, "logging");
			Scribe_Deep.Look(ref notifications, "notifications");
			Scribe_Deep.Look(ref modules, "modules");

			// LookMode.Deep：每个元素写入/读取 <li Class="...">，因此可以存接口列表。
			Scribe_Collections.Look(ref moduleSettings, "moduleSettings", LookMode.Deep);

			// 老配置文件里没有这些节点时，用默认对象补上，避免后续空引用。
			if (general == null) general = new GeneralSettings();
			if (logging == null) logging = new LoggingSettings();
			if (notifications == null) notifications = new NotificationSettings();
			if (modules == null) modules = new ModuleSettings();
			if (moduleSettings == null) moduleSettings = new List<IColonyKitModuleSettings>();

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

			if (moduleSettings == null)
			{
				moduleSettings = new List<IColonyKitModuleSettings>();
				changed = true;
			}

			for (int i = moduleSettings.Count - 1; i >= 0; i--)
			{
				IColonyKitModuleSettings data = moduleSettings[i];
				if (data == null)
				{
					moduleSettings.RemoveAt(i);
					changed = true;
					continue;
				}

				changed |= data.Validate();
			}

			if (changed)
			{
				Log.Warning("[ColonyKit][Settings] invalid values corrected.");
			}

			return changed;
		}
	}
}
