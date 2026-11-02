using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 气泡功能的总闸：模块开关、原 Mod 冲突、运行时错误，三者任一为真都不画气泡。
	/// </summary>
	/// <remarks>
	/// 补丁由 <see cref="Bootstrap"/> 统一安装，Harmony 本身无法「卸载」；
	/// 所以补丁体里必须每次查这里，而不是在安装时判断一次。
	/// </remarks>
	internal static class BubblesFeature
	{
		/// <summary>原版 Interaction Bubbles 的 packageId；同时启用会画两套气泡。</summary>
		private const string ExternalModId = "Jaxe.Bubbles";

		private static int conflictState = -1;
		private static bool failed;

		/// <summary>原 Mod 是否处于启用状态；结果缓存，第一次询问时算。</summary>
		public static bool ExternalModActive
		{
			get
			{
				if (conflictState < 0)
				{
					bool active;
					try
					{
						active = ModsConfig.IsActive(ExternalModId);
					}
					catch
					{
						active = false;
					}

					conflictState = active ? 1 : 0;
				}

				return conflictState == 1;
			}
		}

		/// <summary>本模块是否可用：模块开关打开、没有被原 Mod 顶掉、没有在本次运行里出错。</summary>
		public static bool Available =>
			!failed && !ExternalModActive && ModuleRegistry.IsEnabled(BubblesModule.ModuleId);

		/// <summary>当前是否应该记录/绘制气泡。</summary>
		public static bool Active
		{
			get
			{
				if (!Available)
				{
					return false;
				}

				BubblesSettings settings = BubblesSettingsUtility.Current;
				return settings != null && settings.enabled && settings.activated;
			}
		}

		/// <summary>忘记缓存的冲突状态；模块初始化时调用一次，保证读到最新的 Mod 列表。</summary>
		public static void RefreshConflict()
		{
			conflictState = -1;
		}

		/// <summary>绘制失败时只报一次错，之后整个功能静默停用，避免每帧刷日志。</summary>
		public static void Fail(Exception exception)
		{
			if (failed)
			{
				return;
			}

			failed = true;
			ModLog.Error(
				"Bubbles",
				"bubble drawing failed and has been disabled for this session",
				exception);
			Bubbler.Clear();
		}

		/// <summary>把当前设置里的方向索引换成 <see cref="Rot4"/>。</summary>
		public static Rot4 Direction(BubblesSettings settings)
		{
			int index = Mathf.Clamp(settings?.offsetDirection ?? 0, 0, 3);
			return new Rot4(index);
		}
	}
}
