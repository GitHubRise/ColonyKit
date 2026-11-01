using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ColonyKit.Core
{
	/// <summary>
	/// 原版 1.6 的 <c>ModsConfig.GetModWarnings()</c> 按 packageId 往 <see cref="Dictionary{TKey,TValue}"/> 里 Add；
	/// 当前启用列表里同一个模组出现两次（在模组配置页反复勾选/拖动后会出现），就会抛
	/// 「An item with the same key has already been added」，整个模组页刷红字、点不动。
	/// 这里在它执行前把重复项去掉（同一个 id，或两个 id 指向同一个模组），并让
	/// ActiveModsInLoadOrder 的缓存失效，剩下的照常交给原版处理。
	/// </summary>
	[HarmonyPatch(typeof(ModsConfig), nameof(ModsConfig.GetModWarnings))]
	internal static class Patch_ModsConfig_GetModWarnings
	{
		private static readonly FieldInfo DataField =
			AccessTools.Field(typeof(ModsConfig), "data");

		private static readonly FieldInfo DirtyField =
			AccessTools.Field(typeof(ModsConfig), "activeModsInLoadOrderCachedDirty");

		[HarmonyPrefix]
		private static void Prefix()
		{
			try
			{
				object data = DataField?.GetValue(null);
				if (data == null)
				{
					return;
				}
				FieldInfo activeField = AccessTools.Field(data.GetType(), "activeMods");
				if (!(activeField?.GetValue(data) is List<string> active) || active.Count < 2)
				{
					return;
				}

				HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				HashSet<string> seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				List<string> unique = new List<string>(active.Count);
				bool changed = false;
				foreach (string id in active)
				{
					string key = id?.Trim() ?? "";
					if (key.Length == 0)
					{
						unique.Add(id);
						continue;
					}
					if (!seenIds.Add(key))
					{
						changed = true;
						continue;
					}
					string packageId = null;
					try
					{
						packageId = ModLister.GetModWithIdentifier(id)?.PackageId;
					}
					catch
					{
						// ModLister 还没准备好就按 id 去重，够用了。
					}
					if (!string.IsNullOrEmpty(packageId) && !seenPackages.Add(packageId))
					{
						// 两个不同的 id 指向同一个模组：只保留第一个。
						changed = true;
						continue;
					}
					unique.Add(id);
				}
				if (!changed)
				{
					return;
				}

				active.Clear();
				active.AddRange(unique);
				DirtyField?.SetValue(null, true);
				Log.Warning("[ColonyKit] 模组启用列表里有重复项，已去重 "
					+ (unique.Count) + " -> " + active.Count + "，避免模组页刷红字。");
			}
			catch (Exception exception)
			{
				Log.Warning("[ColonyKit] 去重模组列表失败（不影响游戏）：" + exception.Message);
			}
		}
	}
}
