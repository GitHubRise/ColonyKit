using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 对局级组件：每个存档一份，随存档读写，适合放「整局游戏」级别的状态。
	/// </summary>
	/// <remarks>
	/// 游戏用 <c>Activator.CreateInstance(type, game)</c> 反射实例化所有
	/// <see cref="GameComponent"/> 子类（见 <c>Verse.Game.FillComponents</c>），
	/// 所以**构造函数必须接收一个 <see cref="Game"/> 参数**，且不需要任何 XML 注册。
	///
	/// 不要在这里做重活：<see cref="GameComponentTick"/> 每 tick 都会被调用。
	/// </remarks>
	public class DemoGameComponent : GameComponent
	{
		/// <summary>当前存档数据版本；字段/语义变化时递增。</summary>
		private const int CurrentSaveVersion = 1;

		/// <summary>累计统计到的殖民者生成次数，随存档持久化。</summary>
		private int spawnsTracked;

		/// <summary>下一次写汇总日志的 tick。-1 表示还没初始化。</summary>
		private int nextLogTick = -1;

		/// <summary>存档 schema 版本；0 表示旧存档。</summary>
		private int saveVersion = CurrentSaveVersion;

		/// <param name="game">
		/// 反射实例化时传入。本模板用不到，但签名不能省，否则运行时构造失败。
		/// </param>
		public DemoGameComponent(Game game)
		{
		}

		/// <summary>只读计数，方便以后做 UI / 调试动作时读取。</summary>
		public int SpawnsTracked => spawnsTracked;

		public override void ExposeData()
		{
			// 这两个 key 是存档契约：改名会让老存档丢数据。
			Scribe_Values.Look(ref saveVersion, "saveVersion", 0);
			Scribe_Values.Look(ref spawnsTracked, "spawnsTracked", 0);
			Scribe_Values.Look(ref nextLogTick, "nextLogTick", -1);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				MigrateSave();
			}
		}

		/// <summary>
		/// 存档数据迁移：旧存档没有版本号，按 v0 处理；字段未变时只补版本号。
		/// 以后改字段/语义就在这里按 from 版本补迁移步骤。
		/// </summary>
		private void MigrateSave()
		{
			if (saveVersion >= CurrentSaveVersion)
			{
				return;
			}

			int from = saveVersion;
			saveVersion = CurrentSaveVersion;
			ModLog.Info("Migration", $"save data migrated {from} -> {CurrentSaveVersion}");
		}

		/// <summary>
		/// 开发用开关：
		///   - <c>-modtemplate-techtree</c> 自动打开科技树窗口；
		///   - <c>-modtemplate-selftest</c> 打印菜单/通知/建造/主菜单服务的自检结果。
		/// 正常游戏不会触发。
		/// </summary>
		public override void FinalizeInit()
		{
			base.FinalizeInit();

			// 新游戏/读档后 tick 会重新计数，让各模块清掉上一局留下的静态状态。
			ModuleRegistry.NotifyGameLoaded(Current.Game);

			if (GenCommandLine.CommandLineArgPassed("modtemplate-techtree") && Find.WindowStack != null)
			{
				ModLog.Info("Debug", "opening tech tree window (-modtemplate-techtree)");
				Find.WindowStack.Add(new TechTreeWindow());
			}

			if (GenCommandLine.CommandLineArgPassed("modtemplate-selftest"))
			{
				RunSelfTest();
			}

			if (GenCommandLine.CommandLineArgPassed("modtemplate-ui-test"))
			{
				UiAutomationService.RunAll();
			}
		}

		private static void RunSelfTest()
		{
			Map map = Find.CurrentMap;
			AcceptanceReport buildReport = map != null
				? BuildControlService.CanPlace(ThingDefOf.Wall, map.Center, map, Rot4.North, ThingDefOf.Steel)
				: AcceptanceReport.WasRejected;

			MainButtonDef button = DefDatabase<MainButtonDef>
				.GetNamedSilentFail(MainButtonService.TechTreeButtonDefName);

			ModLog.Info(
				"Selftest",
				$"modules={ModuleRegistry.All.Count}, schema={ModTemplateMod.Settings?.schemaVersion}, " +
				$"pawnMenu={PawnMenuService.Count}, buildRules={BuildControlService.RuleCount}, " +
				$"notifications={NotificationService.Enabled}, " +
				$"mainButton={(button != null && MainButtonService.IsVisible(button))}, " +
				$"wallPlacement={buildReport.Accepted}");

			// 验证级别过滤：默认 Info 下 Debug 行不会出现；切到 Debug 后可见。
			ModLog.Debug("Selftest", "debug-level line");
			ModLog.Info("Selftest", "info-level line");
			ModLog.Warn("Selftest", "warn-level line");

			Diagnostics.Dump();
			NotificationService.Message("ModTemplate.Notification.SelfTest".Translate(), MessageTypeDefOf.NeutralEvent);
		}

		public override void GameComponentTick()
		{
			// 每 tick 都会进来，先做最便宜的判断再取 TickManager。
			// Settings 为 null 只可能发生在极早期，判空避免日志刷屏式的空引用。
			LoggingSettings logging = ModTemplateMod.Settings?.logging;
			if (logging == null || logging.detail == LogDetail.Off)
			{
				return;
			}

			int now = Find.TickManager.TicksGame;
			if (nextLogTick < 0)
			{
				nextLogTick = now + logging.logIntervalTicks;
				return;
			}

			if (now < nextLogTick)
			{
				return;
			}

			nextLogTick = now + logging.logIntervalTicks;
			ModLog.Info("GameComponent", $"tick {now}: tracked {spawnsTracked} colonist spawn(s)");
		}

		/// <summary>
		/// 由 Harmony 补丁在 <c>Pawn.SpawnSetup</c> 之后调用，主线程。
		/// 只累加计数，不碰 Unity API，也不做耗时操作。
		/// </summary>
		public void NotifyColonistSpawned(Pawn pawn)
		{
			spawnsTracked++;

			// 明细日志要同时满足「Verbose 级别」和「明细开关」；后者在设置页里
			// 只有 Verbose 时可勾选，这里再判一次，防止改级别后旧值残留。
			LoggingSettings logging = ModTemplateMod.Settings?.logging;
			if (logging != null && logging.detail == LogDetail.Verbose && logging.verboseSpawnLog)
			{
				ModLog.Info("GameComponent", $"spawn: {pawn.LabelShort} ({pawn.def.defName})");
			}
		}
	}
}
