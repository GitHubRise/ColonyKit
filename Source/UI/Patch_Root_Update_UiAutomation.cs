using HarmonyLib;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 每帧驱动 <see cref="UiAutomationService"/>。放在 Root.Update 上而不是
	/// GameComponentUpdate，是因为设置窗口会暂停游戏，GameComponent 可能不再更新。
	/// </summary>
	[HarmonyPatch(typeof(Root), nameof(Root.Update))]
	internal static class Patch_Root_Update_UiAutomation
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			UiAutomationService.Update();
		}
	}
}
