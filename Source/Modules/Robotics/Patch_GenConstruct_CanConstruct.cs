using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人没有「工作」页：<c>workSettings</c> 可能为 null，或者工作类型默认全是关的。
	/// 原版 <c>GenConstruct.CanConstruct</c> 会先查 <c>pawn.workSettings.WorkIsActive(Construction)</c>，
	/// 不处理就是空引用（之前为了让建造跑起来，我们在 WorkGiver 里传了 forced=true，结果连
	/// 「材料/框架是否被预订」也一起跳过了，两个机器人抢同一摞木头时会刷
	/// 「Could not reserve Thing WoodLog… TryMakePreToilReservations() returned false」）。
	/// 这里只替机器人跳过「有没有分配这个工作」这一项，后面的预约、路径、技能检查全部保留，
	/// WorkGiver 就可以正常传 forced=false 了。
	/// </summary>
	[HarmonyPatch(
		typeof(GenConstruct),
		nameof(GenConstruct.CanConstruct),
		new Type[]
		{
			typeof(Thing), typeof(Pawn), typeof(WorkTypeDef), typeof(bool), typeof(JobDef)
		})]
	internal static class Patch_GenConstruct_CanConstruct
	{
		[HarmonyPrefix]
		private static bool Prefix(
			Thing t,
			Pawn pawn,
			WorkTypeDef workType,
			bool forced,
			JobDef jobForReservation,
			ref bool __result)
		{
			if (pawn == null || !NannyBotUtility.IsNannyBot(pawn))
			{
				return true;
			}
			__result = GenConstruct.CanConstruct(
				t, pawn, workType == WorkTypeDefOf.Construction, forced, jobForReservation);
			return false;
		}
	}
}
