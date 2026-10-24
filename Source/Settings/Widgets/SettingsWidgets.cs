using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 设置页的通用控件库：把原版零散的 Widgets 调用包成「一行一个控件」，
	/// 页面代码只关心值和标签，不用重复摆 Rect。所有控件都参与检索过滤。
	/// </summary>
	/// <remarks>
	/// 检索启用时，控件先检查标签/Tooltip/当前 Section 是否包含查询：
	/// 不匹配就跳过绘制并返回原值；匹配时补画之前挂起的 Section。
	/// </remarks>
	public static class SettingsWidgets
	{
		private static string pendingSection;

		/// <summary>每页开始绘制前重置挂起的 Section。</summary>
		public static void BeginPage()
		{
			pendingSection = null;
		}

		/// <summary>
		/// 小标题 + 分隔线，用来在同一个页签里分区。
		/// 检索启用时先挂起，等第一个匹配控件出现再画，避免空标题。
		/// </summary>
		public static void Section(Listing_Standard listing, string title)
		{
			if (SettingsSearch.Active)
			{
				pendingSection = title;
				return;
			}

			DrawSection(listing, title);
		}

		/// <summary>只读文本行，参与检索。</summary>
		public static void Label(Listing_Standard listing, string text)
		{
			if (!BeginControl(listing, text, null))
			{
				return;
			}

			listing.Label(text);
		}

		/// <summary>按钮，参与检索；未命中时返回 false。</summary>
		public static bool Button(Listing_Standard listing, string label)
		{
			if (!BeginControl(listing, label, null))
			{
				return false;
			}

			return listing.ButtonText(label);
		}

		/// <summary>
		/// 勾选框，支持禁用态：<paramref name="enabled"/> 为 false 时灰显且不可点，
		/// Tooltip 仍会显示，用来解释「为什么不能点」。
		/// </summary>
		public static bool Checkbox(
			Listing_Standard listing,
			string label,
			ref bool value,
			string tooltip = null,
			bool enabled = true)
		{
			if (!BeginControl(listing, label, tooltip))
			{
				return value;
			}

			Rect rect = listing.GetRect(Text.LineHeight);
			Widgets.CheckboxLabeled(rect, label, ref value, disabled: !enabled);

			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}

			return value;
		}

		/// <summary>
		/// 滑块：左侧标签，右侧滑条 + 当前值。step &gt; 0 时按步长吸附（例如 600 tick）。
		/// </summary>
		public static float Slider(
			Listing_Standard listing,
			string label,
			float value,
			float min,
			float max,
			float step = -1f,
			Func<float, string> format = null)
		{
			if (!BeginControl(listing, label, null))
			{
				return value;
			}

			Rect rect = listing.GetRect(30f);
			Widgets.Label(rect.LeftPart(0.42f), label);

			// 显示值也按步长吸附，否则拖动过程中会出现 1234 这种中间值。
			float shown = step > 0f ? Mathf.Round(value / step) * step : value;
			string valueLabel = format != null ? format(shown) : shown.ToString("0.##");

			float result = Widgets.HorizontalSlider(
				rect.RightPart(0.58f),
				value,
				min,
				max,
				middleAlignment: true,
				label: valueLabel,
				roundTo: step);

			if (step > 0f)
			{
				result = Mathf.Round(result / step) * step;
			}

			listing.Gap();
			return Mathf.Clamp(result, min, max);
		}

		/// <summary>
		/// 下拉框：点击后弹出原生 <see cref="FloatMenu"/>，当前项带 ✓。
		/// 选项数量不多时最省事；上百项请换带搜索的窗口。
		/// </summary>
		public static void Dropdown<T>(
			Listing_Standard listing,
			string label,
			T current,
			IEnumerable<T> options,
			Func<T, string> optionLabel,
			Action<T> onChanged)
		{
			if (!BeginControl(listing, label, null))
			{
				return;
			}

			Rect rect = listing.GetRect(30f);
			Widgets.Label(rect.LeftPart(0.42f), label);

			if (!Widgets.ButtonText(rect.RightPart(0.58f), optionLabel(current)))
			{
				return;
			}

			List<FloatMenuOption> menu = new List<FloatMenuOption>();
			foreach (T option in options.ToList())
			{
				T captured = option;
				string text = optionLabel(captured);
				if (EqualityComparer<T>.Default.Equals(captured, current))
				{
					text += " ✓";
				}
				menu.Add(new FloatMenuOption(text, () => onChanged(captured)));
			}

			if (menu.Count > 0)
			{
				Find.WindowStack.Add(new FloatMenu(menu));
			}
		}

		/// <summary>
		/// 单行输入框：左侧标签，右侧文本。超过 <paramref name="maxLength"/> 的输入会被丢弃，
		/// 返回上一次的合法值。
		/// </summary>
		public static string TextField(
			Listing_Standard listing,
			string label,
			string value,
			int maxLength = 64,
			string tooltip = null)
		{
			if (!BeginControl(listing, label, tooltip))
			{
				return value;
			}

			Rect rect = listing.GetRect(Text.LineHeight);
			Widgets.Label(rect.LeftPart(0.42f), label);

			string result = Widgets.TextField(rect.RightPart(0.58f), value ?? "", maxLength);
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}

			return result;
		}

		private static bool BeginControl(Listing_Standard listing, string label, string tooltip)
		{
			if (!SettingsSearch.Active)
			{
				return true;
			}

			bool match = SettingsSearch.Matches(label)
				|| SettingsSearch.Matches(tooltip)
				|| SettingsSearch.Matches(pendingSection);

			if (!match)
			{
				return false;
			}

			SettingsSearch.PageMatches++;

			if (!pendingSection.NullOrEmpty())
			{
				DrawSection(listing, pendingSection);
				pendingSection = null;
			}

			return true;
		}

		private static void DrawSection(Listing_Standard listing, string title)
		{
			listing.Gap(10f);
			listing.GapLine(6f);

			Color previousColor = GUI.color;
			GUI.color = new Color(0.82f, 0.82f, 0.82f);
			listing.Label(title);
			GUI.color = previousColor;
		}
	}
}
