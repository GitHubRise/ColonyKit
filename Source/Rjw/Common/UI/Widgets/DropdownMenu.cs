using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace rjw.RJWUI.RJWidgets;

/// <summary>
/// DropdownMenu 的选项条目。
/// 在构造函数中一次性翻译 <c>labelKey</c> 与 <c>tooltipKey</c>；
/// 并保留原始键，使搜索能索引该选项，而无需调用方重新列出它们。
/// 无法解析的键会回显字面文本，参见 <see cref="DropdownMenu.Literal"/>。
/// </summary>
public readonly struct DropdownOption
{
	public readonly string label;
	public readonly string? tooltip;
	public readonly Action action;
	/// <summary>选项标签的翻译键；通过 <see cref="DropdownMenu.Literal"/> 构造时则为字面文本。</summary>
	public readonly string? searchLabelKey;
	/// <summary>选项提示的翻译键（若有）。</summary>
	public readonly string? searchTooltipKey;

	public DropdownOption(string labelKey, Action action, string? tooltipKey = null)
	{
		label = labelKey.Translate();
		this.action = action;
		tooltip = tooltipKey?.Translate();
		searchLabelKey = labelKey;
		searchTooltipKey = tooltipKey;
	}
}

/// <summary>
/// DropdownMenu 的视觉样式。
/// </summary>
public enum DropdownStyle
{
	/// <summary>
	/// 带灰色边框的透明方框。
	/// </summary>
	Custom,
	/// <summary>
	/// RimWorld 的棕色按钮背景。
	/// </summary>
	VanillaButton
}

public static class DropdownOptionExtensions
{
	/// <summary>
	/// 把一个选项保存的翻译条目追加到 <paramref name="keys"/>。
	/// </summary>
	/// <param name="option">保存标签条目与提示条目的选项。</param>
	/// <param name="keys">接收这些条目的列表。</param>
	/// <remarks>
	/// <para>先追加标签条目，若存在提示条目再追加。空条目会被跳过。</para>
	/// </remarks>
	/// <seealso cref="DropdownMenu"/>
	public static void AddKeysTo(this in DropdownOption option, List<string> keys)
	{
		if (!string.IsNullOrEmpty(option.searchLabelKey))
			keys.Add(option.searchLabelKey!);
		if (!string.IsNullOrEmpty(option.searchTooltipKey))
			keys.Add(option.searchTooltipKey!);
	}
}

/// <summary>
/// 下拉式选项选择器：带边框和箭头的方框，菜单锚定在其下方。
/// </summary>
[StaticConstructorOnStartup]
public static class DropdownMenu
{
	private static readonly Color BorderColor = Color.grey;
	private static readonly Color ArrowColor = Color.grey;

	private static Texture2D? triangleTex;

	private static Texture2D TriangleTex => triangleTex ??= CreateDownTriangleTexture(16, 16);

	/// <summary>
	/// 创建一个使用字面显示文本的选项。
	/// </summary>
	/// <param name="text">在菜单中显示并用于搜索的文本。</param>
	/// <param name="action">选中该选项时运行的回调。</param>
	/// <param name="tooltipLiteral">为该选项显示的提示文本。为 null 则隐藏提示。</param>
	/// <returns>一个带有给定文本与回调的新 <see cref="DropdownOption"/>。</returns>
	/// <remarks>
	/// <para>当文本已经是最终文本时使用此方法。若要使用翻译标识符，请直接调用 <see cref="DropdownOption"/> 构造函数。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var option = DropdownMenu.Literal("Enable", () => value = true);
	/// </code>
	/// </example>
	public static DropdownOption Literal(string text, Action action, string? tooltipLiteral = null)
	{
		return new DropdownOption(text, action, tooltipLiteral);
	}

	/// <summary>
	/// 绘制下拉方框，并在按下时打开锚定菜单。
	/// </summary>
	/// <param name="rect">方框的区域，包括右侧的箭头区。</param>
	/// <param name="currentText">显示在方框内的文本。</param>
	/// <param name="options">菜单打开时显示的选项。</param>
	/// <param name="tooltip">该方框的提示。为 null 则隐藏提示。</param>
	/// <param name="style">视觉样式。<see cref="DropdownStyle.Custom"/> 绘制带边框的方框，<see cref="DropdownStyle.VanillaButton"/> 绘制 RimWorld 按钮。</param>
	/// <param name="disabled">为 true 时绘制变暗的外观并阻止输入。</param>
	/// <returns>本帧打开了菜单时为 true，其他情况为 false。</returns>
	/// <remarks>
	/// <para>用内部纹理绘制箭头，并把菜单放在 <paramref name="rect"/> 下方，同时限制在屏幕内。</para>
	/// </remarks>
	/// <seealso cref="Literal"/>
	public static bool Draw(Rect rect, string currentText, List<DropdownOption> options, string? tooltip = null, DropdownStyle style = DropdownStyle.Custom, bool disabled = false)
	{
		float arrowSize = rect.height;
		Rect arrowRect = new(rect.xMax - arrowSize, rect.y, arrowSize, arrowSize);
		Rect textRect = new(rect.x, rect.y, rect.width - arrowSize, rect.height);

		using (new UiScope(color: disabled ? new Color(0.7f, 0.7f, 0.7f) : GUI.color))
		{
			if (style == DropdownStyle.VanillaButton)
			{
				if (disabled)
					Widgets.DrawAtlas(rect, Widgets.ButtonBGAtlas);
				else
					Widgets.DrawButtonGraphic(rect);
				using (new UiScope(color: Color.black))
				{
					Widgets.DrawLineVertical(arrowRect.x, rect.y, rect.height);
				}
			}
			else
			{
				using (new UiScope(color: BorderColor))
				{
					Widgets.DrawBox(rect);
					Widgets.DrawLineVertical(arrowRect.x, rect.y, rect.height);
				}
				if (!disabled)
					Widgets.DrawHighlightIfMouseover(rect);
			}

			using (new UiScope(anchor: TextAnchor.MiddleCenter))
			{
				Widgets.Label(textRect, currentText.Truncate(textRect.width - 8f));
			}

			Rect caretRect = arrowRect.ContractedBy(arrowSize * 0.28f);
			using (new UiScope(color: style == DropdownStyle.VanillaButton ? new Color(0.8f, 0.8f, 0.8f) : ArrowColor))
			{
				GUI.DrawTexture(caretRect, TriangleTex);
			}
		}

		if (!string.IsNullOrEmpty(tooltip))
			TooltipHandler.TipRegion(rect, tooltip);

		if (disabled)
			return false;

		bool pressed = Widgets.ButtonInvisible(rect);
		if (pressed)
			ShowAnchored(rect, options);
		return pressed;
	}

	private static void ShowAnchored(Rect rect, List<DropdownOption> options)
	{
		var menuOptions = new List<FloatMenuOption>(options.Count);
		foreach (DropdownOption opt in options)
		{
			var menuOption = new FloatMenuOption(opt.label, opt.action);
			string? tip = opt.tooltip;
			if (!string.IsNullOrEmpty(tip))
				menuOption.tooltip = new TipSignal(tip);
			menuOptions.Add(menuOption);
		}
		Find.WindowStack.Add(new AnchoredFloatMenu(menuOptions, Verse.UI.GUIToScreenPoint(new Vector2(rect.x, rect.yMax)), rect.width));
	}

	private static Texture2D CreateDownTriangleTexture(int width, int height)
	{
		var texture = new Texture2D(width, height);
		for (int y = 0; y < height; y++)
		{
			float halfWidth = width * 0.5f * y / height;
			float leftEdge = (width - 2f * halfWidth) / 2f;
			for (int x = 0; x < width; x++)
				texture.SetPixel(x, y, x >= leftEdge && x <= width - leftEdge ? Color.white : Color.clear);
		}
		texture.Apply();
		return texture;
	}

	private sealed class AnchoredFloatMenu : FloatMenu
	{
		private readonly Vector2 anchor;
		private readonly float minWidth;
		private Vector2 menuScrollPosition;

		public AnchoredFloatMenu(List<FloatMenuOption> options, Vector2 anchor, float minWidth) : base(options)
		{
			this.anchor = anchor;
			this.minWidth = minWidth;
			vanishIfMouseDistant = false;
		}

		public override Vector2 InitialSize => new(Mathf.Max(base.InitialSize.x, minWidth), base.InitialSize.y);

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (!IsOpen)
				return;
			// 逐帧轮询的兜底方案：DoWindowContents 中的 Event.current 在
			// 滚动时并不可靠，因为背景滚动视图（先绘制）会先使用该事件，
			// 而 Unity 只会把与位置相关的事件路由到光标下的窗口。
			// 无论 IMGUI 是否消费或路由，Input.mouseScrollDelta 都能看到每次滚动。
			Vector2 wheel = Input.mouseScrollDelta;
			if (Mathf.Abs(wheel.x) > 0.001f || Mathf.Abs(wheel.y) > 0.001f)
			{
				// 在菜单上滚动会滚动它自己的列表；在别处滚动则意味着
				// 锚定按钮可能已移动，因此将其关闭以避免菜单失去锚点。
				// windowRect 与 MousePositionOnUIInverted 共用按 UI 缩放的屏幕空间。
				if (!windowRect.Contains(UI.MousePositionOnUIInverted))
					Close(doCloseSound: false);
				return;
			}
			// 输入文字同样会因失去锚点而关闭。鼠标按键被排除在外
			// （由下面的 closeOnClickedOutside/MouseDown 路径处理）。Escape/Return 也
			// 被排除，交由原版的取消/确认处理（含音效）负责。
			if (Input.anyKeyDown
				&& !Input.GetMouseButtonDown(0)
				&& !Input.GetMouseButtonDown(1)
				&& !Input.GetMouseButtonDown(2)
				&& !Input.GetKeyDown(KeyCode.Escape)
				&& !Input.GetKeyDown(KeyCode.Return)
				&& !Input.GetKeyDown(KeyCode.KeypadEnter))
				Close(doCloseSound: false);
		}

		public override void DoWindowContents(Rect rect)
		{
			Text.Font = GameFont.Small;
			GUI.color = Color.white;
			float totalHeight = 1f;
			foreach (FloatMenuOption opt in options)
				totalHeight += opt.RequiredHeight - 1f;
			bool needsScroll = totalHeight > rect.height + 0.5f;
			float contentWidth = needsScroll ? rect.width - 16f : rect.width;
			if (needsScroll)
				Widgets.BeginScrollView(rect, ref menuScrollPosition, new Rect(0f, 0f, contentWidth, totalHeight));
			float y = 0f;
			bool chosen = false;
			foreach (FloatMenuOption opt in options)
			{
				Rect row = new(0f, y, contentWidth, opt.RequiredHeight);
				y += opt.RequiredHeight - 1f;
				if (opt.DoGUI(row, false, this))
				{
					Find.WindowStack.TryRemove(this);
					chosen = true;
					break;
				}
			}
			if (needsScroll)
				Widgets.EndScrollView();
			if (chosen)
				return;
			// 滚动/输入时关闭：按钮可能移动或消失，使菜单失去锚点。
			// 这是快速的 IMGUI 路径；上面的 WindowUpdate() 处理的是
			// 已被其他窗口消费、或未路由到本窗口的滚动和按键。
			Event evt = Event.current;
			if (evt != null)
			{
				if (evt.type == EventType.ScrollWheel)
				{
					// 在可滚动菜单上的滚动由它自己的滚动视图处理
					// （后者会使用该事件）；此处仍可见的内容要么位于
					// 无需滚动的短菜单上，要么完全在菜单之外。
					if (!needsScroll || !windowRect.Contains(UI.MousePositionOnUIInverted))
					{
						Close(doCloseSound: false);
						return;
					}
				}
				else if (evt.type == EventType.KeyDown)
				{
					Close(doCloseSound: false);
					return;
				}
				if (evt.type == EventType.MouseDown)
				{
					evt.Use();
					Close();
				}
			}
			GUI.color = Color.white;
		}

		protected override void SetInitialSizeAndPosition()
		{
			Vector2 pos = anchor;
			if (pos.x + InitialSize.x > Verse.UI.screenWidth)
				pos.x = Verse.UI.screenWidth - InitialSize.x;
			if (pos.y + InitialSize.y > Verse.UI.screenHeight)
				pos.y = Verse.UI.screenHeight - InitialSize.y;
			if (pos.x < 0f)
				pos.x = 0f;
			if (pos.y < 0f)
				pos.y = 0f;
			windowRect = new Rect(pos.x, pos.y, InitialSize.x, InitialSize.y);
		}
	}
}
