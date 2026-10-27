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
	/// 弹窗式列表（<c>Dialog_DebugOptionListLister</c>，例如「生成污渍…」列出全部污渍 defName）
	/// 直接把 option.label 画到按钮上，不经过 <see cref="LudeonTK.DebugActionNode.LabelNow"/>，
	/// 所以在这里再翻一道；筛选仍用原文，输入英文照样能过滤。
	/// </summary>
	[HarmonyPatch(typeof(LudeonTK.Dialog_DebugOptionLister), nameof(LudeonTK.Dialog_DebugOptionLister.ButtonDebug))]
	internal static class Patch_Dialog_DebugOptionLister_ButtonDebug
	{
		public static void Prefix(ref string label)
		{
			label = DevLabelTranslations.Translate(label);
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
