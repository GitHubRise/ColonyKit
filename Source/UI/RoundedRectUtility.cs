using UnityEngine;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 运行时生成的圆角矩形贴图 + 九宫格绘制：给子页签这类小控件做圆角背景。
	/// IMGUI 没有圆角 API，这里用一张 24×24 的带 alpha 贴图按九宫格拉伸，
	/// 四角保持固定像素、边和中心拉伸，所以宽高变化时圆角不会变形。
	/// 另外提供「只圆上面两个角、下面两个角是直角」的版本，用来画贴着内容区的页签。
	/// </summary>
	public static class RoundedRectUtility
	{
		private const int TextureSize = 24;

		private const int Corner = 8;

		private static Texture2D allCorners;

		private static Texture2D topCorners;

		/// <summary>四角都圆的实心矩形。</summary>
		public static void Draw(Rect rect, Color color)
		{
			DrawTexture(EnsureTexture(ref allCorners, roundBottom: true), rect, color);
		}

		/// <summary>四角都圆、带 1px 边框的矩形。</summary>
		public static void DrawWithBorder(Rect rect, Color fill, Color border, float borderWidth = 1f)
		{
			Draw(rect, border);
			Draw(Inset(rect, borderWidth), fill);
		}

		/// <summary>只圆上面两个角、下面是直角的实心矩形（页签贴住内容区用）。</summary>
		public static void DrawTop(Rect rect, Color color)
		{
			DrawTexture(EnsureTexture(ref topCorners, roundBottom: false), rect, color);
		}

		/// <summary>只圆上面两个角、带边框的矩形；底边不画边框，看起来和下方内容连在一起。</summary>
		public static void DrawTopWithBorder(Rect rect, Color fill, Color border, float borderWidth = 1f)
		{
			DrawTop(rect, border);
			// 内侧只在上、左、右各收 1px，底边保持对齐，避免出现一条底边框。
			Rect inner = new Rect(
				rect.x + borderWidth,
				rect.y + borderWidth,
				rect.width - borderWidth * 2f,
				rect.height - borderWidth);
			DrawTop(inner, fill);
		}

		private static Rect Inset(Rect rect, float amount)
		{
			return new Rect(
				rect.x + amount,
				rect.y + amount,
				rect.width - amount * 2f,
				rect.height - amount * 2f);
		}

		private static Texture2D EnsureTexture(ref Texture2D cache, bool roundBottom)
		{
			if (cache != null)
			{
				return cache;
			}

			Texture2D created = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};

			Color32[] pixels = new Color32[TextureSize * TextureSize];
			float center = Corner - 0.5f;
			for (int y = 0; y < TextureSize; y++)
			{
				for (int x = 0; x < TextureSize; x++)
				{
					float dx = DistanceToEdge(x, center);
					float dy = DistanceToEdge(y, center);
					if (!roundBottom && y < Corner)
					{
						// 下面两个角不圆：贴到上边缘的距离按 0 处理。
						dy = 0f;
					}

					float distance = Mathf.Sqrt(dx * dx + dy * dy);
					float alpha = Mathf.Clamp01(center + 0.5f - distance);
					pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
				}
			}

			created.SetPixels32(pixels);
			created.Apply(false, true);
			cache = created;
			return cache;
		}

		private static float DistanceToEdge(int coordinate, float center)
		{
			if (coordinate < Corner)
			{
				return center - coordinate;
			}

			if (coordinate >= TextureSize - Corner)
			{
				return coordinate - (TextureSize - Corner - 1) - center + 1f;
			}

			return 0f;
		}

		private static void DrawTexture(Texture2D texture, Rect rect, Color color)
		{
			if (texture == null || rect.width < 2f || rect.height < 2f)
			{
				return;
			}

			Color previous = GUI.color;
			GUI.color = color;

			float size = TextureSize;
			float corner = Mathf.Min(Corner, rect.width / 2f, rect.height / 2f);

			// 四角
			DrawPart(texture, new Rect(rect.x, rect.y, corner, corner), 0f, 0f, Corner, Corner);
			DrawPart(texture, new Rect(rect.xMax - corner, rect.y, corner, corner), size - Corner, 0f, Corner, Corner);
			DrawPart(texture, new Rect(rect.x, rect.yMax - corner, corner, corner), 0f, size - Corner, Corner, Corner);
			DrawPart(
				texture,
				new Rect(rect.xMax - corner, rect.yMax - corner, corner, corner),
				size - Corner, size - Corner, Corner, Corner);

			// 四边
			DrawPart(
				texture,
				new Rect(rect.x + corner, rect.y, rect.width - corner * 2f, corner),
				Corner, 0f, size - Corner * 2f, Corner);
			DrawPart(
				texture,
				new Rect(rect.x + corner, rect.yMax - corner, rect.width - corner * 2f, corner),
				Corner, size - Corner, size - Corner * 2f, Corner);
			DrawPart(
				texture,
				new Rect(rect.x, rect.y + corner, corner, rect.height - corner * 2f),
				0f, Corner, Corner, size - Corner * 2f);
			DrawPart(
				texture,
				new Rect(rect.xMax - corner, rect.y + corner, corner, rect.height - corner * 2f),
				size - Corner, Corner, Corner, size - Corner * 2f);

			// 中心
			DrawPart(
				texture,
				new Rect(rect.x + corner, rect.y + corner, rect.width - corner * 2f, rect.height - corner * 2f),
				Corner, Corner, size - Corner * 2f, size - Corner * 2f);

			GUI.color = previous;
		}

		private static void DrawPart(Texture2D texture, Rect dest, float ux, float uy, float uw, float uh)
		{
			const float size = TextureSize;
			GUI.DrawTextureWithTexCoords(
				dest,
				texture,
				new Rect(ux / size, uy / size, uw / size, uh / size));
		}
	}
}
