using ColonyKit.Modules.Robotics;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 对局级组件：每个存档一份，负责把「新游戏 / 读档」事件分发给模块。
	/// </summary>
	/// <remarks>
	/// 游戏用 <c>Activator.CreateInstance(type, game)</c> 反射实例化所有
	/// <see cref="GameComponent"/> 子类（见 <c>Verse.Game.FillComponents</c>），
	/// 所以**构造函数必须接收一个 <see cref="Game"/> 参数**，且不需要任何 XML 注册。
	///
	/// 不要在这里做重活：<see cref="GameComponentTick"/> 每 tick 都会被调用。
	/// </remarks>
	public class ColonyKitGameComponent : GameComponent
	{
		/// <param name="game">反射实例化时传入；签名不能省，否则运行时构造失败。</param>
		public ColonyKitGameComponent(Game game)
		{
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();

			// 新游戏/读档后让各模块恢复状态、清掉上一局残留。
			ModuleRegistry.NotifyGameLoaded(Current.Game);
		}

		public override void GameComponentTick()
		{
			base.GameComponentTick();
			if (Find.TickManager.TicksGame % 30 == 0)
			{
				// AI 设置页「测试连接」的结果只能在主线程弹消息。
				MkVAiService.DrainTestMessages();
			}
		}
	}
}
