using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 自动拾取更好的武器。原版 JobGiver_PickUpOpportunisticWeapon 只比较「远程/近战」大类，
	/// 已经有电弧发射器时不会换成狙击枪，所以这里按射程、伤害、市场价值评分。
	/// 开启双持后还会用第二只手补一把武器：空着副手位就捡最值得拿的，两把都满时只换掉较差的那把。
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

			bool dual = MkVLoadout.Enabled;
			List<ThingWithComps> held = MkVLoadout.HeldWeapons(pawn);
			ThingWithComps primary = pawn.equipment.Primary;

			// 双持：空位从 0 分起算，两把都满时只换掉评分更低的那把；
			// 关闭双持：沿用旧规则，只有比主手更好的远程武器才换。
			float baseline = dual
				? MkVLoadout.PickupBaseline(pawn, held)
				: (primary != null ? MkVLoadout.WeaponScore(primary.def) : -1f);

			Thing best = null;
			float bestScore = baseline;
			foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon))
			{
				if (thing == null || thing.Destroyed || !thing.Spawned || thing.def == null)
				{
					continue;
				}
				ThingWithComps candidate = thing as ThingWithComps;
				if (candidate == null || held.Contains(candidate))
				{
					continue;
				}
				// 空手或关闭双持时只捡远程武器，否则 Mk-V 会拿着一把刀上战场。
				if ((!dual || held.Count == 0) && !thing.def.IsRangedWeapon)
				{
					continue;
				}
				if (!dual && thing == primary)
				{
					continue;
				}
				if (thing.IsForbidden(pawn) || !pawn.CanReserve(thing)
					|| !EquipmentUtility.CanEquip(thing, pawn))
				{
					continue;
				}
				if (pawn.Position.DistanceTo(thing.Position) >
					NannyBotUtility.Settings.mkvWeaponPickupRadius)
				{
					continue;
				}
				float score = MkVLoadout.WeaponScore(thing.def);
				if (score <= bestScore)
				{
					continue;
				}
				best = thing;
				bestScore = score;
			}

			if (best == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Equip, best);
			job.expiryInterval = 600;
			return job;
		}
	}
}
