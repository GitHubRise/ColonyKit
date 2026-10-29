using System.Linq;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.WorkPriority
{
	/// <summary>
	/// 工作优先级自检：只在带 -colonykit-workpriority-test 启动时跑一次，
	/// 验证 SetPriority 能写 9、初始化默认值是灭火 1 / 就医 2 / 其余 5。
	/// 用法：RimWorldWin64.exe -quicktest -colonykit-workpriority-test -logfile -
	/// </summary>
	internal static class WorkPrioritySelftest
	{
		public const string CommandLineKey = "-colonykit-workpriority-test";

		private static bool ran;

		public static void RunIfRequested(Game game)
		{
			if (ran || game == null || !GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				return;
			}

			ran = true;
			try
			{
				Run(game);
			}
			catch (System.Exception exception)
			{
				ModLog.Error("WorkPriority", "selftest crashed: " + exception);
			}
		}

		private static void Run(Game game)
		{
			Map map = Find.AnyPlayerHomeMap ?? (game.Maps.Count > 0 ? game.Maps[0] : null);
			Pawn pawn = map?.mapPawns?.FreeColonistsSpawned?
				.FirstOrDefault(p => p.workSettings != null
					&& p.RaceProps.Humanlike
					&& !p.RaceProps.IsMechanoid);
			if (pawn == null)
			{
				ModLog.Warn("WorkPriority", "selftest skipped: no colonist on the map.");
				return;
			}

			Pawn_WorkSettings settings = pawn.workSettings;

			// 原版「手动分配工作优先级」关闭时 GetPriority 一律返回 3，自检期间临时打开。
			bool previousUseWorkPriorities = Find.PlaySettings.useWorkPriorities;
			Find.PlaySettings.useWorkPriorities = true;
			try
			{
				// 1) 写入/读回：9 和 5 都应原样保存。
				WorkTypeDef probe = DefDatabase<WorkTypeDef>.AllDefsListForReading
					.FirstOrDefault(w => !pawn.WorkTypeIsDisabled(w));
				int probeBefore = probe != null ? settings.GetPriority(probe) : -1;
				bool canWrite = false;
				if (probe != null)
				{
					settings.SetPriority(probe, 9);
					canWrite = settings.GetPriority(probe) == 9;
					settings.SetPriority(probe, 5);
					canWrite &= settings.GetPriority(probe) == 5;
					settings.SetPriority(probe, probeBefore);
				}

				// 2) 初始化默认值：灭火 1、就医 2，其余可做的工作都是 5。
				WorkTypeDef fire = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Firefighter");
				WorkTypeDef patient = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Patient");
				int firePriority = fire != null && !pawn.WorkTypeIsDisabled(fire)
					? settings.GetPriority(fire)
					: -1;
				int patientPriority = patient != null && !pawn.WorkTypeIsDisabled(patient)
					? settings.GetPriority(patient)
					: -1;

				int othersChecked = 0;
				int othersWrong = 0;
				foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
				{
					if (pawn.WorkTypeIsDisabled(workType)
						|| workType.defName == WorkPriorityModule.FirefighterDefName
						|| workType.defName == WorkPriorityModule.PatientDefName)
					{
						continue;
					}
					othersChecked++;
					if (settings.GetPriority(workType) != WorkPriorityModule.DefaultPriorityValue)
					{
						othersWrong++;
					}
				}

				bool pass = canWrite
					&& firePriority == WorkPriorityModule.FirefighterPriority
					&& patientPriority == WorkPriorityModule.PatientPriority
					&& othersWrong == 0;

				ModLog.Info("WorkPriority",
					$"selftest: pawn={pawn.LabelShort} max={WorkPriorityModule.MaxPriority} " +
					$"write9And5={canWrite} probe={probe?.defName ?? "null"}:{probeBefore} " +
					$"firefighter={firePriority} patient={patientPriority} " +
					$"others={othersChecked} wrong={othersWrong} pass={pass}");
			}
			finally
			{
				Find.PlaySettings.useWorkPriorities = previousUseWorkPriorities;
			}
		}
	}
}
