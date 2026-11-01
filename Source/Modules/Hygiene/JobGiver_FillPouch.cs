using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 殖民者/奴隶水袋低于 30 L 时，去仓库拿 1 L 瓶装水灌满水袋（目标 30 L）。
	/// 优先级排在普通工作之前一点点（5.6，工作 5.5），但低于吃饭/睡觉；
	/// 因为水袋 30 L 能顶好几天，实际很少打断工作。
	/// </summary>
	public class JobGiver_FillPouch : ThinkNode_JobGiver
	{
		private const float Priority = 5.6f;

		public override float GetPriority(Pawn pawn)
		{
			if (!HygieneSettings.Current.ShouldRunHygiene
				|| pawn?.needs == null
				|| (!pawn.IsColonist && !pawn.IsSlave)
				|| pawn.Downed
				|| pawn.Drafted
				|| pawn.InMentalState
				|| pawn.GetCaravan() != null)
			{
				return 0f;
			}
			if (pawn.needs.TryGetNeed<Need_Thirst>() == null)
			{
				return 0f;
			}
			if (WaterPouchUtility.GetWater(pawn) >= WaterPouchUtility.FillTarget - 0.5f)
			{
				return 0f;
			}
			return Priority;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (GetPriority(pawn) <= 0f)
			{
				return null;
			}
			float need = WaterPouchUtility.FillTarget - WaterPouchUtility.GetWater(pawn);
			Thing stack = FindBottleStack(pawn, need);
			if (stack == null)
			{
				return null;
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_FillWaterPouch");
			if (def == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def, stack);
			job.count = Mathf.Min(stack.stackCount, Mathf.CeilToInt(need));
			return job;
		}

		private static Thing FindBottleStack(Pawn pawn, float need)
		{
			if (pawn.Map == null)
			{
				return null;
			}
			int minTake = Mathf.Max(1, Mathf.Min(5, Mathf.CeilToInt(need)));
			Thing best = null;
			float bestDistance = float.MaxValue;
			ThingDef[] defs =
			{
				MineralWaterUtility.BoiledDef,
				MineralWaterUtility.PurifiedDef,
				MineralWaterUtility.Def
			};
			for (int i = 0; i < defs.Length; i++)
			{
				ThingDef def = defs[i];
				if (def == null)
				{
					continue;
				}
				foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(def))
				{
					if (thing.stackCount < minTake
						|| thing.IsForbidden(pawn)
						|| !thing.IsInAnyStorage()
						|| !pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
					{
						continue;
					}
					float distance = thing.Position.DistanceToSquared(pawn.Position);
					if (distance < bestDistance)
					{
						bestDistance = distance;
						best = thing;
					}
				}
				if (best != null)
				{
					return best;
				}
			}
			return best;
		}
	}
}
