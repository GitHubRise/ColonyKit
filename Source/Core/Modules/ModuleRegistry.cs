using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 模块注册表：注册、排序、初始化、游戏加载通知、开关读取。
	/// </summary>
	/// <remarks>
	/// 单个模块抛异常只记 Error，不影响其他模块；模块被禁用时跳过
	/// <c>OnDefsLoaded</c>/<c>OnGameLoaded</c>，但仍调用 <c>ResetRuntimeState</c> 清理残留状态。
	/// </remarks>
	public static class ModuleRegistry
	{
		private static readonly List<IModTemplateModule> modules = new List<IModTemplateModule>();
		private static readonly Dictionary<string, IModTemplateModule> byId =
			new Dictionary<string, IModTemplateModule>();

		public static IReadOnlyList<IModTemplateModule> All => modules;

		public static void Register(IModTemplateModule module)
		{
			if (module == null || module.Id.NullOrEmpty() || byId.ContainsKey(module.Id))
			{
				return;
			}

			modules.Add(module);
			byId[module.Id] = module;
			modules.Sort((left, right) => left.InitOrder.CompareTo(right.InitOrder));
		}

		public static IModTemplateModule Get(string id)
		{
			return id != null && byId.TryGetValue(id, out IModTemplateModule module) ? module : null;
		}

		public static bool IsEnabled(string id)
		{
			IModTemplateModule module = Get(id);
			if (module == null)
			{
				return false;
			}
			if (module.Locked)
			{
				return true;
			}

			ModuleSettings settings = ModTemplateMod.Settings?.modules;
			return settings == null ? module.EnabledByDefault : settings.IsEnabled(id);
		}

		/// <summary>设置模块开关；锁定模块返回 false。</summary>
		public static bool SetEnabled(string id, bool enabled)
		{
			IModTemplateModule module = Get(id);
			ModuleSettings settings = ModTemplateMod.Settings?.modules;
			if (module == null || module.Locked || settings == null)
			{
				return false;
			}

			settings.SetEnabled(id, enabled);
			ModLog.Info("Module", $"'{id}' enabled={enabled}");
			return true;
		}

		public static void ResetToDefaults()
		{
			ModTemplateMod.Settings?.modules?.Reset();
		}

		public static void InitAll()
		{
			foreach (IModTemplateModule module in modules)
			{
				if (!IsEnabled(module.Id))
				{
					continue;
				}

				try
				{
					module.OnDefsLoaded();
				}
				catch (Exception exception)
				{
					ModLog.Error("Module", $"'{module.Id}' OnDefsLoaded failed", exception);
				}
			}
		}

		public static void NotifyGameLoaded(Game game)
		{
			foreach (IModTemplateModule module in modules)
			{
				try
				{
					if (IsEnabled(module.Id))
					{
						module.OnGameLoaded(game);
					}

					// 即使被禁用也要清掉上一局留下的静态状态。
					module.ResetRuntimeState();
				}
				catch (Exception exception)
				{
					ModLog.Error("Module", $"'{module.Id}' game-load hook failed", exception);
				}
			}
		}
	}
}
