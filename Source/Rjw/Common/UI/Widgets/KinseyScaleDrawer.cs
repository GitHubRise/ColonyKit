using UnityEngine;
using Verse;

namespace rjw.RJWUI.RJWidgets;

[StaticConstructorOnStartup]
public static class KinseyScaleDrawer
{
	// 颜色
	private static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
	private static readonly Color BarBorderColor = new Color(0.5f, 0.5f, 0.5f);
	private static readonly Color DiagonalLineColor = new Color(1f, 1f, 1f, 0.8f);
	private static readonly Color HighlightColor = new Color(0.4f, 0.7f, 0.9f);

	// 条形使用的渐变颜色（异性恋到同性恋）
	private static readonly Color HeteroColor = new Color(0.9f, 0.4f, 0.5f);
	private static readonly Color BiColor = new Color(0.7f, 0.4f, 0.8f);
	private static readonly Color HomoColor = new Color(0.4f, 0.5f, 0.9f);

	private const int NUM_BARS = 7; // 0-6 金赛评分
	private const float LABEL_HEIGHT = 20f;
	private const float DIAGONAL_LINE_WIDTH = 1.5f;


	private static Texture2D TriangleTexture = CreateTriangleTexture(10f, 8f);

	private static Texture2D CreateTriangleTexture(float width, float height)
	{
		Texture2D texture = new Texture2D((int)width, (int)height);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float normalizedY = y / height;
				float halfWidth = width * 0.5f * normalizedY;
				float leftEdge = (width - 2f * halfWidth) / 2f;
				float rightEdge = width - leftEdge;

				if (x >= leftEdge && x <= rightEdge)
					texture.SetPixel(x, y, Color.white);
				else
					texture.SetPixel(x, y, Color.clear);
			}
		}
		texture.Apply();
		return texture;
	}

	/// <summary>
	/// 绘制七格金赛控件，并在 <paramref name="interactive"/> 为 true 时读取点击。
	/// </summary>
	/// <param name="rect">条形及其下方数字标签的区域。</param>
	/// <param name="currentValue">选中的值，范围 0 到 6。传 -1 表示不选择。</param>
	/// <param name="interactive">为 true 时允许点击选取值，为 false 时只绘制只读输出。</param>
	/// <returns>点击后选中的值；其他情况为 <paramref name="currentValue"/>。</returns>
	/// <remarks>
	/// <para>条形颜色从异性恋色调过渡到同性恋色调。选中的条形会向浅蓝色混合，并显示顶部标记。</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// int picked = KinseyScaleDrawer.DrawKinseyScale(rect, current, true);
	/// </code>
	/// </example>
	/// <seealso cref="DrawKinseyScaleSimple"/>
	public static int DrawKinseyScale(Rect rect, int currentValue, bool interactive = false)
	{
		int result = currentValue;

		float barWidth = rect.width / NUM_BARS;
		float barHeight = rect.height - LABEL_HEIGHT;

		Widgets.DrawBoxSolid(rect, BackgroundColor);

		for (int i = 0; i < NUM_BARS; i++)
		{
			float xPos = rect.x + i * barWidth;
			Rect barRect = new Rect(xPos, rect.y, barWidth, barHeight);
			Rect labelRect = new Rect(xPos, rect.y + barHeight, barWidth, LABEL_HEIGHT);

			Color barColor = GetBarColor(i);

			if (i == currentValue)
			{
				barColor = Color.Lerp(barColor, HighlightColor, 0.25f);
			}

			if (interactive && Mouse.IsOver(barRect))
			{
				barColor = Color.Lerp(barColor, Color.white, 0.3f);

				if (Widgets.ButtonInvisible(barRect))
				{
					result = i;
				}

				TooltipHandler.TipRegion(barRect, GetKinseyDescription(i));
			}

			Widgets.DrawBoxSolid(barRect, barColor);

			Widgets.DrawBox(barRect, 1, Texture2D.whiteTexture);

			if (i == currentValue)
			{
				DrawSelectionIndicator(barRect);
			}

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = (i == currentValue) ? Color.white : new Color(0.8f, 0.8f, 0.8f);
			Widgets.Label(labelRect, i.ToString());
		}

		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;

		// 从第 1 格底部到第 5 格顶部绘制对角线
		DrawDiagonalLine(rect, barWidth, barHeight);

		return result;
	}

	private static void DrawDiagonalLine(Rect rect, float barWidth, float barHeight)
	{
		Vector2 lineStart = new Vector2(
			rect.x + (0.5f * barWidth) + (barWidth * 0.5f),
			rect.y + barHeight
		);

		Vector2 lineEnd = new Vector2(
			rect.x + (5.5f * barWidth) + (barWidth * 0.5f),
			rect.y
		);

		Widgets.DrawLine(lineStart, lineEnd, DiagonalLineColor, DIAGONAL_LINE_WIDTH);
	}

	/// <summary>
	/// 创建从异性恋到双性恋再到同性恋的渐变
	/// </summary>
	/// <param name="kinseyValue"></param>
	/// <returns></returns>
	private static Color GetBarColor(int kinseyValue)
	{
		float t = kinseyValue / 6f;

		if (t < 0.5f)
		{
			return Color.Lerp(HeteroColor, BiColor, t * 2f);
		}
		else
		{
			return Color.Lerp(BiColor, HomoColor, (t - 0.5f) * 2f);
		}
	}

	private static void DrawSelectionIndicator(Rect barRect)
	{
		float markerWidth = 10f;
		float markerHeight = 8f;
		Rect triangleRect = new Rect(
			barRect.x + (barRect.width - markerWidth) / 2f,
			barRect.y,
			markerWidth,
			markerHeight
		);
		GUI.DrawTexture(triangleRect, TriangleTexture);
	}

	private static string GetKinseyDescription(int value)
	{
		return value switch
		{
			0 => $"0 - {"SC_Kinsey_Heterosexual".Translate()}",
			1 => $"1 - {"SC_Kinsey_MostlyHeterosexual".Translate()}",
			2 => $"2 - {"SC_Kinsey_LeaningHeterosexual".Translate()}",
			3 => $"3 - {"SC_Kinsey_Bisexual".Translate()}",
			4 => $"4 - {"SC_Kinsey_LeaningHomosexual".Translate()}",
			5 => $"5 - {"SC_Kinsey_MostlyHomosexual".Translate()}",
			6 => $"6 - {"SC_Kinsey_Homosexual".Translate()}",
			_ => "Unknown",
		};
	}

	/// <summary>
	/// 绘制金赛控件，不接收输入。
	/// </summary>
	/// <param name="rect">条形及其下方数字标签的区域。</param>
	/// <param name="currentValue">选中的值，范围 0 到 6。传 -1 表示不选择。</param>
	/// <remarks>
	/// <para>调用 <see cref="DrawKinseyScale"/>，并关闭交互。</para>
	/// </remarks>
	/// <seealso cref="DrawKinseyScale"/>
	public static void DrawKinseyScaleSimple(Rect rect, int currentValue)
	{
		DrawKinseyScale(rect, currentValue, false);
	}
}
