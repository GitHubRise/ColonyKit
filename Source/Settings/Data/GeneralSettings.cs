using System;
using Verse;

namespace ModTemplate
{
	/// <summary>枚举/少选项控件的展示方式。</summary>
	public enum SettingsControlMode
	{
		/// <summary>点击按钮弹出 FloatMenu 选择。</summary>
		Dropdown,

		/// <summary>用 ◀ / ▶ 逐个循环，首尾相接。</summary>
		Cycle,

		/// <summary>每个选项一行，选中打 ✓、未选中打 ✗。</summary>
		CheckCross
	}

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

		/// <summary>选项控件展示方式：下拉框或 ◀ / ▶ 循环。</summary>
		public SettingsControlMode controlMode = SettingsControlMode.Dropdown;

		public void ExposeData()
		{
			// key 是配置文件契约，发布后不要改名。
			Scribe_Values.Look(ref trackSpawns, "trackSpawns", true);
			Scribe_Values.Look(ref enablePawnMenu, "enablePawnMenu", true);
			Scribe_Values.Look(ref showTechTreeButton, "showTechTreeButton", true);
			Scribe_Values.Look(ref controlMode, "controlMode", SettingsControlMode.Dropdown);
		}

		/// <summary>恢复本页默认值。只改内存，关窗时统一落盘。</summary>
		public void Reset()
		{
			trackSpawns = true;
			enablePawnMenu = true;
			showTechTreeButton = true;
			controlMode = SettingsControlMode.Dropdown;
		}

		/// <summary>校验枚举合法；非法值回退下拉框。</summary>
		public bool Validate()
		{
			if (!Enum.IsDefined(typeof(SettingsControlMode), controlMode))
			{
				controlMode = SettingsControlMode.Dropdown;
				return true;
			}

			return false;
		}
	}
}
