using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Bubbles.Patches
{
	/// <summary>
	/// 游戏内「显示气泡」图标：挂在原版播放设置那一排，Shift+点击直接打开本模块的设置页。
	/// </summary>
	/// <remarks>
	/// 状态存在 <see cref="BubblesSettings.activated"/> 而不是静态字段，所以重开游戏后
	/// 仍然保持玩家上次的选择；关闭时立即清空已画出的气泡。
	/// </remarks>
	[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
	internal static class Patch_PlaySettings_DoPlaySettingsGlobalControls
	{
		private static void Postfix(WidgetRow row, bool worldView)
		{
			if (worldView || row == null || !BubblesFeature.Available)
			{
				return;
			}

			BubblesSettings settings = BubblesSettingsUtility.Current;
			if (settings == null)
			{
				return;
			}

			bool activated = settings.activated;
			row.ToggleableIcon(
				ref activated,
				BubblesTextures.Icon,
				"ColonyKit.Bubbles.Toggle".Loc(),
				SoundDefOf.Mouseover_ButtonToggle);

			if (activated == settings.activated)
			{
				return;
			}

			// 和原 Mod 一致：Shift+点击不是切换，而是打开设置页。
			if (Event.current != null && Event.current.shift)
			{
				OpenSettings();
				return;
			}

			settings.activated = activated;
			if (!activated)
			{
				Bubbler.Clear();
			}
		}

		private static void OpenSettings()
		{
			if (ColonyKitMod.Instance == null || Find.WindowStack == null)
			{
				return;
			}

			if (Find.WindowStack.IsOpen(typeof(Dialog_ModSettings)))
			{
				return;
			}

			ColonyKitMod.Instance.SelectSettingsPage("bubbles");
			Find.WindowStack.Add(new Dialog_ModSettings(ColonyKitMod.Instance));
		}
	}
}
