using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace rjw.RJWUI.RJWidgets;

/// <summary>
/// 设置窗口的可搜索作用域。
/// 未筛选时：绘制所有行，表头立即绘制。
/// 筛选时：只绘制匹配项，父级按需绘制；被禁用的小节保持隐藏，匹配的隐藏子项
///            会把祖先标记为上下文。标题匹配则显示整组。
/// 索引（<see cref="NewIndexScope"/>）：对同一标签页主体做一次静默的第二轮运行，记录
///            是否有任何内容会匹配，但不绘制。用于按标签页判断“是否有匹配项”，
///            这样就不必手工维护行列表。在索引模式下，所有行都必须经过
///            本作用域的 API：直接绘制在 <see cref="Listing"/> 上的行对它不可见。
/// <example>
/// 示例：
/// <code>
/// using (var g = sec.Group("Title")) { g.Checkbox(...); }
/// </code>
/// </example>
/// </summary>
public sealed class SearchSection : IDisposable
{
	private readonly SearchSection? parent;
	private readonly Listing_Standard parentListing;
	private readonly Listing_Standard activeListing;
	private readonly Listing_Standard? ownedSection;
	private readonly QuickSearchFilter? filter;
	private readonly bool filtering;
	private readonly bool indexing;
	private readonly bool active;
	private readonly bool titleHit;
	private readonly string? titleKey;
	private readonly string? titleTipKey;
	private readonly string[]? titleExtra;
	private readonly float leadingGap;
	private readonly float trailingGap;
	private readonly string? toggleLabelKey;
	private readonly string? toggleTipKey;
	private readonly bool toggleSelfHit;
	private readonly bool toggleInitialValue;

	private readonly Action<bool>? toggleWriteBack;
	private bool toggleDrawn;
	private bool toggleDrawnValue;
	private bool headerDrawn;
	private bool disposed;
	private float contentHeight;

	/// <summary>
	/// 当本作用域（或其子作用域）内有任意一行匹配筛选条件时为 true。
	/// 仅在筛选时有效；其他情况下无意义。
	/// </summary>
	public bool AnyMatched { get; private set; }

	private void RecordHit()
	{
		for (SearchSection? s = this; s != null; s = s.parent)
			s.AnyMatched = true;
	}

	/// <summary>
	/// 用于绘制的 Listing（已打开的子小节，否则为父级）。
	/// </summary>
	public Listing_Standard Listing => activeListing;

	public bool Filtering => filtering;

	/// <summary>
	/// 因游戏条件被隐藏时（并非筛选）为 false。
	/// </summary>
	public bool Active => active;

	/// <summary>
	/// 调用 <see cref="Dispose"/> 之后的内部高度（用于缓存小节高度）。
	/// </summary>
	public float ContentHeight => contentHeight;

	private SearchSection(
		SearchSection? parent,
		Listing_Standard parentListing,
		Listing_Standard activeListing,
		Listing_Standard? ownedSection,
		QuickSearchFilter? filter,
		bool filtering,
		bool indexing,
		bool active,
		bool titleHit,
		string? titleKey,
		string? titleTipKey,
		string[]? titleExtra,
		float leadingGap,
		float trailingGap,
		string? toggleLabelKey,
		string? toggleTipKey,
		string[]? toggleExtra,
		bool toggleSelfHit,
		bool toggleInitialValue,
		Action<bool>? toggleWriteBack)
	{
		this.parent = parent;
		this.parentListing = parentListing;
		this.activeListing = activeListing;
		this.ownedSection = ownedSection;
		this.filter = filter;
		this.filtering = filtering;
		this.indexing = indexing;
		this.active = active;
		this.titleHit = titleHit;
		this.titleKey = titleKey;
		this.titleTipKey = titleTipKey;
		this.titleExtra = titleExtra;
		this.leadingGap = leadingGap;
		this.trailingGap = trailingGap;
		this.toggleLabelKey = toggleLabelKey;
		this.toggleTipKey = toggleTipKey;
		this.toggleSelfHit = toggleSelfHit;
		this.toggleInitialValue = toggleInitialValue;
		this.toggleWriteBack = toggleWriteBack;
	}

	/// <summary>
	/// 单个标签页的根作用域：永不隐藏。
	/// </summary>
	public SearchSection(Listing_Standard listing, QuickSearchFilter? filter)
		: this(null, listing, listing, null, filter, filter is { Active: true },
			false, true, false, null, null, null, 0f, 0f, null, null, null, false, false, null)
	{
	}

	/// <summary>
	/// 为索引运行创建一个静默作用域。
	/// </summary>
	/// <param name="filter">用于匹配检查的快速筛选器。</param>
	/// <returns>一个记录匹配但不绘制的新作用域。</returns>
	/// <remarks>
	/// <para>在本作用域内运行同一标签页主体，即可得知是否有行会匹配。在本作用域之外绘制的行对它仍不可见。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (var index = SearchSection.NewIndexScope(filter)) { DrawBody(index, true); }
	/// return index.AnyMatched;
	/// </code>
	/// </example>
	public static SearchSection NewIndexScope(QuickSearchFilter filter)
	{
		return new SearchSection(null, IndexSink, IndexSink, null, filter, true, true,
			true, false, null, null, null, 0f, 0f, null, null, null, false, false, null);
	}

	private static readonly Listing_Standard IndexSink = NewSink();

	private static Listing_Standard NewSink()
	{
		var sink = new Listing_Standard();
		sink.Begin(new Rect(0f, 0f, 0f, 0f));
		return sink;
	}

	private bool EffectiveTitleHit => titleHit || (parent != null && parent.titleHit);

	/// <summary>
	/// 用筛选条件检查一行，并在该行将显示时绘制表头。
	/// </summary>
	/// <param name="labelKey">标签标识符，匹配前会先翻译。</param>
	/// <param name="tooltipKey">提示标识符，匹配前会先翻译。为 null 则跳过此项。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <returns>调用方应绘制该行时为 true；该行被隐藏或处于索引模式时为 false。</returns>
	/// <remarks>
	/// <para>正常模式下返回 true。筛选模式下，匹配时返回 true，并在本作用域及其父级上记录命中。</para>
	/// </remarks>
	/// <seealso cref="RowTranslated"/>
	public bool Row(string labelKey, string? tooltipKey = null, params string[]? extraKeysOrLiterals)
	{
		if (!active)
		{
			if (!filtering)
				return false;
			// 隐藏的匹配：把被禁用的祖先标记为上下文。
			if (!Search.Matches(filter, labelKey, tooltipKey, extraKeysOrLiterals) && !EffectiveTitleHit)
				return false;
			RecordHit();
			if (!indexing)
				EnsureHeadersDrawnAsContext();
			return false;
		}
		if (!filtering)
			return true;
		bool hit = Search.Matches(filter, labelKey, tooltipKey, extraKeysOrLiterals) || EffectiveTitleHit;
		if (hit)
			RecordHit();
		else
			return false;
		if (indexing)
			return false;
		EnsureHeadersDrawn();
		return true;
	}

	/// <summary>
	/// 用已翻译的行文本检查筛选条件。
	/// </summary>
	/// <param name="textA">要搜索的第一段文本。</param>
	/// <param name="textB">要搜索的第二段文本。为 null 时只搜索 <paramref name="textA"/>。</param>
	/// <returns>调用方应绘制该行时为 true；该行被隐藏或处于索引模式时为 false。</returns>
	/// <remarks>
	/// <para>每个词元都必须出现在 <paramref name="textA"/> 或 <paramref name="textB"/> 中。实际检查会调用 <see cref="Search.MatchesTranslated"/>。</para>
	/// </remarks>
	/// <seealso cref="Row"/>
	public bool RowTranslated(string textA, string? textB = null)
	{
		if (!active)
		{
			if (!filtering)
				return false;
			if (!Search.MatchesTranslated(filter, textA, textB) && !EffectiveTitleHit)
				return false;
			RecordHit();
			if (!indexing)
				EnsureHeadersDrawnAsContext();
			return false;
		}
		if (!filtering)
			return true;
		if (Search.MatchesTranslated(filter, textA, textB) || EffectiveTitleHit)
		{
			RecordHit();
			if (!indexing)
				EnsureHeadersDrawn();
			return !indexing;
		}
		return false;
	}

	/// <summary>
	/// 绘制一行带标签的复选框。
	/// </summary>
	/// <param name="labelKey">标签标识符，匹配和绘制前会先翻译。</param>
	/// <param name="tooltipKey">提示标识符。为 null 则隐藏提示。</param>
	/// <param name="value">当前状态。在用户输入后更新。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>；<see cref="Row"/> 返回 false 时不绘制任何内容。</para>
	/// </remarks>
	/// <seealso cref="Row"/>
	public void Checkbox(string labelKey, string? tooltipKey, ref bool value, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, tooltipKey, extraKeysOrLiterals))
			return;
		activeListing.CheckboxLabeled(labelKey.Translate(), ref value, Tip(tooltipKey));
	}

	/// <summary>
	/// 绘制一行带标签的浮点滑块。
	/// </summary>
	/// <param name="labelKey">标签标识符，匹配和绘制前会先翻译。</param>
	/// <param name="tooltipKey">提示标识符。为 null 则隐藏提示。</param>
	/// <param name="value">当前值。在用户输入后更新。</param>
	/// <param name="min">滑块的下界。</param>
	/// <param name="max">滑块的上界。</param>
	/// <param name="valueText">为标签格式化 <paramref name="value"/> 的函数。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>；<see cref="Row"/> 返回 false 时不绘制任何内容。</para>
	/// </remarks>
	/// <seealso cref="SliderPercent"/>
	public void Slider(string labelKey, string? tooltipKey, ref float value, float min, float max, Func<float, string> valueText, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, tooltipKey, extraKeysOrLiterals))
			return;
		activeListing.Label(labelKey.Translate() + ": " + valueText(value), -1f, new TipSignal(Tip(tooltipKey)));
		value = activeListing.Slider(value, min, max);
	}

	/// <summary>
	/// 绘制一行带标签的整数滑块。
	/// </summary>
	/// <param name="labelKey">标签标识符，匹配和绘制前会先翻译。</param>
	/// <param name="tooltipKey">提示标识符。为 null 则隐藏提示。</param>
	/// <param name="value">当前值。在用户输入后更新。</param>
	/// <param name="min">滑块的下界。</param>
	/// <param name="max">滑块的上界。</param>
	/// <param name="valueText">为标签格式化 <paramref name="value"/> 的函数。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>；<see cref="Row"/> 返回 false 时不绘制任何内容。</para>
	/// </remarks>
	/// <seealso cref="SliderPercent"/>
	public void Slider(string labelKey, string? tooltipKey, ref int value, int min, int max, Func<int, string> valueText, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, tooltipKey, extraKeysOrLiterals))
			return;
		activeListing.Label(labelKey.Translate() + ": " + valueText(value), -1f, new TipSignal(Tip(tooltipKey)));
		value = (int)activeListing.Slider(value, min, max);
	}

	/// <summary>
	/// 绘制一行带标签的百分比滑块。
	/// </summary>
	/// <param name="labelKey">标签标识符，匹配和绘制前会先翻译。</param>
	/// <param name="tooltipKey">提示标识符。为 null 则隐藏提示。</param>
	/// <param name="value">当前比例，取值在 <paramref name="min"/> 到 <paramref name="max"/> 之间。在用户输入后更新。</param>
	/// <param name="min">滑块的下界。</param>
	/// <param name="max">滑块的上界。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <remarks>
	/// <para>调用带百分比格式的浮点 <c>Slider</c> 重载。</para>
	/// </remarks>
	public void SliderPercent(string labelKey, string? tooltipKey, ref float value, float min, float max, params string[]? extraKeysOrLiterals)
	{
		Slider(labelKey, tooltipKey, ref value, min, max, v => Math.Round(v * 100f, 0) + "%", extraKeysOrLiterals);
	}

	/// <summary>
	/// 绘制一行占满宽度的按钮。
	/// </summary>
	/// <param name="labelKey">用于匹配的标签标识符。</param>
	/// <param name="tooltipKey">用于匹配的提示标识符。按钮本身不显示提示。</param>
	/// <param name="text">返回按钮文本的函数。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <returns>本帧按钮被按下时为 true。</returns>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>；<see cref="Row"/> 返回 false 时不绘制任何内容。</para>
	/// </remarks>
	public bool ButtonText(string labelKey, string? tooltipKey, Func<string> text, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, tooltipKey, extraKeysOrLiterals))
			return false;
		return activeListing.ButtonText(text());
	}

	/// <summary>
	/// 绘制一行带标签的按钮，并附带一个侧边按钮。
	/// </summary>
	/// <param name="labelKey">用于匹配的标签标识符。</param>
	/// <param name="tooltipKey">用于匹配的提示标识符。</param>
	/// <param name="label">返回左侧标签文本的函数。</param>
	/// <param name="button">返回按钮文本的函数。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <returns>本帧按钮被按下时为 true。</returns>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>；<see cref="Row"/> 返回 false 时不绘制任何内容。</para>
	/// </remarks>
	public bool ButtonTextLabeled(string labelKey, string? tooltipKey, Func<string> label, Func<string> button, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, tooltipKey, extraKeysOrLiterals))
			return false;
		return activeListing.ButtonTextLabeled(label(), button(), TextAnchor.MiddleLeft, null, Tip(tooltipKey));
	}

	/// <summary>
	/// 绘制一行带标签的按钮，标签与按钮使用各自的提示。
	/// </summary>
	/// <param name="labelKey">用于匹配的标签标识符。</param>
	/// <param name="labelTooltipKey">用于匹配的标签提示标识符。</param>
	/// <param name="label">返回左侧标签文本的函数。</param>
	/// <param name="button">返回按钮文本的函数。</param>
	/// <param name="buttonTooltip">按钮的提示。为 null 则隐藏按钮提示。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <returns>本帧按钮被按下时为 true。</returns>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>，<see cref="Row"/> 返回 false 时不绘制任何内容。标签与按钮的提示绘制在各自区域。</para>
	/// </remarks>
	public bool ButtonTextLabeledWithButtonTooltip(string labelKey, string? labelTooltipKey, Func<string> label, Func<string> button, string? buttonTooltip, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, labelTooltipKey, extraKeysOrLiterals))
			return false;
		float labelPct = 0.5f;
		string labelText = label();
		string buttonText = button();
		float height = Math.Max(Text.CalcHeight(labelText, activeListing.ColumnWidth * labelPct), 30f);
		Rect rect = activeListing.GetRect(height);
		Rect buttonRect = rect.RightPart(1f - labelPct);
		buttonRect.height = 30f;
		Rect labelRect = rect.LeftPart(labelPct);
		bool result;
		using (new UiScope(anchor: TextAnchor.MiddleLeft))
		{
			Widgets.Label(labelRect, labelText);
			result = Widgets.ButtonText(buttonRect, buttonText.Truncate(buttonRect.width - 20f));
		}
		string? labelTip = Tip(labelTooltipKey);
		if (!labelTip.NullOrEmpty())
		{
			if (Mouse.IsOver(labelRect))
			{
				Widgets.DrawHighlight(labelRect);
			}
			TooltipHandler.TipRegion(labelRect, labelTip);
		}
		if (!buttonTooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(buttonRect, buttonTooltip);
		}
		activeListing.Gap(activeListing.verticalSpacing);
		return result;
	}

	private static string? Tip(string? tooltipKey)
	{
		return tooltipKey == null ? null : tooltipKey.Translate();
	}

	/// <summary>
	/// 下拉选项的搜索键，使选项自身的标签/提示可被找到，
	/// 而无需调用方把它们再列为额外项。
	/// </summary>
	private static string[]? OptionKeys(List<DropdownOption> options, string[]? extraKeysOrLiterals)
	{
		int extraCount = extraKeysOrLiterals?.Length ?? 0;
		var keys = new List<string>(extraCount + options.Count * 2);
		for (int i = 0; i < extraCount; i++)
			keys.Add(extraKeysOrLiterals![i]);
		for (int i = 0; i < options.Count; i++)
			options[i].AddKeysTo(keys);
		return keys.Count == 0 ? null : keys.ToArray();
	}

	/// <summary>
	/// 绘制一行占满宽度的下拉框。
	/// </summary>
	/// <param name="labelKey">用于匹配的标签标识符。</param>
	/// <param name="tooltipKey">用于匹配的提示标识符。</param>
	/// <param name="text">返回当前按钮文本的函数。</param>
	/// <param name="tooltip">返回按钮提示的函数。为 null 则隐藏提示。</param>
	/// <param name="options">菜单打开时显示的选项。它们保存的条目会参与匹配。</param>
	/// <param name="style">传给 <see cref="DropdownMenu.Draw"/> 的视觉样式。</param>
	/// <param name="disabled">为 true 时绘制变暗的外观并阻止输入。</param>
	/// <param name="extraKeysOrLiterals">更多参与匹配的标识符或字面文本。</param>
	/// <returns>本帧菜单被打开时为 true。</returns>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>，<see cref="Row"/> 返回 false 时不绘制任何内容。选项条目会自动加入匹配，无需调用方操作。</para>
	/// </remarks>
	/// <seealso cref="DropdownLabeledWithOptionTooltips"/>
	public bool Dropdown(string labelKey, string? tooltipKey, Func<string> text, Func<string?> tooltip, List<DropdownOption> options, DropdownStyle style = DropdownStyle.Custom, bool disabled = false, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, tooltipKey, OptionKeys(options, extraKeysOrLiterals)))
			return false;
		return DropdownMenu.Draw(activeListing.GetRect(30f), text(), options, tooltip(), style, disabled);
	}

	/// <summary>
	/// 绘制一行带侧边按钮的带标签下拉框。
	/// </summary>
	/// <param name="labelKey">用于匹配的标签标识符。</param>
	/// <param name="labelTooltipKey">用于匹配的标签提示标识符。</param>
	/// <param name="label">返回左侧标签文本的函数。</param>
	/// <param name="button">返回按钮文本的函数。</param>
	/// <param name="buttonTooltip">返回按钮提示的函数。</param>
	/// <param name="options">菜单打开时显示的选项。其保存的条目参与匹配。</param>
	/// <param name="style">传给 <see cref="DropdownMenu.Draw"/> 的视觉样式。</param>
	/// <param name="disabled">为 true 时以暗色绘制并阻止输入。</param>
	/// <param name="extraKeysOrLiterals">参与匹配的更多标识符或字面字符串。</param>
	/// <returns>本帧打开了菜单时为 true。</returns>
	/// <remarks>
	/// <para>先调用 <see cref="Row"/>，当 <see cref="Row"/> 返回 false 时不绘制任何内容。标签提示绘制在标签区域内。</para>
	/// </remarks>
	/// <seealso cref="Dropdown"/>
	public bool DropdownLabeledWithOptionTooltips(string labelKey, string? labelTooltipKey, Func<string> label, Func<string> button, Func<string?> buttonTooltip, List<DropdownOption> options, DropdownStyle style = DropdownStyle.Custom, bool disabled = false, params string[]? extraKeysOrLiterals)
	{
		if (!Row(labelKey, labelTooltipKey, OptionKeys(options, extraKeysOrLiterals)))
			return false;
		float labelPct = 0.5f;
		string labelText = label();
		float height = Math.Max(Text.CalcHeight(labelText, activeListing.ColumnWidth * labelPct), 30f);
		Rect rect = activeListing.GetRect(height);
		Rect buttonRect = rect.RightPart(1f - labelPct);
		buttonRect.height = 30f;
		Rect labelRect = rect.LeftPart(labelPct);
		bool result;
		using (new UiScope(anchor: TextAnchor.MiddleLeft))
		{
			Widgets.Label(labelRect, labelText);
			result = DropdownMenu.Draw(buttonRect, button(), options, buttonTooltip(), style, disabled);
		}
		string? labelTip = Tip(labelTooltipKey);
		if (!labelTip.NullOrEmpty())
		{
			if (Mouse.IsOver(labelRect))
			{
				Widgets.DrawHighlight(labelRect);
			}
			TooltipHandler.TipRegion(labelRect, labelTip);
		}
		activeListing.Gap(activeListing.verticalSpacing);
		return result;
	}

	/// <summary>
	/// 开启一个带标题的分组作用域。
	/// </summary>
	/// <param name="titleKey">标题标识符，在匹配和绘制之前先被翻译。</param>
	/// <param name="titleTipKey">标题提示标识符。为 null 时隐藏提示。</param>
	/// <param name="leadingGap">普通模式下标题前绘制的间距。</param>
	/// <param name="drawTitle">自定义标题写入器。为 null 时绘制默认标题标签。</param>
	/// <param name="titleExtraKeysOrLiterals">参与标题匹配的更多标识符或字面字符串。</param>
	/// <returns>分组内各行使用的新子作用域。释放该作用域即可关闭它。</returns>
	/// <remarks>
	/// <para>在筛选模式下，标题在分组内首次匹配时绘制。标题匹配会显示分组内的所有行。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (var g = sec.Group("TitleKey")) { g.Checkbox("RowKey", null, ref value); }
	/// </code>
	/// </example>
	/// <seealso cref="Nested"/>
	public SearchSection Group(string titleKey, string? titleTipKey = null, float leadingGap = 12f, Action<Listing_Standard>? drawTitle = null, params string[]? titleExtraKeysOrLiterals)
	{
		if (!active)
		{
			if (!filtering)
				return InactiveChild();
			// 保留键，这样隐藏的匹配仍会把此项标记为上下文。
			bool inactiveHit = Search.Matches(filter, titleKey, titleTipKey, titleExtraKeysOrLiterals);
			if (inactiveHit)
				RecordHit();
			var inactiveGroup = new SearchSection(this, activeListing, activeListing, null, filter, true, indexing, false,
				inactiveHit || EffectiveTitleHit, titleKey, titleTipKey, titleExtraKeysOrLiterals, leadingGap, 0f, null, null, null, false, false, null);
			inactiveGroup.customTitle = drawTitle;
			return inactiveGroup;
		}
		bool hit = filtering && Search.Matches(filter, titleKey, titleTipKey, titleExtraKeysOrLiterals);
		if (hit)
			RecordHit();
		var group = new SearchSection(this, activeListing, activeListing, null, filter, filtering, indexing, true,
			hit || EffectiveTitleHit, titleKey, titleTipKey, titleExtraKeysOrLiterals, leadingGap, 0f, null, null, null, false, false, null);
		group.customTitle = drawTitle;
		if (!filtering)
			group.DrawTitleNow();
		return group;
	}

	private Action<Listing_Standard>? customTitle;

	/// <summary>
	/// 开启一个无标题的子作用域。
	/// </summary>
	/// <param name="condition">为 true 时开启活动作用域。为 false 时开启隐藏作用域。</param>
	/// <param name="sectionHeight">普通模式下为内部区段保留的高度。</param>
	/// <param name="gapAfter">普通模式下在区段之后绘制的间距。</param>
	/// <returns>新的子作用域。释放该作用域即可关闭它。</returns>
	/// <remarks>
	/// <para>在筛选模式下，作用域保持扁平，其中的行使用父级列表。隐藏作用域内的匹配会记录到祖先上。</para>
	/// </remarks>
	/// <seealso cref="Group"/>
	public SearchSection Nested(bool condition, float sectionHeight, float gapAfter = 4f)
	{
		if (!active)
			return InactiveChild();
		if (!filtering)
		{
			if (!condition)
				return InactiveChild();
			Listing_Standard section = activeListing.BeginSection(sectionHeight);
			return new SearchSection(this, activeListing, section, section, filter, false, indexing, true,
				EffectiveTitleHit, null, null, null, 0f, gapAfter, null, null, null, false, false, null);
		}
		if (!condition)
			return new SearchSection(this, activeListing, activeListing, null, filter, true, indexing, false,
				EffectiveTitleHit, null, null, null, 0f, 0f, null, null, null, false, false, null);
		return new SearchSection(this, activeListing, activeListing, null, filter, true, indexing, true,
			EffectiveTitleHit, null, null, null, 0f, 0f, null, null, null, false, false, null);
	}

	/// <summary>
	/// 开启一个带方框子项的开关父级。
	/// </summary>
	/// <param name="labelKey">在匹配和绘制之前被翻译的标签标识符。</param>
	/// <param name="tooltipKey">提示标识符。为 null 时隐藏提示。</param>
	/// <param name="currentValue">当前开关状态。</param>
	/// <param name="writeBack">接收绘制状态的回调。</param>
	/// <param name="sectionHeight">普通模式下为内部区段保留的高度。</param>
	/// <param name="gapAfter">普通模式下在区段之后绘制的间距。</param>
	/// <param name="onSectionHeight">普通模式下接收测得高度的回调。为 null 时跳过该回调。</param>
	/// <param name="extraKeysOrLiterals">参与匹配的更多标识符或字面字符串。</param>
	/// <returns>开关内各行使用的新子作用域。释放该作用域即可关闭它。</returns>
	/// <remarks>
	/// <para>表头复选框始终绘制。在筛选模式下，被关闭的开关内部的匹配会记录到父级上。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (var t = sec.Toggle("ToggleKey", null, flag, v => flag = v, 200f)) { t.Checkbox("RowKey", null, ref inner); }
	/// </code>
	/// </example>
	public SearchSection Toggle(string labelKey, string? tooltipKey, bool currentValue, Action<bool>? writeBack, float sectionHeight, float gapAfter = 4f, Action<float>? onSectionHeight = null, params string[]? extraKeysOrLiterals)
	{
		if (!active)
		{
			if (!filtering)
				return InactiveChild();
			bool inactiveSelfHit = Search.Matches(filter, labelKey, tooltipKey, extraKeysOrLiterals);
			if (inactiveSelfHit)
				RecordHit();
			return new SearchSection(this, activeListing, activeListing, null, filter, true, indexing, false,
				EffectiveTitleHit, null, null, null, 0f, 0f, labelKey, tooltipKey, extraKeysOrLiterals, inactiveSelfHit, currentValue, writeBack);
		}
		if (!filtering)
		{
			bool value = currentValue;
			parentListing.CheckboxLabeled(labelKey.Translate(), ref value, Tip(tooltipKey));
			writeBack?.Invoke(value);
			if (!value)
			{
				var off = new SearchSection(this, activeListing, activeListing, null, filter, false, false, false,
					EffectiveTitleHit, null, null, null, 0f, 0f, null, null, null, false, false, null);
				off.headerDrawn = true;
				return off;
			}
			Listing_Standard section = activeListing.BeginSection(sectionHeight);
			var on = new SearchSection(this, activeListing, section, section, filter, false, false, true,
				EffectiveTitleHit, null, null, null, 0f, gapAfter, null, null, null, false, false, null);
			on.headerDrawn = true;
			on.onSectionHeightTarget = onSectionHeight;
			return on;
		}
		bool selfHit = Search.Matches(filter, labelKey, tooltipKey, extraKeysOrLiterals);
		if (selfHit)
			RecordHit();
		if (!currentValue)
		{
			// 已禁用：隐藏子项，让表头仍可作为上下文被找到。
			return new SearchSection(this, activeListing, activeListing, null, filter, true, indexing, false,
				EffectiveTitleHit, null, null, null, 0f, 0f, labelKey, tooltipKey, extraKeysOrLiterals, selfHit, currentValue, writeBack);
		}
		return new SearchSection(this, activeListing, activeListing, null, filter, true, indexing, true,
			EffectiveTitleHit, null, null, null, 0f, 0f, labelKey, tooltipKey, extraKeysOrLiterals, selfHit, currentValue, writeBack);
	}

	private Action<float>? onSectionHeightTarget;

	private SearchSection InactiveChild()
	{
		return new SearchSection(this, activeListing, activeListing, null, filter, filtering, indexing, false,
			EffectiveTitleHit, null, null, null, 0f, 0f, null, null, null, false, false, null);
	}

	private void EnsureHeadersDrawn()
	{
		if (headerDrawn)
			return;
		parent?.EnsureHeadersDrawn();
		DrawOwnHeader();
		headerDrawn = true;
	}

	private void EnsureHeadersDrawnAsContext()
	{
		if (headerDrawn)
			return;
		parent?.EnsureHeadersDrawnAsContext();
		DrawOwnHeaderAsContext();
		headerDrawn = true;
	}

	private void DrawOwnHeader()
	{
		if (titleKey != null)
			DrawTitleNow();
		else if (toggleLabelKey != null)
			DrawToggleNow();
	}

	private void DrawOwnHeaderAsContext()
	{
		if (titleKey != null)
			DrawTitleAsContext();
		else if (toggleLabelKey != null)
			DrawToggleAsContext();
		// 嵌套项没有表头：只标记祖先。
	}

	private void DrawTitleNow()
	{
		if (headerDrawn)
			return;
		if (leadingGap != 0f)
			activeListing.Gap(leadingGap);
		if (customTitle != null)
			customTitle(activeListing);
		else
		{
			using (UiScope.title)
			{
				activeListing.Label(titleKey!.Translate());
			}
		}
		headerDrawn = true;
	}

	private void DrawToggleNow()
	{
		if (headerDrawn)
			return;
		bool value = toggleDrawn ? toggleDrawnValue : toggleInitialValue;
		parentListing.CheckboxLabeled(toggleLabelKey!.Translate(), ref value, Tip(toggleTipKey));
		toggleDrawn = true;
		toggleDrawnValue = value;
		toggleWriteBack?.Invoke(value);
		headerDrawn = true;
	}

	private void DrawTitleAsContext()
	{
		if (headerDrawn)
			return;
		if (leadingGap != 0f)
			activeListing.Gap(leadingGap);
		if (customTitle != null)
		{
			// 自定义标题：不加标记地显示。
			customTitle(activeListing);
		}
		else
		{
			using (UiScope.title)
			{
				activeListing.Label(titleKey!.Translate() + " (" + "RJWSettingsSearchHasHiddenMatch".Translate() + ")");
			}
		}
		headerDrawn = true;
	}

	private void DrawToggleAsContext()
	{
		if (headerDrawn)
			return;
		bool value = toggleDrawn ? toggleDrawnValue : toggleInitialValue;
		string hiddenMark = "RJWSettingsSearchHasHiddenMatch".Translate();
		string hiddenDesc = "RJWSettingsSearchHasHiddenMatch_desc".Translate();
		string? tip = Tip(toggleTipKey);
		string fullTip = string.IsNullOrEmpty(tip) ? hiddenDesc : tip + "\n\n" + hiddenDesc;
		parentListing.CheckboxLabeled(toggleLabelKey!.Translate() + " (" + hiddenMark + ")", ref value, fullTip);
		toggleDrawn = true;
		toggleDrawnValue = value;
		toggleWriteBack?.Invoke(value);
		headerDrawn = true;
	}

	/// <summary>
	/// 关闭作用域并写入区段页脚。
	/// </summary>
	/// <remarks>
	/// <para>在普通模式下结束内部区段并记录内容高度。在筛选模式下为自身匹配绘制待处理的表头。</para>
	/// </remarks>
	/// <seealso cref="Group"/>
	public void Dispose()
	{
		if (disposed)
			return;
		disposed = true;
		if (ownedSection != null && !filtering)
		{
			contentHeight = ownedSection.MaxColumnHeightSeen;
			parentListing.EndSection(ownedSection);
			if (trailingGap != 0f)
				parentListing.Gap(trailingGap);
			onSectionHeightTarget?.Invoke(contentHeight);
			return;
		}
		if (filtering && !indexing && !headerDrawn && (titleKey != null || toggleLabelKey != null))
		{
			if (!active)
			{
				// 受限表头：自身匹配则绘制，标题匹配则标记为上下文。
				bool self = titleKey != null
					? Search.Matches(filter, titleKey, titleTipKey, titleExtra)
					: toggleSelfHit;
				if (self)
					EnsureHeadersDrawn();
				else if (EffectiveTitleHit)
					EnsureHeadersDrawnAsContext();
				return;
			}
			// 孤立表头：自身匹配但没有可见的后代。
			bool selfHit = titleKey != null
				? Search.Matches(filter, titleKey, titleTipKey, titleExtra) || EffectiveTitleHit
				: toggleSelfHit || EffectiveTitleHit;
			if (selfHit)
				EnsureHeadersDrawn();
		}
	}
}
