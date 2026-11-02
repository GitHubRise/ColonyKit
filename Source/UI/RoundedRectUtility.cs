using UnityEngine;

namespace ColonyKit
{
	/// <summary>
	/// 运行时生成的圆角矩形贴图 + 九宫格绘制：给子页签这类小控件做圆角背景。
	/// IMGUI 没有圆角 API，这里用一张 24×24 的带 alpha 贴图按九宫格拉伸，
	/// 四角保持固定像素、边和中心拉伸，所以宽高变化时圆角不会变形。
	/// </summary>
	public static class RoundedRectUtility
	{
		private const int TextureSize = 24;

		private const int Corner = 8;

		private static Texture2D texture;

		private static void EnsureTexture()
		{
			if (texture != null)
			{
				return;
			}

			texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};

			Color32[] pixels = new Color32[TextureSize * TextureSize];
			float center = Corner - 0.5f;
			for (int y = 0; y < TextureSize; y++)
			{
				for (int x = 0; x < TextureSize; x++)
				{
					float dx = 0f;
					if (x < Corner)
					{
						dx = center - x;
					}
					else if (x >= TextureSize - Corner)
					{
						dx = x - (TextureSize - Corner - 1) - center + 1f;
					}

					float dy = 0f;
					if (y < Corner)
					{
						dy = center - y;
					}
					else if (y >= TextureSize - Corner)
					{
						dy = y - (TextureSize - Corner - 1) - center + 1f;
					}

					float distance = Mathf.Sqrt(dx * dx + dy * dy);
					float alpha = Mathf.Clamp01(center + 0.5f - distance);
					pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
				}
			}

			texture.SetPixels32(pixels);
			texture.Apply(false, true);
		}

		/// <summary>画一个圆角实心矩形（颜色由 GUI.color 之外单独传入，内部会恢复）。</summary>
		public static void Draw(Rect rect, Color color)
		{
			if (rect.width < 2f || rect.height < 2f)
			{
				return;
			}

			EnsureTexture();

			Color previous = GUI.color;
			GUI.color = color;

			float size = TextureSize;
			float corner = Mathf.Min(Corner, rect.width / 2f, rect.height / 2f);

			// 四角
			DrawPart(new Rect(rect.x, rect.y, corner, corner), 0f, 0f, Corner, Corner);
			DrawPart(new Rect(rect.xMax - corner, rect.y, corner, corner), size - Corner, 0f, Corner, Corner);
			DrawPart(new Rect(rect.x, rect.yMax - corner, corner, corner), 0f, size - Corner, Corner, Corner);
			DrawPart(
				new Rect(rect.xMax - corner, rect.yMax - corner, corner, corner),
				size - Corner, size - Corner, Corner, Corner);

			// 四边
			DrawPart(
				new Rect(rect.x + corner, rect.y, rect.width - corner * 2f, corner),
				Corner, 0f, size - Corner * 2f, Corner);
			DrawPart(
				new Rect(rect.x + corner, rect.yMax - corner, rect.width - corner * 2f, corner),
				Corner, size - Corner, size - Corner * 2f, Corner);
			DrawPart(
				new Rect(rect.x, rect.y + corner, corner, rect.height - corner * 2f),
				0f, Corner, Corner, size - Corner * 2f);
			DrawPart(
				new Rect(rect.xMax - corner, rect.y + corner, corner, rect.height - corner * 2f),
				size - Corner, Corner, Corner, size - Corner * 2f);

			// 中心
			DrawPart(
				new Rect(rect.x + corner, rect.y + corner, rect.width - corner * 2f, rect.height - corner * 2f),
				Corner, Corner, size - Corner * 2f, size - Corner * 2f);

			GUI.color = previous;
		}

		/// <summary>连带边框的圆角矩形：外层边框色铺满，内层再盖一层填充色。</summary>
		public static void DrawWithBorder(Rect rect, Color fill, Color border, float borderWidth = 1f)
		{
			Draw(rect, border);
			Draw(new Rect(rect.x + borderWidth, rect.y + borderWidth, rect.width - borderWidth * 2f, rect.height - borderWidth * 2f), fill);
		}

		private static void DrawPart(Rect dest, float ux, float uy, float uw, float uh)
		{
			const float size = TextureSize;
			GUI.DrawTextureWithTexCoords(
				dest,
				texture,
				new Rect(ux / size, uy / size, uw / size, uh / size));
		}
	}
}
