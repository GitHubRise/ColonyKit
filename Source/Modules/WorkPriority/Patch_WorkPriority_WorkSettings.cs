using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.WorkPriority
{
	/// <summary>
	/// 放开 <see cref="Pawn_WorkSettings.SetPriority"/> 的上限：5–9 直接写进优先级表，
	/// 同时跳过原版对 &gt;4 打的「Trying to set work to invalid priority」日志。
	/// </summary>
	/// <remarks>
	/// 0–4 仍走原版逻辑（含禁用工作检查、工作列表失效标记），行为不变；
	/// 5–9 自己写字段，所以要把原版那两件事补齐。
	/// </remarks>
	[HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
	internal static class Patch_WorkPriority_SetPriority
	{
		[HarmonyPrefix]
		private static bool Prefix(
			Pawn_WorkSettings __instance,
			Pawn ___pawn,
			DefMap<WorkTypeDef, int> ___priorities,
			ref bool ___workGiversDirty,
			WorkTypeDef w,
			int priority)
		{
			if (!WorkPriorityModule.IsActive || priority <= WorkPriorityModule.VanillaMaxPriority)
			{
				return true;
			}

			// 还没初始化时交给原版：它会先补初始化，走到这里也只可能是 0–4。
			if (___priorities == null || w == null)
			{
				return true;
			}

			if (___pawn != null && ___pawn.WorkTypeIsDisabled(w))
			{
				Log.Error("Tried to change priority on disabled worktype " + w + " for pawn " + ___pawn);
				return false;
			}

			int clamped = priority > WorkPriorityModule.MaxPriorityValue
				? WorkPriorityModule.MaxPriorityValue
				: priority;
			___priorities[w] = clamped;
			___workGiversDirty = true;
			return false;
		}
	}

	/// <summary>
	/// 小人第一次拿到工作设置时套用默认优先级：灭火 1、就医 2、其余 5。
	/// </summary>
	[HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.EnableAndInitialize))]
	internal static class Patch_WorkPriority_EnableAndInitialize
	{
		[HarmonyPostfix]
		private static void Postfix(Pawn_WorkSettings __instance, Pawn ___pawn)
		{
			if (!WorkPriorityModule.IsActive)
			{
				return;
			}

			WorkPriorityDefaults.Apply(___pawn, __instance);
		}
	}
}
