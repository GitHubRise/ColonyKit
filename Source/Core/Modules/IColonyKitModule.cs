using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 模块契约：每个可选功能实现一个模块，由 <see cref="ModuleRegistry"/> 统一初始化。
	/// </summary>
	/// <remarks>
	/// 模块只负责「生命周期与开关」，具体服务仍可以是静态类；
	/// 模块被禁用时，服务读取 <see cref="ModuleRegistry.IsEnabled"/> 后提前返回。
	/// </remarks>
	public interface IColonyKitModule
	{
		/// <summary>稳定标识，用于设置持久化与日志。</summary>
		string Id { get; }

		/// <summary>设置页显示名的翻译键。</summary>
		string DisplayNameKey { get; }

		/// <summary>设置页说明的翻译键。</summary>
		string DescriptionKey { get; }

		/// <summary>初始化顺序，小者先。</summary>
		int InitOrder { get; }

		/// <summary>锁定模块（如 core）始终启用，不允许关闭。</summary>
		bool Locked { get; }

		/// <summary>默认是否启用；首次运行时使用。</summary>
		bool EnabledByDefault { get; }

		/// <summary>Def 就绪后调用一次（可安全读 Def）。</summary>
		void OnDefsLoaded();

		/// <summary>新游戏 / 读档后调用。</summary>
		void OnGameLoaded(Game game);

		/// <summary>清空本模块的静态缓存/节流状态。</summary>
		void ResetRuntimeState();
	}

	/// <summary>模块基类：只实现默认值，业务模块按需覆写。</summary>
	public abstract class ColonyKitModule : IColonyKitModule
	{
		public abstract string Id { get; }

		public abstract string DisplayNameKey { get; }

		public abstract string DescriptionKey { get; }

		public virtual int InitOrder => 100;

		public virtual bool Locked => false;

		public virtual bool EnabledByDefault => true;

		public virtual void OnDefsLoaded()
		{
		}

		public virtual void OnGameLoaded(Game game)
		{
		}

		public virtual void ResetRuntimeState()
		{
		}
	}
}
