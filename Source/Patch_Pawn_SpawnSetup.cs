using HarmonyLib;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 最小 Harmony 补丁示例：在 <c>Pawn.SpawnSetup</c> 之后记一笔，不替换原方法逻辑。
	/// </summary>
	/// <remarks>
	/// 选 <c>SpawnSetup</c> 的原因：
	///   - 它是小人进入地图的统一入口（生成、空投、读档恢复都会走）；
	///   - 签名多年稳定（<c>(Map, bool)</c>），补丁不易因版本升级失效；
	///   - 只做只读统计，失败也不会破坏游戏流程。
	///
	/// 只挂 Postfix：原方法照常执行，我们只在它结束后追加行为。
	/// 需要阻止原方法时才用 Prefix 返回 false——那会接管原逻辑，风险高得多。
	/// </remarks>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
	internal static class Patch_Pawn_SpawnSetup
	{
		/// <param name="__instance">Harmony 按名字注入的原方法实例。</param>
		/// <param name="respawningAfterLoad">
		/// 读档恢复时为 true。此时计数已经在存档里，跳过，避免读一次档就把总数翻倍。
		/// </param>
		[HarmonyPostfix]
		private static void Postfix(Pawn __instance, bool respawningAfterLoad)
		{
			// 读档不属于「新生成」，直接忽略。
			if (respawningAfterLoad)
			{
				return;
			}

			// 设置可能尚未就绪（极早期生成）；关闭统计时也直接退出。
			GeneralSettings general = ModTemplateMod.Settings?.general;
			if (general == null || !general.trackSpawns)
			{
				return;
			}

			// SpawnSetup 也会被尸体、动物、敌人、囚犯走到，这里只统计玩家殖民者。
			if (__instance == null || __instance.Dead || !__instance.IsColonist)
			{
				return;
			}

			// 读档 / 生成世界阶段 Current.Game 可能为 null；
			// GameComponent 也可能还没被 FillComponents 创建。
			Current.Game?.GetComponent<DemoGameComponent>()?.NotifyColonistSpawned(__instance);
		}
	}
}
