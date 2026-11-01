using System;
using System.IO;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 开发用探针：<c>-colonykit-ai-test</c> 启动时验证两件事：
	/// 1) DeepSeek 响应 JSON 能解析成 action/say/think；
	/// 2) 真实 HTTPS 请求能到达 api.deepseek.com（用无效 Key，预期 401）。
	/// 不需要真实 API Key，也不消耗额度。
	/// </summary>
	[StaticConstructorOnStartup]
	internal static class MkVAiSelfTest
	{
		public const string CommandLineKey = "-colonykit-ai-test";

		private const string MockResponse =
			"{\"id\":\"ck-test\",\"choices\":[{\"message\":{\"role\":\"assistant\","
			+ "\"content\":\"{\\\"action\\\":\\\"work\\\",\\\"say\\\":\\\"自检通过\\\","
			+ "\\\"think\\\":\\\"我在自检，一切正常。\\\"}\"}}]}";

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

		/// <summary>
		/// 实际遇到过：模型把内容当「思考过程」用英文说了一遍（"We need answer only JSON..."），
		/// content 里一个 JSON 都没有。这种必须判成「格式失败」，才能触发严格格式重试。
		/// </summary>
		private const string ProseResponse =
			"{\"id\":\"ck-test\",\"choices\":[{\"message\":{\"role\":\"assistant\","
			+ "\"content\":\"The bot has 65% HP, one hostile at 21 distance. Candidates: attack, work.\"},"
			+ "\"finish_reason\":\"stop\"}]}";

		/// <summary>content 只是英文总结、真正的 JSON 塞在 reasoning_content 里：解析器要能翻出来。</summary>
		private const string ReasoningOnlyJsonResponse =
			"{\"id\":\"ck-test\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\","
			+ "\"content\":\"I should attack the closest enemy.\","
			+ "\"reasoning_content\":\"{\\\"action\\\":\\\"attack:123\\\",\\\"say\\\":\\\"先打近的\\\"}\"},"
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
			// think 是「日志」页第一人称独白，缺了不影响执行动作，所以单独报一项。
			bool thinkPass = parsed != null && parsed.ok
				&& parsed.think == "我在自检，一切正常。";

			AiRequestResult alias = MkVAiService.ParseForTest(AliasResponse);
			bool aliasPass = alias != null && alias.ok
				&& alias.action == "work" && alias.say == "无敌情，交还家务AI待命";

			AiRequestResult reasoning = MkVAiService.ParseForTest(ReasoningResponse);
			bool reasoningPass = reasoning != null && reasoning.ok
				&& reasoning.action == "work" && reasoning.say == "无敌情，交还家务AI待命";

			AiRequestResult prose = MkVAiService.ParseForTest(ProseResponse);
			bool proseFail = prose != null && !prose.ok && prose.formatFailure;

			AiRequestResult buried = MkVAiService.ParseForTest(ReasoningOnlyJsonResponse);
			bool reasoningJsonPass = buried != null && buried.ok && buried.action == "attack:123";

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
				+ " thinkPass=" + thinkPass
				+ " proseFail=" + proseFail
				+ " reasoningJsonPass=" + reasoningJsonPass
				+ " parsedAction=" + (parsed?.action ?? "none")
				+ " parsedSay=" + (parsed?.say ?? "none")
				+ " parsedThink=" + (parsed?.think ?? "none")
				+ " aliasAction=" + (alias?.action ?? "none")
				+ " reasoningAction=" + (reasoning?.action ?? "none")
				+ " buriedAction=" + (buried?.action ?? "none")
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
