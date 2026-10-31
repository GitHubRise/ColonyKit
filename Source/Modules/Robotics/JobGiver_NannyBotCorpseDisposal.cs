using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 专用：处理敌人的尸体。
	/// 优先把尸体送进燃烧坑/焚化炉的焚化账单；没有可用的焚化账单时，
	/// 把尸体搬进允许尸体的库存区（尸体存放区）。
	/// </summary>
	public class JobGiver_NannyBotCorpseDisposal : JobGiver_NannyBotChore
	{
		private const string CremateRecipeDefName = "CremateCorpse";

		private static Dictionary<ThingDef, List<WorkGiver_DoBill>> workersByGiverDef;
		private static int cachedWorkGiverCount = -1;

		protected override RobotChore Chore => RobotChore.CorpseDisposal;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || !NannyBotUtility.IsMkV(pawn) || pawn.Map == null)
			{
				return null;
			}
			if (!HasEnemyCorpse(pawn))
			{
				return null;
			}

			Job cremateJob = CremationJob(pawn);
			if (cremateJob != null)
			{
				return cremateJob;
			}
			return HaulToStorageJob(pawn);
		}

		/// <summary>地图上有敌人尸体，且至少一具在 Mk-V 的工作范围内。</summary>
		private static bool HasEnemyCorpse(Pawn pawn)
		{
			List<Thing> corpses = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
			for (int i = 0; i < corpses.Count; i++)
			{
				if (corpses[i] is Corpse corpse && IsEnemyCorpse(corpse)
					&& NannyBotUtility.IsWithinWorkArea(pawn, corpse.Position))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsEnemyCorpse(Corpse corpse)
		{
			Pawn inner = corpse?.InnerPawn;
			if (inner == null || inner.Faction == null)
			{
				return false;
			}
			return inner.Faction.HostileTo(Faction.OfPlayer);
		}

		/// <summary>找到带焚化账单的燃烧坑/焚化炉，交给对应的 DoBill 工作器。</summary>
		private static Job CremationJob(Pawn pawn)
		{
			List<Building> buildings = pawn.Map.listerBuildings.allBuildingsColonist;
			for (int i = 0; i < buildings.Count; i++)
			{
				Building building = buildings[i];
				if (!building.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, building.Position))
				{
					continue;
				}
				IBillGiver giver = building as IBillGiver;
				if (giver == null || !HasCremateBill(giver))
				{
					continue;
				}
				// 敌人在家区域外死亡时尸体默认被禁用，焚化账单的原料搜索会跳过禁用物品。
				UnforbidEnemyCorpses(pawn);
				List<WorkGiver_DoBill> workers = WorkersFor(building.def);
				if (workers == null)
				{
					continue;
				}
				for (int j = 0; j < workers.Count; j++)
				{
					Job job = workers[j].JobOnThing(pawn, building);
					if (job != null)
					{
						return job;
					}
				}
			}
			return null;
		}

		private static bool HasCremateBill(IBillGiver giver)
		{
			BillStack stack = giver.BillStack;
			for (int i = 0; i < stack.Count; i++)
			{
				Bill bill = stack[i];
				if (bill?.recipe != null
					&& bill.recipe.defName == CremateRecipeDefName
					&& bill.ShouldDoNow())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>没有焚化账单可做时，把最近的敌人尸体搬进允许尸体的库存区。</summary>
		private static Job HaulToStorageJob(Pawn pawn)
		{
			Corpse best = null;
			float bestDistance = float.MaxValue;
			List<Thing> corpses = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
			for (int i = 0; i < corpses.Count; i++)
			{
				if (!(corpses[i] is Corpse corpse) || !IsEnemyCorpse(corpse) || !corpse.Spawned
					|| corpse.IsBurning() || corpse.IsInValidStorage()
					|| !NannyBotUtility.IsWithinWorkArea(pawn, corpse.Position)
					|| !pawn.CanReserve(corpse, 1, -1, null, false))
				{
					continue;
				}
				float distance = corpse.Position.DistanceToSquared(pawn.Position);
				if (distance < bestDistance)
				{
					best = corpse;
					bestDistance = distance;
				}
			}
			if (best == null
				|| !pawn.CanReach(best, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
			{
				return null;
			}
			// 敌人在家区域外死亡时尸体默认被禁用，搬运前先解禁。
			best.SetForbidden(false, false);
			return HaulAIUtility.HaulToStorageJob(pawn, best, false);
		}

		private static void UnforbidEnemyCorpses(Pawn pawn)
		{
			List<Thing> corpses = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
			for (int i = 0; i < corpses.Count; i++)
			{
				if (corpses[i] is Corpse corpse && IsEnemyCorpse(corpse) && corpse.Spawned
					&& NannyBotUtility.IsWithinWorkArea(pawn, corpse.Position))
				{
					corpse.SetForbidden(false, false);
				}
			}
		}

		/// <summary>按建筑 Def 查可处理它的 DoBill 工作器（燃烧坑的 CK_DoBillsBurnPit 等）。</summary>
		private static List<WorkGiver_DoBill> WorkersFor(ThingDef giverDef)
		{
			List<WorkGiverDef> all = DefDatabase<WorkGiverDef>.AllDefsListForReading;
			if (workersByGiverDef == null || cachedWorkGiverCount != all.Count)
			{
				workersByGiverDef = new Dictionary<ThingDef, List<WorkGiver_DoBill>>();
				cachedWorkGiverCount = all.Count;
				for (int i = 0; i < all.Count; i++)
				{
					WorkGiverDef def = all[i];
					if (!(def.Worker is WorkGiver_DoBill worker) || def.fixedBillGiverDefs == null)
					{
						continue;
					}
					for (int j = 0; j < def.fixedBillGiverDefs.Count; j++)
					{
						ThingDef target = def.fixedBillGiverDefs[j];
						if (target == null)
						{
							continue;
						}
						List<WorkGiver_DoBill> list;
						if (!workersByGiverDef.TryGetValue(target, out list))
						{
							list = new List<WorkGiver_DoBill>();
							workersByGiverDef[target] = list;
						}
						if (!list.Contains(worker))
						{
							list.Add(worker);
						}
					}
				}
			}
			List<WorkGiver_DoBill> result;
			workersByGiverDef.TryGetValue(giverDef, out result);
			return result;
		}
	}
}
