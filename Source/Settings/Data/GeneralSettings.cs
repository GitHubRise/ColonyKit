using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 「常规」页的数据。只放字段和 Scribe，不画 UI（UI 在 <see cref="GeneralSettingsPage"/>）。
	/// </summary>
	/// <remarks>
	/// 由 <see cref="ModTemplateSettings"/> 用 <c>Scribe_Deep</c> 落盘，
	/// 所以字段增删只需要考虑本类，不会和其他页互相影响。
	/// </remarks>
	public class GeneralSettings : IExposable
	{
		/// <summary>是否统计殖民者生成。默认开；只读统计，不改变玩法。</summary>
		public bool trackSpawns = true;

		/// <summary>是否启用右键小人菜单里的扩展项。</summary>
		public bool enablePawnMenu = true;

		/// <summary>是否显示底部菜单里的「科技树」按钮。</summary>
		public bool showTechTreeButton = true;

		public void ExposeData()
		{
			// key 是配置文件契约，发布后不要改名。
			Scribe_Values.Look(ref trackSpawns, "trackSpawns", true);
			Scribe_Values.Look(ref enablePawnMenu, "enablePawnMenu", true);
			Scribe_Values.Look(ref showTechTreeButton, "showTechTreeButton", true);
		}

		/// <summary>恢复本页默认值。只改内存，关窗时统一落盘。</summary>
		public void Reset()
		{
			trackSpawns = true;
			enablePawnMenu = true;
			showTechTreeButton = true;
		}
	}
}
