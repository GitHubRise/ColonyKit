using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Builds the Mk-V body graphic from the configurable path and size settings.</summary>
	internal static class MkVSkinUtility
	{
		private static string lastSkinKey;

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
				Log.Warning($"[ColonyKit][MkVskin] texture path not found: {path}; falling back to the def graphic.");
				return null;
			}

			float drawSize = Mathf.Clamp(settings.mkvDrawSize,
				RoboticsSettings.MinMkvDrawSize, RoboticsSettings.MaxMkvDrawSize);
			float width = Mathf.Clamp(settings.mkvWidthScale,
				RoboticsSettings.MinMkvSkinScale, RoboticsSettings.MaxMkvSkinScale);
			float height = Mathf.Clamp(settings.mkvHeightScale,
				RoboticsSettings.MinMkvSkinScale, RoboticsSettings.MaxMkvSkinScale);
			string key = path + "|" + drawSize.ToString("0.00") + "|"
				+ width.ToString("0.00") + "|" + height.ToString("0.00");
			if (key != lastSkinKey)
			{
				lastSkinKey = key;
				Log.Message($"[ColonyKit][MkVskin] settings path={path} draw={drawSize:0.00} "
					+ $"width={width:0.00} height={height:0.00}");
			}
			return GraphicDatabase.Get<Graphic_Multi>(
				path,
				ShaderDatabase.Cutout,
				new Vector2(drawSize * width, drawSize * height),
				Color.white);
		}
	}

}
