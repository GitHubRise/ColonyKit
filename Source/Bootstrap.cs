using System;
using HarmonyLib;
using ColonyKit.Modules;
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

				RegisterModules();
				ModuleRegistry.InitAll();

				ModLog.Info("Bootstrap", $"loaded; modules={ModuleRegistry.All.Count}");
			}
			catch (Exception e)
			{
				ModLog.Error("Bootstrap", "bootstrap failed", e);
			}
		}

		private static void RegisterModules()
		{
			// 0.0.1 框架自带模块。
			ModuleRegistry.Register(new CoreModule());
			ModuleRegistry.Register(new PawnMenuModule());
			ModuleRegistry.Register(new NotificationModule());
			ModuleRegistry.Register(new BuildModule());
			ModuleRegistry.Register(new TechTreeModule());
			ModuleRegistry.Register(new DiagnosticsModule());

			// 参考来源的四个功能模块（0.0.1 仅占位）。
			ModuleRegistry.Register(new HygieneModule());
			ModuleRegistry.Register(new StorageModule());
			ModuleRegistry.Register(new LocalizationModule());
			ModuleRegistry.Register(new RjwModule());
		}
	}
}
