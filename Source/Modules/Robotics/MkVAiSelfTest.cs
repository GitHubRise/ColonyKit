using System;
using System.IO;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 开发用探针：<c>-colonykit-ai-test</c> 启动时验证两件事：
	/// 1) DeepSeek 响应 JSON 能解析成 action/say；
	/// 2) 真实 HTTPS 请求能到达 api.deepseek.com（用无效 Key，预期 401）。
	/// 不需要真实 API Key，也不消耗额度。
	/// </summary>
	[StaticConstructorOnStartup]
	internal static class MkVAiSelfTest
	{
		public const string CommandLineKey = "-colonykit-ai-test";

		private const string MockResponse =
			"{\"id\":\"ck-test\",\"choices\":[{\"message\":{\"role\":\"assistant\","
			+ "\"content\":\"{\\\"action\\\":\\\"work\\\",\\\"say\\\":\\\"自检通过\\\"}\"}}]}";

		/// <summary>
		/// 模型有时照 candidates 的字段名回 id 而不是 action（DeepSeek 实际吐过这个形状），
		/// 解析器必须兜住，否则一句话都不说就整轮「请求失败」。
		/// </summary>
		private const string AliasResponse =
			"{\"id\":\"ck-test\",\"choices\":[{\"message\":{\"role\":\"assistant\","
			+ "\"content\":\"{\\\"id\\\":\\\"work\\\",\\\"say\\\":\\\"无敌情，交还家务AI待命\\\"}\"}}]}";

		/// <summary>
		/// deepseek-flash 这类推理模型有时把整段输出放进 reasoning_content、content 留空，
		/// 解析器要退回复用它（日志里「响应里没有 content」刷屏就是这种形状）。
		/// </summary>
		private const string ReasoningResponse =
			"{\"id\":\"ck-test\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\","
			+ "\"content\":\"\",\"reasoning_content\":\"{\\\"id\\\":\\\"work\\\",\\\"say\\\":\\\"无敌情，交还家务AI待命\\\"}\"},"
			+ "\"finish_reason\":\"stop\"}]}";

		private static bool ran;

		static MkVAiSelfTest()
		{
			if (GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				Run();
			}
		}

		public static void Run()
		{
			if (ran)
			{
				return;
			}
			ran = true;

			AiRequestResult parsed = MkVAiService.ParseForTest(MockResponse);
			bool jsonPass = parsed != null && parsed.ok
				&& parsed.action == "work" && parsed.say == "自检通过";

			AiRequestResult alias = MkVAiService.ParseForTest(AliasResponse);
			bool aliasPass = alias != null && alias.ok
				&& alias.action == "work" && alias.say == "无敌情，交还家务AI待命";

			AiRequestResult reasoning = MkVAiService.ParseForTest(ReasoningResponse);
			bool reasoningPass = reasoning != null && reasoning.ok
				&& reasoning.action == "work" && reasoning.say == "无敌情，交还家务AI待命";

			AiRequestResult http = null;
			try
			{
				RoboticsSettings settings = new RoboticsSettings
				{
					aiBaseUrl = "https://api.deepseek.com",
					aiApiKey = "invalid-self-test",
					aiModel = RoboticsSettings.DefaultAiModel,
					aiTimeout = 10f
				};
				http = MkVAiService.SendBlockingForTest(
					settings, "{\"name\":\"Mk-V\",\"candidates\":[{\"id\":\"work\"}]}");
			}
			catch (Exception exception)
			{
				http = AiRequestResult.Failure(exception.Message);
			}

			string httpStatus = http == null ? "none" : (http.ok ? "ok" : http.error);
			string summary = "AI self-test: jsonPass=" + jsonPass
				+ " aliasPass=" + aliasPass
				+ " reasoningPass=" + reasoningPass
				+ " parsedAction=" + (parsed?.action ?? "none")
				+ " parsedSay=" + (parsed?.say ?? "none")
				+ " aliasAction=" + (alias?.action ?? "none")
				+ " reasoningAction=" + (reasoning?.action ?? "none")
				+ " parsedError=" + (parsed?.error ?? "none")
				+ " httpStatus=" + httpStatus;
			ModLog.Force("Robotics", summary);
			Log.Message("[ColonyKit] " + summary);
			try
			{
				File.WriteAllText(
					Path.Combine(GenFilePaths.SaveDataFolderPath, "ai-selftest.txt"),
					summary);
			}
			catch
			{
				// 写文件失败不影响自检结果。
			}
		}
	}
}
