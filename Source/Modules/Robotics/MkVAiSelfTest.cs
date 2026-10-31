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

			AiRequestResult http = null;
			try
			{
				RoboticsSettings settings = new RoboticsSettings
				{
					aiBaseUrl = "https://api.deepseek.com",
					aiApiKey = "invalid-self-test",
					aiModel = "deepseek-chat",
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
				+ " parsedAction=" + (parsed?.action ?? "none")
				+ " parsedSay=" + (parsed?.say ?? "none")
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
