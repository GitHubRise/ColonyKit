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
			// 展示方式由常规页的 controlMode 决定；Cycle 模式下复用同一行布局。
			if (ModTemplateMod.Settings?.general?.controlMode == SettingsControlMode.Cycle)
			{
				T result = Cycle(listing, label, current, options, optionLabel);
				if (!EqualityComparer<T>.Default.Equals(result, current))
				{
					onChanged(result);
				}
				return;
			}

			if (ModTemplateMod.Settings?.general?.controlMode == SettingsControlMode.CheckCross)
			{
				T result = CheckCross(listing, label, current, options, optionLabel);
				if (!EqualityComparer<T>.Default.Equals(result, current))
				{
					onChanged(result);
				}
				return;
			}

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
		/// ✓ / ✗ 列表：每个选项一行，选中打 ✓、未选中打 ✗，点击整行切换。
		/// 适合 2–5 个选项，比下拉少一次点击。
		/// </summary>
		public static T CheckCross<T>(
			Listing_Standard listing,
			string label,
			T current,
			IEnumerable<T> options,
			Func<T, string> optionLabel)
		{
			if (!BeginControl(listing, label, null))
			{
				return current;
			}

			List<T> list = options?.ToList() ?? new List<T>();
			if (list.Count == 0)
			{
				return current;
			}

			Widgets.Label(listing.GetRect(Text.LineHeight), label);

			T result = current;
			foreach (T option in list)
			{
				bool selected = EqualityComparer<T>.Default.Equals(option, current);
				string text = (selected ? "✓ " : "✗ ") + optionLabel(option);
				Rect row = listing.GetRect(24f);

				if (Widgets.ButtonText(row, text))
				{
					result = option;
				}

				TooltipHandler.TipRegion(row, "ModTemplate.Settings.CheckCross.Select".Translate());
			}

			listing.Gap();
			return result;
		}

		/// <summary>
		/// ◀ / ▶ 循环选择：左侧标签，右侧「上一项 / 当前值 / 下一项」。
		/// 首尾相接；当前值不在选项里时，第一次点击落到第一项（◀ 落到最后一项）。
		/// </summary>
		public static T Cycle<T>(
			Listing_Standard listing,
			string label,
			T current,
			IEnumerable<T> options,
			Func<T, string> optionLabel)
		{
			if (!BeginControl(listing, label, null))
			{
				return current;
			}

			List<T> list = options?.ToList() ?? new List<T>();
			if (list.Count == 0)
			{
				return current;
			}

			int index = list.FindIndex(option =>
				EqualityComparer<T>.Default.Equals(option, current));

			Rect rect = listing.GetRect(30f);
			Widgets.Label(rect.LeftPart(0.42f), label);

			Rect right = rect.RightPart(0.58f);
			Rect previousRect = new Rect(right.x, right.y, 30f, right.height);
			Rect nextRect = new Rect(right.xMax - 30f, right.y, 30f, right.height);
			Rect valueRect = new Rect(
				previousRect.xMax,
				right.y,
				Mathf.Max(0f, nextRect.x - previousRect.xMax),
				right.height);

			int target = index;

			if (Widgets.ButtonText(previousRect, "◀"))
			{
				target = index < 0 ? list.Count - 1 : (index == 0 ? list.Count - 1 : index - 1);
			}
			else if (Widgets.ButtonText(nextRect, "▶"))
			{
				target = index < 0 ? 0 : (index >= list.Count - 1 ? 0 : index + 1);
			}

			TooltipHandler.TipRegion(previousRect, "ModTemplate.Settings.Cycle.Previous".Translate());
			TooltipHandler.TipRegion(nextRect, "ModTemplate.Settings.Cycle.Next".Translate());

			string shown = index >= 0 ? optionLabel(list[index]) : optionLabel(current);
			TextAnchor previousAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(valueRect, shown);
			Text.Anchor = previousAnchor;

			listing.Gap();
			return target >= 0 && target < list.Count ? list[target] : current;
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
