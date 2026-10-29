using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 固定的身体贴图入口。
	///
	/// 外观不再从设置读取：路径固定为 Things/Pawn/MkV/MkV，
	/// drawSize 固定 2.2，宽高比例固定 1。这样旧存档里保存的
	/// 自定义路径/尺寸也不会继续影响新版本。
	/// </summary>
	internal static class MkVSkinUtility
	{
		private const string TexturePath = "Things/Pawn/MkV/MkV";
		private const float DrawSize = 2.2f;

		public static Graphic GraphicFor(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn))
			{
				return null;
			}

			// 先确认四向贴图存在；缺失时返回 null，让自定义 RenderNode 回退
			// 到原版 PawnKindLifeStage.bodyGraphicData，避免整只 Mk-V 透明。
			if (ContentFinder<Texture2D>.Get(TexturePath + "_south", false) == null)
			{
				Log.Warning($"[ColonyKit][MkVskin] missing texture path: {TexturePath}");
				return null;
			}

			return GraphicDatabase.Get<Graphic_Multi>(
				TexturePath,
				ShaderDatabase.Cutout,
				new Vector2(DrawSize, DrawSize),
				Color.white);
		}
	}
}
