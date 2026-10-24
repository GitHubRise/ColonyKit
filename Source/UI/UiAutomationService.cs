using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 一个 UI 自动化场景：框架负责打开、等待绘制、截图、关闭；
	/// 使用者只需要提供「窗口怎么建」和可选的「截图后点什么」。
	/// </summary>
	public sealed class UiScenario
	{
		public string Id { get; }
		public Func<Window> CreateWindow { get; }
		public Action OnCaptured { get; }

		/// <param name="id">文件名与日志标识。</param>
		/// <param name="createWindow">返回要打开的窗口；返回 null 表示只截当前界面（例如底部菜单）。</param>
		/// <param name="onCaptured">截图之后执行的动作，用来模拟点击/触发；可为 null。</param>
		public UiScenario(string id, Func<Window> createWindow = null, Action onCaptured = null)
		{
			Id = id;
			CreateWindow = createWindow;
			OnCaptured = onCaptured;
		}
	}

	/// <summary>
	/// 截图式 UI 自动化：按注册顺序打开窗口 → 等绘制 → `ScreenCapture` → 执行动作 → 关闭。
	/// </summary>
	/// <remarks>
	/// 只在带 <c>-modtemplate-ui-test</c> 启动时运行；截图写到
	/// <c>Config/ModTemplateUiTests/</c>，日志里会打印每个文件路径。
	/// 使用方只需要 <see cref="Register"/> 数据，不需要写窗口循环或计时。
	/// </remarks>
	public static class UiAutomationService
	{
		private enum Phase
		{
			Idle,
			Opening,
			Capturing,
			Closing
		}

		private static readonly List<UiScenario> scenarios = new List<UiScenario>();
		private static readonly int settleFrames = 20;
		private static readonly int afterCaptureFrames = 20;

		private static Phase phase = Phase.Idle;
		private static int index;
		private static int waitFrames;
		private static Window currentWindow;
		private static UiScenario currentScenario;
		private static string outputDir;
		private static int captured;
		private static int skipped;

		static UiAutomationService()
		{
			Register(new UiScenario("01-bottom-bar"));

			Register(new UiScenario(
				"02-settings-notifications",
				OpenNotificationSettings));

			Register(new UiScenario(
				"03-pawn-menu",
				OpenPawnMenu,
				ClickFirstPawnMenuOption));

			Register(new UiScenario(
				"04-tech-tree",
				() => new TechTreeWindow()));

			Register(new UiScenario(
				"05-demo-drawer",
				() => new ModDrawerWindow()));
		}

		public static void Register(UiScenario scenario)
		{
			if (scenario != null && !scenario.Id.NullOrEmpty())
			{
				scenarios.Add(scenario);
			}
		}

		public static void RunAll()
		{
			if (phase != Phase.Idle)
			{
				return;
			}

			outputDir = Path.Combine(GenFilePaths.ConfigFolderPath, "ModTemplateUiTests");
			Directory.CreateDirectory(outputDir);

			index = 0;
			captured = 0;
			skipped = 0;
			waitFrames = 0;
			phase = Phase.Opening;

			ModLog.Message($"ui-test: start, output={outputDir}, scenarios={scenarios.Count}");
		}

		/// <summary>由 Root.Update 的 Postfix 每帧调用。</summary>
		public static void Update()
		{
			if (phase == Phase.Idle)
			{
				return;
			}

			if (waitFrames > 0)
			{
				waitFrames--;
				return;
			}

			switch (phase)
			{
				case Phase.Opening:
					OpenNext();
					break;
				case Phase.Capturing:
					CaptureCurrent();
					break;
				case Phase.Closing:
					CloseCurrent();
					break;
			}
		}

		private static void OpenNext()
		{
			if (index >= scenarios.Count)
			{
				Finish();
				return;
			}

			currentScenario = scenarios[index];
			try
			{
				currentWindow = currentScenario.CreateWindow?.Invoke();
				if (currentWindow != null && Find.WindowStack != null)
				{
					Find.WindowStack.Add(currentWindow);
				}
			}
			catch (Exception exception)
			{
				ModLog.Error($"ui-test '{currentScenario.Id}' open failed: {exception}");
				currentWindow = null;
			}

			waitFrames = settleFrames;
			phase = Phase.Capturing;
		}

		private static void CaptureCurrent()
		{
			string path = Path.Combine(outputDir, currentScenario.Id + ".png").Replace('\\', '/');
			try
			{
				ScreenCapture.CaptureScreenshot(path);
				captured++;
				ModLog.Message($"ui-test: captured {currentScenario.Id} -> {path}");
			}
			catch (Exception exception)
			{
				skipped++;
				ModLog.Error($"ui-test '{currentScenario.Id}' capture failed: {exception}");
			}

			try
			{
				currentScenario.OnCaptured?.Invoke();
			}
			catch (Exception exception)
			{
				ModLog.Error($"ui-test '{currentScenario.Id}' action failed: {exception}");
			}

			waitFrames = afterCaptureFrames;
			phase = Phase.Closing;
		}

		private static void CloseCurrent()
		{
			if (currentWindow != null)
			{
				try
				{
					currentWindow.Close(false);
				}
				catch (Exception exception)
				{
					ModLog.Error($"ui-test close failed: {exception}");
				}
				currentWindow = null;
			}

			index++;
			waitFrames = 3;
			phase = Phase.Opening;
		}

		private static void Finish()
		{
			phase = Phase.Idle;
			ModLog.Message($"ui-test: done, captured={captured}, skipped={skipped}");
		}

		private static Window OpenNotificationSettings()
		{
			ModTemplateMod mod = ModTemplateMod.Instance;
			if (mod == null)
			{
				return null;
			}

			mod.SelectSettingsPage("notifications");
			Dialog_ModSettings dialog = new Dialog_ModSettings(mod)
			{
				forcePause = false
			};
			return dialog;
		}

		private static readonly List<FloatMenuOption> pawnMenuOptions = new List<FloatMenuOption>();

		private static Window OpenPawnMenu()
		{
			pawnMenuOptions.Clear();

			Map map = Find.CurrentMap;
			Pawn pawn = map?.mapPawns?.FreeColonists?.FirstOrDefault();
			if (pawn == null)
			{
				return null;
			}

			pawnMenuOptions.AddRange(PawnMenuService.GetOptions(pawn));
			if (pawnMenuOptions.Count == 0)
			{
				return null;
			}

			return new FloatMenu(pawnMenuOptions)
			{
				vanishIfMouseDistant = false
			};
		}

		private static void ClickFirstPawnMenuOption()
		{
			if (pawnMenuOptions.Count == 0)
			{
				ModLog.Warning("ui-test: no pawn menu option to click");
				return;
			}

			Action action = pawnMenuOptions[0].action;
			if (action == null)
			{
				ModLog.Warning("ui-test: first pawn menu option has no action");
				return;
			}

			action();
			ModLog.Message("ui-test: first pawn menu option executed");
		}

		private static void ClickDemoButton()
		{
			MainButtonDef def = DefDatabase<MainButtonDef>
				.GetNamedSilentFail(MainButtonService.DemoButtonDefName);
			if (def == null)
			{
				ModLog.Warning("ui-test: demo bottom button def missing");
				return;
			}

			def.Worker.InterfaceTryActivate();
			ModLog.Message("ui-test: demo bottom button activated");
		}
	}
}
