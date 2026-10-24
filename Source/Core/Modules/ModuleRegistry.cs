using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace ColonyKit
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
		private static readonly List<IColonyKitModule> modules = new List<IColonyKitModule>();
		private static readonly Dictionary<string, IColonyKitModule> byId =
			new Dictionary<string, IColonyKitModule>();

		public static IReadOnlyList<IColonyKitModule> All => modules;

		/// <summary>
		/// 扫描程序集里所有可实例化的 <see cref="IColonyKitModule"/> 并注册。
		/// 新增模块只要实现接口 + 无参构造，不需要改 <see cref="Bootstrap"/>。
		/// </summary>
		public static int DiscoverFromAssembly(Assembly assembly)
		{
			if (assembly == null)
			{
				return 0;
			}

			int count = 0;
			foreach (Type type in GetLoadableTypes(assembly))
			{
				if (type == null
					|| !type.IsClass
					|| type.IsAbstract
					|| !typeof(IColonyKitModule).IsAssignableFrom(type)
					|| type.GetConstructor(Type.EmptyTypes) == null)
				{
					continue;
				}

				try
				{
					IColonyKitModule module = (IColonyKitModule)Activator.CreateInstance(type);
					if (module == null || byId.ContainsKey(module.Id))
					{
						continue;
					}

					Register(module);
					count++;
				}
				catch (Exception exception)
				{
					ModLog.Error("Module", $"failed to create module type '{type.FullName}'", exception);
				}
			}

			return count;
		}

		private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				return exception.Types.Where(type => type != null);
			}
		}

		public static void Register(IColonyKitModule module)
		{
			if (module == null || module.Id.NullOrEmpty() || byId.ContainsKey(module.Id))
			{
				return;
			}

			modules.Add(module);
			byId[module.Id] = module;
			modules.Sort((left, right) => left.InitOrder.CompareTo(right.InitOrder));
		}

		public static IColonyKitModule Get(string id)
		{
			return id != null && byId.TryGetValue(id, out IColonyKitModule module) ? module : null;
		}

		public static bool IsEnabled(string id)
		{
			IColonyKitModule module = Get(id);
			if (module == null)
			{
				return false;
			}
			if (module.Locked)
			{
				return true;
			}

			ModuleSettings settings = ColonyKitMod.Settings?.modules;
			return settings == null ? module.EnabledByDefault : settings.IsEnabled(id);
		}

		/// <summary>设置模块开关；锁定模块返回 false。</summary>
		public static bool SetEnabled(string id, bool enabled)
		{
			IColonyKitModule module = Get(id);
			ModuleSettings settings = ColonyKitMod.Settings?.modules;
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
			ColonyKitMod.Settings?.modules?.Reset();
		}

		public static void InitAll()
		{
			foreach (IColonyKitModule module in modules)
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
			foreach (IColonyKitModule module in modules)
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
