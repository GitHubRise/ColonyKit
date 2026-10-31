using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>智能机器人模块：当前落地保姆机器人（清洁/扫地/搬运 + 充电）。</summary>
	public sealed class RoboticsModule : ColonyKitModule
	{
		public override string Id => "robotics";

		public override string DisplayNameKey => "ColonyKit.Module.Robotics.Name";

		public override string DescriptionKey => "ColonyKit.Module.Robotics.Desc";

		public override int InitOrder => 140;

		public override void OnDefsLoaded()
		{
			bool race = DefDatabase<ThingDef>.GetNamedSilentFail("CK_RobotNanny") != null;
			bool kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CK_RobotNannyKind") != null;
			bool station = DefDatabase<ThingDef>.GetNamedSilentFail("CK_NannyBotStation") != null;
			bool thinkTree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("CK_NannyBot") != null;
			bool cleanJob = DefDatabase<JobDef>.GetNamedSilentFail("CK_NannyCleanFilth") != null;
			bool chargeJob = DefDatabase<JobDef>.GetNamedSilentFail("CK_NannyCharge") != null;
			bool energyNeed = DefDatabase<NeedDef>.GetNamedSilentFail("CK_RobotEnergy") != null;
			bool mkvKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CK_RobotMkVKind") != null;
			bool mkvBay = DefDatabase<ThingDef>.GetNamedSilentFail("CK_MkVAssemblyBay") != null;
			ResearchProjectDef mkvProject = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("CK_RobotMkV");
			int mkvPrereqs = mkvProject?.prerequisites?.Count ?? 0;
			bool workshop = DefDatabase<ThingDef>.GetNamedSilentFail("CK_WorkshopFactory") != null;
			bool twoStation = DefDatabase<ThingDef>.GetNamedSilentFail("CK_TwoStationFactory") != null;
			bool frame = DefDatabase<ThingDef>.GetNamedSilentFail("CK_FrameFactory") != null;
			bool prefab = DefDatabase<ThingDef>.GetNamedSilentFail("CK_PrefabFactory") != null;
			bool unmanned = DefDatabase<ThingDef>.GetNamedSilentFail("CK_UnmannedFactory") != null;
			bool mk1 = DefDatabase<PawnKindDef>.GetNamedSilentFail("CK_NannyBotMkIKind") != null;
			bool mk2 = DefDatabase<PawnKindDef>.GetNamedSilentFail("CK_NannyBotMkIIKind") != null;
			bool mk3 = DefDatabase<PawnKindDef>.GetNamedSilentFail("CK_NannyBotMkIIIKind") != null;
			bool mk4 = DefDatabase<PawnKindDef>.GetNamedSilentFail("CK_NannyBotMkIVKind") != null;

			ModLog.Info(
				"Robotics",
				$"module loaded; race={race}, kind={kind}, station={station}, thinkTree={thinkTree}, " +
				$"cleanJob={cleanJob}, chargeJob={chargeJob}, energyNeed={energyNeed}, " +
				$"mkvKind={mkvKind}, mkvBay={mkvBay}, mkvResearch={mkvProject != null}, mkvPrereqs={mkvPrereqs}, " +
				$"factories={workshop}/{twoStation}/{frame}/{prefab}/{unmanned}, mkKinds={mk1}/{mk2}/{mk3}/{mk4}");

			// 设置文件在 Mod 构造时就已经读好，这里把电弧刃的面板数值写进 Def。
			MkVArcBlade.ApplySettings();

			if (GenCommandLine.CommandLineArgPassed(MkVAiSelfTest.CommandLineKey))
			{
				MkVAiSelfTest.Run();
			}
		}

		public override void OnGameLoaded(Game game)
		{
			if (game == null)
			{
				return;
			}

			NannyBotSelftest.RunIfRequested(game);

			// 读档后重新同步一次（Def 不会存档，数值存在设置里）。
			MkVArcBlade.ApplySettings();

			int robots = 0;
			int withEnergy = 0;
			foreach (Map map in game.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (!NannyBotUtility.IsNannyBot(pawn))
					{
						continue;
					}
					robots++;
					if (NannyBotUtility.Energy(pawn) != null)
					{
						withEnergy++;
					}
				}
			}

			if (robots > 0)
			{
				ModLog.Info("Robotics", $"nanny bots on maps={robots}, withEnergy={withEnergy}");
			}
		}
	}
}
