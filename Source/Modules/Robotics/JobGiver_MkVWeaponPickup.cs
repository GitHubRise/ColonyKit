using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 自动拾取更好的远程武器。原版 JobGiver_PickUpOpportunisticWeapon 只比较
	/// “远程/近战”大类，已经有电弧发射器时不会换成狙击枪，所以这里按射程、伤害和市场价值比较。
	/// </summary>
	public class JobGiver_MkVWeaponPickup : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Dead || pawn.Downed || !pawn.Spawned
				|| pawn.Drafted || pawn.Map == null || pawn.equipment == null
				|| !NannyBotUtility.Settings.mkvAutoPickupWeapons)
			{
				return null;
			}
			if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackStatic)
			{
				return null;
			}

			Thing current = pawn.equipment.Primary;
			float currentScore = Score(current?.def);
			Thing best = null;
			float bestScore = currentScore;

			foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon))
			{
				if (thing == null || thing.Destroyed || !thing.Spawned || thing == current)
				{
					continue;
				}
				if (!thing.def.IsRangedWeapon || thing.IsForbidden(pawn)
					|| !pawn.CanReserve(thing) || !EquipmentUtility.CanEquip(thing, pawn))
				{
					continue;
				}
				if (pawn.Position.DistanceTo(thing.Position) >
					NannyBotUtility.Settings.mkvWeaponPickupRadius)
				{
					continue;
				}
				float score = Score(thing.def);
				if (score > bestScore)
				{
					best = thing;
					bestScore = score;
				}
			}

			if (best == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Equip, best);
			job.expiryInterval = 600;
			return job;
		}

		private static float Score(ThingDef def)
		{
			if (def == null || !def.IsRangedWeapon)
			{
				return -1f;
			}
			float range = 0f;
			if (def.Verbs != null)
			{
				for (int i = 0; i < def.Verbs.Count; i++)
				{
					range = Mathf.Max(range, def.Verbs[i].range);
				}
			}
			// 只有 Mk-V 会跑这个 JobGiver，自己出生自带的电弧发射器要把专属加成算进去，
			// 否则会因为面板射程偏低被换成实际更差的枪。
			DefModExtension_MkvExclusiveWeapon exclusive =
				def.GetModExtension<DefModExtension_MkvExclusiveWeapon>();
			if (exclusive != null && exclusive.rangeBonus > 0f)
			{
				range += exclusive.rangeBonus;
			}
			float damage = def.projectile?.GetDamageAmount(def, null) ?? 0f;
			int burst = 1;
			if (def.Verbs != null)
			{
				for (int i = 0; i < def.Verbs.Count; i++)
				{
					burst = Mathf.Max(burst, def.Verbs[i].burstShotCount);
				}
			}
			return def.BaseMarketValue + range * 2f + damage * burst * 12f;
		}
	}
}
