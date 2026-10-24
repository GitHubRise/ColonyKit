using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 通知统一出口：消息、信件、带节流的消息。
	/// </summary>
	/// <remarks>
	/// - 所有方法受「通知」页总开关控制；
	/// - 节流按 key 记录下一次允许的 tick，避免同一事件刷屏；
	/// - 只在游戏内调用（依赖 <c>Find.TickManager</c> / <c>Find.LetterStack</c>）。
	/// </remarks>
	public static class NotificationService
	{
		private static readonly Dictionary<string, int> nextAllowedTick = new Dictionary<string, int>();

		public static bool Enabled =>
			ModuleRegistry.IsEnabled("notifications")
			&& (ModTemplateMod.Settings?.notifications?.enabled ?? true);

		/// <summary>当前节流表里有多少个 key（诊断用）。</summary>
		public static int ThrottleCount => nextAllowedTick.Count;

		/// <summary>发一条左上角消息。type 为空时用 NeutralEvent。</summary>
		public static void Message(string text, MessageTypeDef type = null, bool historical = false)
		{
			if (!Enabled || text.NullOrEmpty())
			{
				return;
			}

			Messages.Message(text, type ?? MessageTypeDefOf.NeutralEvent, historical);
		}

		/// <summary>发一封信。def 为空时用 NeutralEvent。</summary>
		public static void Letter(string label, string text, LetterDef def = null, LookTargets targets = null)
		{
			if (!Enabled || label.NullOrEmpty() || text.NullOrEmpty())
			{
				return;
			}

			Find.LetterStack?.ReceiveLetter(label, text, def ?? LetterDefOf.NeutralEvent, targets);
		}

		/// <summary>
		/// 节流消息：同一个 key 在冷却时间内只发一次。cooldownTicks &lt; 0 时用设置里的值。
		/// </summary>
		public static void ThrottledMessage(
			string key,
			string text,
			MessageTypeDef type = null,
			int cooldownTicks = -1)
		{
			if (!Enabled || text.NullOrEmpty())
			{
				return;
			}

			int now = Current.Game != null ? Find.TickManager?.TicksGame ?? 0 : 0;
			int cooldown = cooldownTicks >= 0
				? cooldownTicks
				: ModTemplateMod.Settings?.notifications?.cooldownTicks ?? 600;

			string throttleKey = key.NullOrEmpty() ? text : key;
			if (nextAllowedTick.TryGetValue(throttleKey, out int next) && now < next)
			{
				return;
			}

			nextAllowedTick[throttleKey] = now + cooldown;
			Message(text, type);
		}

		/// <summary>清掉某个 key 的节流记录；key 为空时清全部。</summary>
		public static void ResetThrottle(string key = null)
		{
			if (key.NullOrEmpty())
			{
				nextAllowedTick.Clear();
			}
			else
			{
				nextAllowedTick.Remove(key);
			}
		}
	}
}
