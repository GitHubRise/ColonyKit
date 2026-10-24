using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 模块自带设置数据的契约：实现类由 <see cref="ColonyKitSettings.moduleSettings"/> 统一持久化。
	/// 新增模块设置只需实现本接口，不需要修改 <see cref="ColonyKitSettings"/> 的字段列表。
	/// </summary>
	public interface IColonyKitModuleSettings : IExposable
	{
		/// <summary>稳定标识，用于日志/迁移；发布后不要改。</summary>
		string SettingsKey { get; }

		/// <summary>把非法值夹回合法范围；返回是否发生了修改。</summary>
		bool Validate();
	}
}
