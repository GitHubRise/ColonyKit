using System.Collections.Generic;
using System.Reflection.Emit;
using ColonyKit.Modules.Localization;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Localization
{
	/// <summary>把开发者菜单节点标签替换为翻译表里的中文。</summary>
	[HarmonyPatch(typeof(LudeonTK.DebugActionNode), "LabelNow", MethodType.Getter)]
	internal static class Patch_DebugActionNode_LabelNow
	{
		public static void Postfix(ref string __result)
		{
			__result = DevLabelTranslations.Translate(__result);
		}
	}

	/// <summary>
	/// Debug 窗口按钮的 tooltip 是硬编码字符串；在 DrawButtons 里每个字符串常量后
	/// 插入一次 TranslateTooltip，只翻译已知的提示。
	/// </summary>
	[HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
	internal static class Patch_DebugWindowsOpener_DrawButtons
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var translate = AccessTools.Method(
				typeof(DevLabelTranslations),
				nameof(DevLabelTranslations.TranslateTooltip));

			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Ldstr
					&& instruction.operand is string text
					&& DevLabelTranslations.HasTooltip(text))
				{
					yield return new CodeInstruction(OpCodes.Call, translate);
				}
			}
		}
	}
}
