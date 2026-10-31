using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 电弧刃的面板数值由设置页实时同步到 <c>ThingDef.tools</c>。Tool 的 power / cooldownTime /
	/// armorPenetration 都是普通字段，近战结算（VerbProperties.AdjustedMeleeDamageAmount 等）
	/// 和武器信息卡的 DPS / 伤害 / 破甲统计都直接读它，所以改这里既能生效又不用另打补丁。
	/// </summary>
	internal static class MkVArcBlade
	{
		public static void ApplySettings()
		{
			ThingDef def = NannyBotDefOf.CK_ArcBlade;
			if (def?.tools == null || def.tools.Count == 0)
			{
				return;
			}
			RoboticsSettings settings = NannyBotUtility.Settings;
			float damage = Mathf.Clamp(settings.mkvArcBladeDamage,
				RoboticsSettings.MinArcBladeDamage, RoboticsSettings.MaxArcBladeDamage);
			float cooldown = Mathf.Clamp(settings.mkvArcBladeCooldown,
				RoboticsSettings.MinArcBladeCooldown, RoboticsSettings.MaxArcBladeCooldown);
			float armorPenetration = Mathf.Clamp(settings.mkvArcBladeArmorPenetration,
				RoboticsSettings.MinArcBladeArmorPenetration,
				RoboticsSettings.MaxArcBladeArmorPenetration);

			for (int i = 0; i < def.tools.Count; i++)
			{
				Tool tool = def.tools[i];
				if (tool == null)
				{
					continue;
				}
				if (Mathf.Abs(tool.power - damage) > 0.001f)
				{
					tool.power = damage;
				}
				if (Mathf.Abs(tool.cooldownTime - cooldown) > 0.001f)
				{
					tool.cooldownTime = cooldown;
				}
				if (Mathf.Abs(tool.armorPenetration - armorPenetration) > 0.001f)
				{
					tool.armorPenetration = armorPenetration;
				}
			}
		}
	}
}
