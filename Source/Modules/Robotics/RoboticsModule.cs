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

			ModLog.Info(
				"Robotics",
				$"module loaded; race={race}, kind={kind}, station={station}, thinkTree={thinkTree}, " +
				$"cleanJob={cleanJob}, chargeJob={chargeJob}, energyNeed={energyNeed}");
		}

		public override void OnGameLoaded(Game game)
		{
			if (game == null)
			{
				return;
			}

			NannyBotSelftest.RunIfRequested(game);

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
