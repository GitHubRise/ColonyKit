using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 在原生放置校验之后追加 <see cref="BuildControlService"/> 的规则。
	/// 只改 <c>AcceptanceReport</c>，不改变原版校验本身的逻辑。
	/// </summary>
	/// <remarks>
	/// 打在实际做校验的 <c>CanPlaceBlueprintAt_NewTemp</c> 上，外层
	/// <c>CanPlaceBlueprintAt</c> 会委托到它，所以两条调用路径都能覆盖。
	/// </remarks>
	[HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintAt_NewTemp))]
	internal static class Patch_GenConstruct_CanPlaceBlueprintAt
	{
		[HarmonyPostfix]
		private static void Postfix(
			BuildableDef entDef,
			IntVec3 center,
			Rot4 rot,
			Map map,
			ThingDef stuffDef,
			ref AcceptanceReport __result)
		{
			if (__result.Accepted)
			{
				__result = BuildControlService.ApplyRules(entDef, center, map, rot, stuffDef, __result);
			}
		}
	}
}
