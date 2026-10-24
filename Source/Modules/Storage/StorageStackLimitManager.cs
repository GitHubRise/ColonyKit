using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 堆叠上限的运行时管理器：缓存每个 ThingDef 的原始 stackLimit，
	/// 按设置倍率重算；关闭模块或检测到 Stack XXL 时恢复原值。
	/// </summary>
	internal static class StorageStackLimitManager
	{
		private static readonly Dictionary<ThingDef, int> BaseLimits = new Dictionary<ThingDef, int>();
		private static bool captured;

		public static bool StackXxlActive
		{
			get
			{
				return ModsConfig.ActiveModsInLoadOrder.Any(mod =>
					string.Equals(mod.PackageId, "indeed.stackxxl", StringComparison.OrdinalIgnoreCase));
			}
		}

		public static void Apply(StorageSettings settings)
		{
			if (settings == null || !settings.enabled || StackXxlActive)
			{
				Restore();
				if (settings != null && settings.enabled && StackXxlActive)
				{
					ModLog.Once(
						"storage.stackxxl-active",
						ModLogLevel.Warn,
						"Storage",
						"Stack XXL is active; ColonyKit stack rules are disabled to avoid double multipliers.");
				}
				return;
			}

			CaptureBaseLimits();
			int multiplier = Math.Max(StorageSettings.MinMultiplier, Math.Min(StorageSettings.MaxMultiplier, settings.stackMultiplier));

			foreach (KeyValuePair<ThingDef, int> pair in BaseLimits)
			{
				ThingDef def = pair.Key;
				long scaled = (long)pair.Value * multiplier;
				int newLimit = (int)Math.Min(int.MaxValue, Math.Max(1L, scaled));
				def.stackLimit = newLimit;

				if (settings.logChanges && pair.Value != newLimit)
				{
					ModLog.Info("Storage", $"{def.defName}: {pair.Value} -> {newLimit}");
				}
			}

			ModLog.Info(
				"Storage",
				$"stack rules applied: {BaseLimits.Count} defs x{multiplier}; StackXXL active=False");
		}

		public static void Restore()
		{
			if (!captured)
			{
				return;
			}

			foreach (KeyValuePair<ThingDef, int> pair in BaseLimits)
			{
				pair.Key.stackLimit = pair.Value;
			}
		}

		private static void CaptureBaseLimits()
		{
			if (captured)
			{
				return;
			}

			captured = true;
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
			{
				if (StorageStackRules.CanMultiply(def))
				{
					BaseLimits[def] = def.stackLimit;
				}
			}
		}
	}
}
