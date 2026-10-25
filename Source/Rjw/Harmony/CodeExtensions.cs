#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace rjw
{
	public static class CodeExtensions
	{
		/// <summary>
		/// <para>在某个特定位置拆分指令列表，该位置由
		/// 谓词函数确定。第一条匹配谓词的指令会
		/// 包含在 `before` 列表中，其余所有指令则
		/// 位于 `after` 列表中。</para>
		/// <para>如果没有指令匹配谓词，则所有指令都会位于
		/// `before` 列表，而 `after` 列表为空。</para>
		/// <para>这适用于把指令拆成更易处理的
		/// 片段，以便更好地定位补丁的目标。</para>
		/// </summary>
		/// <param name="instructions">该指令列表。</param>
		/// <param name="predicate">用于确定拆分点的谓词。</param>
		/// <returns>一个元组，第一项是到匹配谓词的指令为止（含该指令）的
		/// 指令，第二项是其后
		/// 的所有内容。</returns>
		public static (List<CodeInstruction> before, List<CodeInstruction> after) SplitAfter(
			this List<CodeInstruction> instructions,
			Predicate<CodeInstruction> predicate)
		{
			if (instructions.FindIndex(predicate) is var idx and not -1)
			{
				var before = instructions.Take(idx + 1).ToList();
				var after = instructions.Skip(before.Count).ToList();
				return (before, after);
			}

			return (instructions, new());
		}

		/// <summary>
		/// <para>检查该指令列表中接下来的指令，确保
		/// 它们都符合某种预期，每条指令对应一个谓词。如果
		/// 所有谓词都匹配，则生成两个新列表，一个包含匹配的
		/// 指令，另一个包含移除这些匹配指令后的内容。</para>
		/// <para>如果有任一谓词不匹配，`cut` 列表将为空，
		/// 而 `remainder` 列表将与 `instructions` 是同一实例。</para>
		/// <para>用它来验证代码确实是你预期的代码，
		/// 并把它与其他指令隔离，以便安全地调整。</para>
		/// </summary>
		/// <param name="instructions">该指令列表。</param>
		/// <param name="predicates">用于测试每条指令的一个或多个谓词。</param>
		/// <returns>一个元组，第一项是切出的指令列表，第二项
		/// 是切出后剩余的指令。</returns>
		public static (List<CodeInstruction> cut, List<CodeInstruction> remainder) CutMatchingSequence(
			this List<CodeInstruction> instructions,
			params Predicate<CodeInstruction>[] predicates)
		{
			if (predicates.Length == 0)
				return (new(), instructions);
			if (instructions.Count < predicates.Length)
				return (new(), instructions);
			if (instructions.Zip(predicates, (il, pred) => pred(il)).Contains(false))
				return (new(), instructions);

			var cut = instructions.Take(predicates.Length).ToList();
			var remainder = instructions.Skip(predicates.Length).ToList();
			return (cut, remainder);
		}

		/// <summary>
		/// <para>判断该指令是否为某种与局部变量表中
		/// 给定索引匹配的 `stloc`。</para>
		/// <para>为什么这不在 Harmony 的 API 中？</para>
		/// </summary>
		/// <param name="il">该指令。</param>
		/// <param name="index">要检查的局部变量表索引。</param>
		/// <returns>该指令是否为以给定索引为目标的 `stloc`。</returns>
		public static bool IsStlocOf(this CodeInstruction il, int index) => index switch
		{
			0 when il.opcode == OpCodes.Stloc_0 => true,
			1 when il.opcode == OpCodes.Stloc_1 => true,
			2 when il.opcode == OpCodes.Stloc_2 => true,
			3 when il.opcode == OpCodes.Stloc_3 => true,
			// 最常见的情况……
			_ when il.IsStloc() && il.operand is LocalBuilder lb => lb.LocalIndex == index,
			// ……但这些在 `stloc_s` 和 `stloc` 下技术上也是可能的。
			_ when il.IsStloc() && il.operand is byte bIndex => bIndex == Convert.ToByte(index),
			_ when il.IsStloc() && il.operand is ushort uIndex => uIndex == Convert.ToUInt16(index),
			_ => false
		};

		/// <summary>
		/// 将该指令的标签复制到其替换指令。如果替换
		/// 指令本身也有标签，则会确保丢弃重复的标签。
		/// </summary>
		/// <param name="il">该指令。</param>
		/// <param name="newIl">将要替换它的指令。</param>
		/// <returns>替换后的指令。</returns>
		public static CodeInstruction Replace(this CodeInstruction il, CodeInstruction newIl)
		{
			var copyLabels = newIl.labels.ToList();
			newIl.labels.Clear();
			newIl.labels.AddRange(copyLabels.Concat(il.labels).Distinct());
			return newIl;
		}
	}
}
