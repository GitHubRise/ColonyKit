using System;
using UnityEngine;
using Verse;

namespace rjw.RJWUI;

/// <summary>
/// 保存和恢复 GUI 状态的辅助类
/// </summary>
readonly ref struct UiScope : IDisposable
{
	private readonly GameFont savedFont;
	private readonly TextAnchor savedAnchor;
	private readonly Color savedColor;
	private readonly bool savedWordWrap;
	private readonly Color savedBackgroundColor;
	private readonly Color savedContentColor;

	private readonly bool restoreFont;
	private readonly bool restoreAnchor;
	private readonly bool restoreColor;
	private readonly bool restoreWordWrap;
	private readonly bool restoreBackgroundColor;
	private readonly bool restoreContentColor;

	/// <summary>
	/// 保存并恢复 GUI 状态。未指定的状态保持不变。
	/// </summary>
	/// <example>
	/// <code>
	/// using (new UiScope(font: GameFont.Medium, anchor: TextAnchor.UpperCenter))
	/// {
	/// 	Widgets.Label(rect, "Label");
	/// }
	/// </code>
	/// 此处会恢复 font 与 anchor
	/// </example>
	/// <param name="font"></param>
	/// <param name="anchor"></param>
	/// <param name="color"></param>
	/// <param name="wordWrap">是否启用文本自动换行；null 表示保持不变。</param>
	/// <param name="backgroundColor">要应用的 GUI 背景色；null 表示保持不变。</param>
	/// <param name="contentColor">要应用的 GUI 内容色；null 表示保持不变。</param>
	public UiScope(
		GameFont? font = null,
		TextAnchor? anchor = null,
		Color? color = null,
		bool? wordWrap = null,
		Color? backgroundColor = null,
		Color? contentColor = null
	)
	{
		// 保存当前值
		savedFont = Text.Font;
		savedAnchor = Text.Anchor;
		savedColor = GUI.color;
		savedWordWrap = Text.WordWrap;
		savedBackgroundColor = GUI.backgroundColor;
		savedContentColor = GUI.contentColor;

		// 仅应用指定的值
		if (font.HasValue)
		{
			Text.Font = font.Value;
			restoreFont = true;
		}
		if (anchor.HasValue)
		{
			Text.Anchor = anchor.Value;
			restoreAnchor = true;
		}
		if (color.HasValue)
		{
			GUI.color = color.Value;
			restoreColor = true;
		}
		if (wordWrap.HasValue)
		{
			Text.WordWrap = wordWrap.Value;
			restoreWordWrap = true;
		}
		if (backgroundColor.HasValue)
		{
			GUI.backgroundColor = backgroundColor.Value;
			restoreBackgroundColor = true;
		}
		if (contentColor.HasValue)
		{
			GUI.contentColor = contentColor.Value;
			restoreContentColor = true;
		}
	}

	/// <summary>
	/// 标题的默认样式
	/// </summary>
	public static UiScope title => new UiScope(font: GameFont.Medium, anchor: TextAnchor.MiddleLeft, color: Color.yellow);

	/// <summary>
	/// 禁用内容的颜色
	/// </summary>
	public static readonly Color DisabledColor = new Color(0.62f, 0.62f, 0.65f);

	/// <summary>
	/// 禁用内容的默认样式
	/// </summary>
	public static UiScope disabled => new UiScope(color: DisabledColor);

	/// <summary>
	/// 正常内容的颜色
	/// </summary>
	public static readonly Color OkColor = new Color(0.55f, 0.85f, 0.6f);

	/// <summary>
	/// 正常内容的默认样式
	/// </summary>
	public static UiScope ok => new UiScope(color: OkColor);

	/// <summary>
	/// 只恢复我们覆盖过的那些
	/// </summary>
	public void Dispose()
	{
		if (restoreFont) Text.Font = savedFont;
		if (restoreAnchor) Text.Anchor = savedAnchor;
		if (restoreColor) GUI.color = savedColor;
		if (restoreWordWrap) Text.WordWrap = savedWordWrap;
		if (restoreBackgroundColor) GUI.backgroundColor = savedBackgroundColor;
		if (restoreContentColor) GUI.contentColor = savedContentColor;
	}
}
