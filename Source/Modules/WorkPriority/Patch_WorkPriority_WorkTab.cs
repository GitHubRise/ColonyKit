using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.WorkPriority
{
	/// <summary>
	/// 把工作页里「点一下就绕回 4 / 超过 4 就归零」的那两个常量换成运行时上限。
	/// </summary>
	/// <remarks>
	/// 原版 <c>WidgetsWork.DrawWorkBoxFor</c> 与 <c>PawnColumnWorker_WorkPriority.HeaderClicked</c>
	/// 里各出现一次「归 4」和一次「&gt; 4 归 0」。同一个方法里还有 <c>TextAnchor.MiddleCenter</c>（值 4）
	/// 与 <c>KnowledgeAmount.SmallInteraction</c>（值 4），所以不能见 4 就换：
	/// 只认「条件跳转 + 存局部变量」和「读局部变量 + 条件跳转」这两种夹法。
	/// 换进去的是 <see cref="WorkPriorityModule.MaxPriority"/> 的调用，模块关闭时自动退回 4。
	/// </remarks>
	internal static class WorkPriorityTranspiler
	{
		private static readonly MethodInfo MaxPriorityGetter =
			AccessTools.PropertyGetter(typeof(WorkPriorityModule), nameof(WorkPriorityModule.MaxPriority));

		public static IEnumerable<CodeInstruction> RaiseWrapping(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			int rewritten = 0;
			for (int i = 1; i < list.Count - 1; i++)
			{
				if (!IsFour(list[i]))
				{
					continue;
				}

				bool wrapFromOff = IsCondBranch(list[i - 1]) && IsStoreLocal(list[i + 1]);
				bool wrapToOff = IsLoadLocal(list[i - 1]) && IsCondBranch(list[i + 1]);
				if (!wrapFromOff && !wrapToOff)
				{
					continue;
				}

				// 原地改 opcode，保留 CodeInstruction 上的标签。
				list[i].opcode = OpCodes.Call;
				list[i].operand = MaxPriorityGetter;
				rewritten++;
			}

			ModLog.Info("WorkPriority", $"rewrote {rewritten} priority wrap constant(s)");
			return list;
		}

		private static bool IsFour(CodeInstruction instruction)
		{
			if (instruction.opcode == OpCodes.Ldc_I4_4)
			{
				return true;
			}
			if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte shortValue)
			{
				return shortValue == 4;
			}
			if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int intValue)
			{
				return intValue == 4;
			}
			return false;
		}

		private static bool IsCondBranch(CodeInstruction instruction)
		{
			return instruction.opcode.FlowControl == FlowControl.Cond_Branch;
		}

		private static bool IsStoreLocal(CodeInstruction instruction)
		{
			return instruction.opcode.Name.StartsWith("stloc");
		}

		private static bool IsLoadLocal(CodeInstruction instruction)
		{
			return instruction.opcode.Name.StartsWith("ldloc");
		}
	}

	/// <summary>工作页单元格点击循环：左上右下都按运行时上限绕圈。</summary>
	[HarmonyPatch(typeof(WidgetsWork), nameof(WidgetsWork.DrawWorkBoxFor))]
	internal static class Patch_WorkPriority_DrawWorkBoxFor
	{
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return WorkPriorityTranspiler.RaiseWrapping(instructions);
		}
	}

	/// <summary>工作页列头 Shift 点击整列升降，同样按运行时上限绕圈。</summary>
	[HarmonyPatch(typeof(PawnColumnWorker_WorkPriority), "HeaderClicked")]
	internal static class Patch_WorkPriority_HeaderClicked
	{
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return WorkPriorityTranspiler.RaiseWrapping(instructions);
		}
	}

	/// <summary>5–9 也给出颜色：沿用原版「数字越大越灰」的观感。</summary>
	[HarmonyPatch(typeof(WidgetsWork), nameof(WidgetsWork.ColorOfPriority))]
	internal static class Patch_WorkPriority_ColorOfPriority
	{
		[HarmonyPostfix]
		private static void Postfix(int prio, ref Color __result)
		{
			if (!WorkPriorityModule.IsActive || prio <= WorkPriorityModule.VanillaMaxPriority)
			{
				return;
			}

			__result = ColorFor(prio);
		}

		private static Color ColorFor(int priority)
		{
			switch (priority)
			{
				case 5:
					return new Color(0.66f, 0.66f, 0.66f);
				case 6:
					return new Color(0.58f, 0.58f, 0.58f);
				case 7:
					return new Color(0.50f, 0.50f, 0.50f);
				case 8:
					return new Color(0.43f, 0.43f, 0.43f);
				case 9:
					return new Color(0.36f, 0.36f, 0.36f);
				default:
					return Color.grey;
			}
		}
	}
}
