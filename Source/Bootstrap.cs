using System;
using HarmonyLib;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 启动入口。带 <see cref="StaticConstructorOnStartup"/> 的静态构造函数由游戏在
	/// **所有 Def 解析完、DefOf 绑定完**之后，在主线程统一执行一次。
	/// </summary>
	/// <remarks>
	/// 这里是唯一适合做「依赖 Def 的初始化」的地方：
	///   - 可以安全读 DefDatabase / DefOf；
	///   - 可以安装 Harmony 补丁（此时所有 Mod 的程序集都已加载）；
	///   - 抛异常不会中断游戏启动，但会被吞进日志，所以自己 catch 并加前缀更利于排查。
	/// </remarks>
	[StaticConstructorOnStartup]
	internal static class Bootstrap
	{
		static Bootstrap()
		{
			try
			{
				// PatchAll 扫描本程序集里所有 [HarmonyPatch] 类。
				// id 用 packageId，保证和别的 Mod 不冲突。
				new Harmony("local.colonykit").PatchAll(typeof(Bootstrap).Assembly);

				// 模块由程序集自动发现：新增模块不需要改本文件。
				int discovered = ModuleRegistry.DiscoverFromAssembly(typeof(Bootstrap).Assembly);
				ModuleRegistry.InitAll();

				ModLog.Info("Bootstrap", $"loaded; modules={ModuleRegistry.All.Count} (discovered={discovered})");
			}
			catch (Exception e)
			{
				ModLog.Error("Bootstrap", "bootstrap failed", e);
			}
		}
	}
}
