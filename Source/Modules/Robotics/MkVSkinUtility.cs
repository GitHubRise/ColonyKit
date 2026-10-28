using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Builds the Mk-V body graphic from the configurable path and size settings.</summary>
	internal static class MkVSkinUtility
	{
		public static Graphic GraphicFor(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn))
			{
				return null;
			}

			RoboticsSettings settings = NannyBotUtility.Settings;
			string path = settings.mkvTexturePath;
			if (path.NullOrEmpty())
			{
				path = RoboticsSettings.DefaultMkvTexturePath;
			}
			path = path.Trim();
			if (path.StartsWith("Textures/"))
			{
				path = path.Substring("Textures/".Length);
			}
			if (path.EndsWith(".png"))
			{
				path = path.Substring(0, path.Length - ".png".Length);
			}
			if (ContentFinder<Texture2D>.Get(path + "_south", false) == null)
			{
				return null;
			}

			float drawSize = Mathf.Clamp(settings.mkvDrawSize,
				RoboticsSettings.MinMkvDrawSize, RoboticsSettings.MaxMkvDrawSize);
			float width = Mathf.Clamp(settings.mkvWidthScale,
				RoboticsSettings.MinMkvSkinScale, RoboticsSettings.MaxMkvSkinScale);
			float height = Mathf.Clamp(settings.mkvHeightScale,
				RoboticsSettings.MinMkvSkinScale, RoboticsSettings.MaxMkvSkinScale);
			return GraphicDatabase.Get<Graphic_Multi>(
				path,
				ShaderDatabase.Cutout,
				new Vector2(drawSize * width, drawSize * height),
				Color.white);
		}
	}

	[HarmonyLib.HarmonyPatch(typeof(PawnRenderNode_AnimalPart), nameof(PawnRenderNode_AnimalPart.GraphicFor))]
	internal static class Patch_MkV_Skin
	{
		private static void Postfix(Pawn pawn, ref Graphic __result)
		{
			Graphic custom = MkVSkinUtility.GraphicFor(pawn);
			if (custom != null)
			{
				__result = custom;
			}
		}
	}
}
