using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 设置页抽象：一个页签 = 一个 <see cref="SettingsPage"/> 子类。
	/// 只负责往 <see cref="Listing_Standard"/> 里追加控件，不处理标签栏、滚动、保存。
	/// </summary>
	/// <remarks>
	/// 把这些交给 <see cref="SettingsController"/> 统一调度，页面就能保持成
	/// 「纯 UI + 读写 settings」的小文件，新增页不需要改窗口代码。
	/// </remarks>
	public abstract class SettingsPage
	{
		/// <summary>
		/// 稳定标识。用于记住滚动位置；不要用翻译后的标题当 key。
		/// </summary>
		public abstract string Id { get; }

		/// <summary>页签标题的翻译键，由控制器负责 Translate。</summary>
		public abstract string TitleKey { get; }

		/// <summary>排序值，小的排前面（10、20、90…）。</summary>
		public abstract int Order { get; }

		/// <summary>
		/// 是否显示这个页。留给以后按 DLC / 其他 Mod 条件隐藏页面用。
		/// </summary>
		public virtual bool Visible => true;

		/// <summary>
		/// 是否支持“重置本页”。只读页（比如关于页）返回 false，控制器会隐藏按钮。
		/// </summary>
		public virtual bool CanReset => true;

		/// <summary>
		/// 往 listing 里追加本页控件。滚动和边距由控制器处理，这里只管行内容。
		/// </summary>
		public abstract void DoContents(Listing_Standard listing, ColonyKitSettings settings);

		/// <summary>
		/// 恢复本页默认值。默认空实现（只读页无需重置）。
		/// 只改内存，落盘由窗口关闭时的 <see cref="Mod.WriteSettings"/> 统一完成。
		/// </summary>
		public virtual void Reset(ColonyKitSettings settings)
		{
		}
	}
}
