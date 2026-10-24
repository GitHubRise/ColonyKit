using System;
using System.IO;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 设置迁移：版本号、旧结构升级、损坏备份。
	/// </summary>
	/// <remarks>
	/// 迁移发生在 <see cref="ModTemplateSettings.ExposeData"/> 的 PostLoadInit 阶段；
	/// 此时 <see cref="ModTemplateMod.Settings"/> 还没完成读取，所以这里用 <see cref="Log"/>
	/// 而不是 <see cref="ModLog"/>，避免递归读设置。
	/// </remarks>
	public static class SettingsMigration
	{
		/// <summary>
		/// 1：嵌套设置结构；2：模块开关 + 框架日志级别。
		/// 加新字段/改结构时递增，并在 <see cref="Migrate"/> 里补一步。
		/// </summary>
		public const int CurrentVersion = 2;

		private const string LogPrefix = "[ModTemplate][Migration] ";
		private static bool dirty;

		/// <summary>是否有迁移/校验改动需要回写。</summary>
		public static bool ConsumeDirty()
		{
			bool result = dirty;
			dirty = false;
			return result;
		}

		public static void Migrate(ModTemplateSettings settings)
		{
			if (settings == null)
			{
				return;
			}

			int from = settings.schemaVersion;
			if (from > CurrentVersion)
			{
				Log.Warning(LogPrefix + "settings schema " + from
					+ " is newer than " + CurrentVersion + "; keeping values as-is.");
				return;
			}

			bool changed = false;

			if (from < 1)
			{
				// v0 是旧的扁平设置；嵌套对象已在 ExposeData 里补默认值，这里只记一次。
				changed = true;
			}

			if (from < 2 && settings.modules == null)
			{
				settings.modules = new ModuleSettings();
				changed = true;
			}

			if (settings.Validate())
			{
				changed = true;
			}

			if (from < CurrentVersion)
			{
				settings.schemaVersion = CurrentVersion;
				changed = true;
				Log.Message(LogPrefix + "settings migrated " + from + " -> " + CurrentVersion);
			}

			if (changed)
			{
				dirty = true;
			}
		}

		/// <summary>启动时把现有设置文件复制为 .bak，避免迁移失败后无法回退。</summary>
		public static void BackupSettings(string folderName, string modTypeName)
		{
			try
			{
				string path = Path.Combine(
					GenFilePaths.ConfigFolderPath,
					"Mod_" + folderName + "_" + modTypeName + ".xml");

				if (!File.Exists(path))
				{
					return;
				}

				string backup = path + ".bak";
				File.Copy(path, backup, true);
				Log.Message(LogPrefix + "settings backup: " + backup);
			}
			catch (Exception exception)
			{
				Log.Warning(LogPrefix + "settings backup failed: " + exception.Message);
			}
		}
	}
}
