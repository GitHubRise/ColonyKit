using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 「通知」页的数据：总开关与节流冷却。只放字段和 Scribe，不画 UI。
	/// </summary>
	public class NotificationSettings : IExposable
	{
		/// <summary>总开关；关闭后 <see cref="NotificationService"/> 不再发消息/信件。</summary>
		public bool enabled = true;

		/// <summary>节流冷却，单位 tick。默认 600（约 10 秒）。</summary>
		public int cooldownTicks = 600;

		public void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "enabled", true);
			Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 600);
		}

		public void Reset()
		{
			enabled = true;
			cooldownTicks = 600;
		}

		/// <summary>把冷却夹到 60–6000 tick。</summary>
		public bool Validate()
		{
			if (cooldownTicks < 60)
			{
				cooldownTicks = 60;
				return true;
			}
			if (cooldownTicks > 6000)
			{
				cooldownTicks = 6000;
				return true;
			}
			return false;
		}
	}
}
