using System;
using HarmonyLib;
using Verse;

namespace ModTemplate
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
				new Harmony("local.modtemplate").PatchAll(typeof(Bootstrap).Assembly);

				RegisterModules();
				ModuleRegistry.InitAll();

				// 自检：能读到 DefOf 就说明 XML → 类型 → 绑定这条链路是通的。
				// note 为空表示可选补丁没有命中（例如没装 Biotech）。
				DemoConfigDef config = ModTemplateDefOf.Default;
				ModLog.Info(
					"Bootstrap",
					$"loaded; greeting=\"{config?.greeting ?? "<missing>"}\", note=\"{config?.note ?? ""}\"");
			}
			catch (Exception e)
			{
				ModLog.Error("Bootstrap", "bootstrap failed", e);
			}
		}

		private static void RegisterModules()
		{
			ModuleRegistry.Register(new CoreModule());
			ModuleRegistry.Register(new PawnMenuModule());
			ModuleRegistry.Register(new NotificationModule());
			ModuleRegistry.Register(new BuildModule());
			ModuleRegistry.Register(new TechTreeModule());
			ModuleRegistry.Register(new DiagnosticsModule());
		}
	}
}
