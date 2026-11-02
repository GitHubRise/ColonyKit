using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 一个气泡：把 <see cref="LogEntry"/> 的互动文本画在小人头顶，带九宫格背景和淡出。
	/// </summary>
	/// <remarks>
	/// 文本、尺寸、样式都按需缓存：设置里的字号/内边距在绘制时读取，
	/// 所以改设置后不需要重建气泡，只有「显示彩色文字」切换时才调用 <see cref="Rebuild"/>。
	/// </remarks>
	internal sealed class Bubble
	{
		private static readonly Regex ColorTag = new Regex("<\\/?color[^>]*>");
		private static readonly GUIContent Content = new GUIContent();

		private readonly Pawn pawn;
		private readonly LogEntry entry;

		private string text;
		private bool textColored;
		private GUIStyle style;

		public Bubble(Pawn pawn, LogEntry entry)
		{
			this.pawn = pawn;
			this.entry = entry;
		}

		public LogEntry Entry => entry;

		public int Height { get; private set; }

		public int Width { get; private set; }

		private GUIStyle Style => style ?? (style = new GUIStyle(Verse.Text.CurFontStyle)
		{
			alignment = TextAnchor.MiddleCenter,
			clipping = TextClipping.Clip
		});

		/// <summary>画一个气泡；已经完全淡出时返回 false，调用方负责把它移除。</summary>
		public bool Draw(Vector2 pos, bool isSelected, float scale, BubblesSettings settings)
		{
			string body = GetText(settings);

			ScaleFont(ref scale, settings);
			ScaleDimensions(scale, settings, body);
			ScalePadding(scale, settings);

			float posX = pos.x;
			float posY = pos.y;

			Rot4 direction = BubblesFeature.Direction(settings);
			if (direction.IsHorizontal)
			{
				posY -= Height * 0.5f;
			}
			else
			{
				posX -= Width * 0.5f;
			}

			Rect rect = new Rect(
				Mathf.Ceil(posX),
				Mathf.Ceil(posY),
				Width,
				Height);

			float fade = Event.current != null && Event.current.shift && Mouse.IsOver(rect)
				? settings.opacityStart
				: Mathf.Min(GetFade(settings), Mouse.IsOver(rect) ? settings.opacityHover : 1f);

			if (fade <= 0f)
			{
				return false;
			}

			Color background = WithAlpha(
				isSelected ? settings.backgroundSelected : settings.background,
				fade);
			Color foreground = WithAlpha(
				isSelected ? settings.foregroundSelected : settings.foreground,
				fade);

			Color previous = GUI.color;

			GUI.color = background;
			DrawAtlas(rect, BubblesTextures.Inner);

			GUI.color = foreground;
			DrawAtlas(rect, BubblesTextures.Outer);
			GUI.Label(rect, body, Style);

			GUI.color = previous;
			return true;
		}

		/// <summary>『显示彩色文字』切换后清掉缓存，下次绘制重新取值。</summary>
		public void Rebuild()
		{
			text = null;
		}

		private static Color WithAlpha(Color color, float alpha)
		{
			return new Color(color.r, color.g, color.b, color.a * alpha);
		}

		private static void DrawAtlas(Rect rect, Texture2D atlas)
		{
			int scale = Mathf.RoundToInt(Mathf.Min(atlas.width * 0.25f, rect.height * 0.25f, rect.width * 0.25f));

			BubblesCompat.BeginGroupAt(rect);

			// 四角 + 四边 + 中心，各取九宫格的一格。
			Widgets.DrawTexturePart(new Rect(0f, 0f, scale, scale), new Rect(0f, 0f, 0.25f, 0.25f), atlas);
			Widgets.DrawTexturePart(new Rect(rect.width - scale, 0f, scale, scale), new Rect(0.75f, 0f, 0.25f, 0.25f), atlas);
			Widgets.DrawTexturePart(new Rect(0f, rect.height - scale, scale, scale), new Rect(0f, 0.75f, 0.25f, 0.25f), atlas);
			Widgets.DrawTexturePart(new Rect(rect.width - scale, rect.height - scale, scale, scale), new Rect(0.75f, 0.75f, 0.25f, 0.25f), atlas);
			Widgets.DrawTexturePart(new Rect(scale, scale, rect.width - scale * 2f, rect.height - scale * 2f), new Rect(0.25f, 0.25f, 0.5f, 0.5f), atlas);
			Widgets.DrawTexturePart(new Rect(scale, 0f, rect.width - scale * 2f, scale), new Rect(0.25f, 0f, 0.5f, 0.25f), atlas);
			Widgets.DrawTexturePart(new Rect(scale, rect.height - scale, rect.width - scale * 2f, scale), new Rect(0.25f, 0.75f, 0.5f, 0.25f), atlas);
			Widgets.DrawTexturePart(new Rect(0f, scale, scale, rect.height - scale * 2f), new Rect(0f, 0.25f, 0.25f, 0.5f), atlas);
			Widgets.DrawTexturePart(new Rect(rect.width - scale, scale, scale, rect.height - scale * 2f), new Rect(0.75f, 0.25f, 0.25f, 0.5f), atlas);

			BubblesCompat.EndGroupNow();
		}

		private void ScaleFont(ref float scale, BubblesSettings settings)
		{
			Style.fontSize = Mathf.RoundToInt(settings.fontSize * scale);
			scale = Style.fontSize / (float)Mathf.Max(1, settings.fontSize);
		}

		private void ScalePadding(float scale, BubblesSettings settings)
		{
			int paddingX = Mathf.RoundToInt(settings.paddingX * scale);
			int paddingY = Mathf.RoundToInt(settings.paddingY * scale);
			Style.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);
		}

		private void ScaleDimensions(float scale, BubblesSettings settings, string body)
		{
			Content.text = body;
			Width = Mathf.RoundToInt(Mathf.Min(Style.CalcSize(Content).x, settings.widthMax * scale));
			Height = Mathf.RoundToInt(Style.CalcHeight(Content, Width));
		}

		/// <summary>按「显示彩色文字」取值；同一开关下复用缓存，避免每帧重新格式化富文本。</summary>
		private string GetText(BubblesSettings settings)
		{
			bool colored = settings.doTextColors;
			if (text != null && textColored == colored)
			{
				return text;
			}

			string value = entry.ToGameStringFromPOV(pawn);
			text = value == null ? "" : (colored ? value : ColorTag.Replace(value, string.Empty));
			textColored = colored;
			return text;
		}

		private float GetFade(BubblesSettings settings)
		{
			if (Find.TickManager == null)
			{
				return 1f;
			}

			int elapsed = Find.TickManager.TicksAbs - Entry.Tick - settings.fadeStart;
			if (elapsed <= 0)
			{
				return settings.opacityStart;
			}

			if (elapsed > settings.fadeLength)
			{
				return 0f;
			}

			return settings.opacityStart * (1f - elapsed / (float)settings.fadeLength);
		}
	}
}
