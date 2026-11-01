using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 敌情评估：这一群敌人里谁最该先打。
	/// 1) 带范围武器的（燃烧瓶、手雷、火箭筒、迫击炮、喷火器）永远排最前——一发就能带走半队人；
	/// 2) 其次是武器单发伤害高、命中高的；
	/// 3) 同样危险时，血少、护甲薄的先打（打得死才算解决），太远的适当降权。
	/// 分数只在同一张地图、同一时刻的敌人之间比较，绝对值没有意义。
	/// </summary>
	internal static class MkVThreat
	{
		/// <summary>范围武器的固定加分，足以压过普通枪械的伤害差。</summary>
		public const int AreaScore = 120;

		/// <summary>只解析一次的单兵武器档案。</summary>
		internal class Profile
		{
			public bool area;
			public bool ranged;
			public float damage;
			public float accuracy;
			public float health = 1f;
			public float armor;
			public string weaponLabel;
		}

		/// <summary>排好序的威胁条目：目标、档案、分数和给模型看的标签。</summary>
		internal class Threat
		{
			public Pawn pawn;
			public Profile profile;
			public int score;
			public float distance;
			public string label;
		}

		/// <summary>解析一个敌对身体里的武器：范围 / 单发伤害 / 命中，以及血量与护甲。</summary>
		public static Profile Describe(Pawn hostile)
		{
			Profile profile = new Profile();
			if (hostile == null)
			{
				return profile;
			}

			profile.health = Mathf.Clamp01(hostile.health?.summaryHealth?.SummaryHealthPercent ?? 1f);
			profile.armor = Mathf.Clamp01(hostile.GetStatValue(StatDefOf.ArmorRating_Sharp));

			ThingWithComps weapon = hostile.equipment?.Primary;
			ThingDef weaponDef = weapon?.def;
			List<VerbProperties> verbs = weaponDef?.Verbs;
			if (verbs == null || verbs.Count == 0)
			{
				verbs = hostile.def?.Verbs;
			}
			if (verbs != null)
			{
				for (int i = 0; i < verbs.Count; i++)
				{
					VerbProperties verb = verbs[i];
					if (verb == null || !verb.LaunchesProjectile)
					{
						continue;
					}
					ProjectileProperties projectile = verb.defaultProjectile?.projectile;
					if (projectile == null)
					{
						continue;
					}
					profile.ranged = true;
					if (IsAreaProjectile(projectile))
					{
						profile.area = true;
					}
					float damage = projectile.GetDamageAmount(weapon);
					if (damage > profile.damage)
					{
						profile.damage = damage;
					}
					float accuracy = (verb.accuracyTouch + verb.accuracyShort
						+ verb.accuracyMedium + verb.accuracyLong) / 4f;
					if (accuracy > profile.accuracy)
					{
						profile.accuracy = accuracy;
					}
				}
			}

			// 手雷、燃烧瓶：投掷物本身就是爆炸物，武器 Def 上带爆炸组件或 Grenade 标签。
			if (weaponDef != null && (HasExplosiveComp(weaponDef) || HasGrenadeTag(weaponDef)))
			{
				profile.area = true;
				profile.ranged = true;
			}

			// 近战武器、机械体自带工具：用「力量 / 冷却」估个每秒伤害，再打个八折（得贴脸才打得着）。
			if (!profile.ranged && hostile.def?.tools != null)
			{
				float bestDps = 0f;
				for (int i = 0; i < hostile.def.tools.Count; i++)
				{
					Tool tool = hostile.def.tools[i];
					if (tool == null || tool.cooldownTime <= 0.01f)
					{
						continue;
					}
					float dps = tool.power / tool.cooldownTime;
					if (dps > bestDps)
					{
						bestDps = dps;
					}
				}
				profile.damage = bestDps * 1.5f * 0.8f;
				profile.accuracy = 0.7f;
			}

			profile.weaponLabel = weaponDef?.label;
			if (string.IsNullOrEmpty(profile.weaponLabel) && !profile.ranged)
			{
				profile.weaponLabel = null;
			}
			return profile;
		}

		/// <summary>把地图上（或视野内）的敌人按威胁从大到小排好；同分时近的靠前。</summary>
		public static List<Threat> Rank(Pawn self, bool includeFogged)
		{
			List<Threat> list = new List<Threat>();
			Map map = self?.Map;
			if (map == null)
			{
				return list;
			}
			foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
			{
				if (other == null || other == self || other == null || other.Dead || other.Downed
					|| !other.Spawned || !other.HostileTo(Faction.OfPlayer))
				{
					continue;
				}
				if (!includeFogged && other.Position.Fogged(map))
				{
					continue;
				}
				Threat threat = new Threat
				{
					pawn = other,
					profile = Describe(other),
					distance = self.Position.DistanceTo(other.Position)
				};
				threat.score = Score(threat.profile, threat.distance);
				threat.label = Label(other, threat.profile);
				list.Add(threat);
			}
			list.Sort((left, right) =>
			{
				int byScore = right.score.CompareTo(left.score);
				return byScore != 0 ? byScore : left.distance.CompareTo(right.distance);
			});
			return list;
		}

		public static int Score(Profile profile, float distance)
		{
			if (profile == null)
			{
				return 0;
			}
			float score = 0f;
			if (profile.area)
			{
				score += AreaScore;
			}
			score += Mathf.Min(profile.damage, 120f) * 0.55f;
			score += profile.accuracy * 25f;
			score += (1f - profile.health) * 30f;
			score -= profile.armor * 25f;
			score -= distance * 0.2f;
			return Mathf.RoundToInt(score);
		}

		/// <summary>给候选动作和日志用的标签：「海盗（燃烧瓶·范围）」「海盗（突击步枪）」「海盗」。</summary>
		public static string Label(Pawn hostile, Profile profile)
		{
			if (hostile == null)
			{
				return "?";
			}
			string tag = null;
			if (profile != null && profile.area)
			{
				tag = "范围";
			}
			else if (profile != null && profile.damage >= 22f)
			{
				tag = "高伤";
			}
			string weapon = profile?.weaponLabel;
			string inner;
			if (string.IsNullOrEmpty(weapon))
			{
				inner = tag;
			}
			else if (tag == null)
			{
				inner = weapon;
			}
			else
			{
				inner = weapon + "·" + tag;
			}
			return string.IsNullOrEmpty(inner)
				? hostile.LabelShort
				: hostile.LabelShort + "（" + inner + "）";
		}

		/// <summary>给候选动作用的短标签：范围 / 高伤 / 普通。</summary>
		public static string Tag(Profile profile)
		{
			if (profile == null)
			{
				return null;
			}
			if (profile.area)
			{
				return "范围";
			}
			return profile.damage >= 22f ? "高伤" : null;
		}

		private static bool IsAreaProjectile(ProjectileProperties projectile)
		{
			if (projectile.explosionRadius > 0.1f)
			{
				return true;
			}
			string damage = projectile.damageDef?.defName;
			return damage == "Flame" || damage == "Bomb" || damage == "BombSuper"
				|| damage == "FireBomb" || damage == "Smoke";
		}

		private static bool HasExplosiveComp(ThingDef def)
		{
			if (def.comps == null)
			{
				return false;
			}
			for (int i = 0; i < def.comps.Count; i++)
			{
				if (def.comps[i] is CompProperties_Explosive explosive && explosive.explosiveRadius > 0f)
				{
					return true;
				}
			}
			return false;
		}

		private static bool HasGrenadeTag(ThingDef def)
		{
			if (def.weaponTags == null)
			{
				return false;
			}
			for (int i = 0; i < def.weaponTags.Count; i++)
			{
				string tag = def.weaponTags[i];
				if (!string.IsNullOrEmpty(tag)
					&& tag.IndexOf("grenade", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}
