using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 便携水容器（脸盆 / 水桶）的公共查找。两者都带 <see cref="CompBasinWater"/>，
	/// 区别只在容量与 Def；取水/倒水/喝水都走这里，避免每个 Job 各写一套背包遍历。
	/// </summary>
	internal static class WaterContainerUtility
	{
		private static ThingDef cachedBucketDef;

		public static ThingDef BucketDef
		{
			get
			{
				if (cachedBucketDef == null)
				{
					cachedBucketDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_WaterBucket");
				}
				return cachedBucketDef;
			}
		}

		public static bool IsBucket(CompBasinWater container)
		{
			return container != null && container.parent != null && container.parent.def == BucketDef;
		}

		/// <summary>背包里最合适的容器：优先大容量；requireWater 时只找有水的，否则只找没满的。</summary>
		public static CompBasinWater CarriedContainer(Pawn pawn, bool requireWater)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			CompBasinWater best = null;
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null
					|| (requireWater ? container.IsEmpty : container.IsFull))
				{
					continue;
				}
				if (best == null || container.Props.Capacity > best.Props.Capacity)
				{
					best = container;
				}
			}
			return best;
		}

		/// <summary>背包里任意容器（不管满没满），用于给出「已经满了」的提示。</summary>
		public static CompBasinWater CarriedContainerAny(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			CompBasinWater best = null;
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container != null && (best == null || container.Props.Capacity > best.Props.Capacity))
				{
					best = container;
				}
			}
			return best;
		}

		/// <summary>背包里的脸盆（不含水桶）；默认只找没满的，打水用。</summary>
		public static CompBasinWater CarriedBasin(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			CompBasinWater best = null;
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null || IsBucket(container) || container.IsFull)
				{
					continue;
				}
				if (best == null || container.Props.Capacity > best.Props.Capacity)
				{
					best = container;
				}
			}
			return best;
		}

		/// <summary>地上能用的脸盆（不含水桶）；找不到返回 null。</summary>
		public static Thing FindSpawnedBasin(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.Map.listerThings.AllThings)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null
					|| IsBucket(container)
					|| container.IsFull
					|| thing.IsForbidden(pawn)
					|| thing.IsBurning())
				{
					continue;
				}
				if (pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					return thing;
				}
			}
			return null;
		}

		/// <summary>背包里的水桶；默认只找没满的（打水用），requireWater 时只找有水的。</summary>
		public static CompBasinWater CarriedBucket(Pawn pawn, bool requireWater = false)
		{
			return CarriedBucketInternal(pawn, requireWater, allowFull: false);
		}

		/// <summary>背包里的水桶，满的也算（倒水/结算用）。</summary>
		public static CompBasinWater CarriedBucketAny(Pawn pawn)
		{
			return CarriedBucketInternal(pawn, requireWater: false, allowFull: true);
		}

		private static CompBasinWater CarriedBucketInternal(Pawn pawn, bool requireWater, bool allowFull)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			CompBasinWater best = null;
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null
					|| !IsBucket(container)
					|| (requireWater ? container.IsEmpty : (!allowFull && container.IsFull)))
				{
					continue;
				}
				if (best == null || container.Props.Capacity > best.Props.Capacity)
				{
					best = container;
				}
			}
			return best;
		}

		/// <summary>地上能用的水桶：优先没满的；找不到返回 null。</summary>
		public static Thing FindSpawnedBucket(Pawn pawn, bool requireWater)
		{
			if (BucketDef == null || pawn?.Map == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(BucketDef))
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null
					|| (requireWater ? container.IsEmpty : container.IsFull)
					|| thing.IsForbidden(pawn)
					|| thing.IsBurning())
				{
					continue;
				}
				if (pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					return thing;
				}
			}
			return null;
		}

		/// <summary>地上任意能用的容器（脸盆或水桶），给手动打水和就地喝水用。</summary>
		public static Thing FindSpawnedContainer(Pawn pawn, bool requireWater, float maxDistance = 9999f)
		{
			Map map = pawn?.Map;
			if (map == null)
			{
				return null;
			}
			Thing best = null;
			float bestDistance = maxDistance * maxDistance;
			foreach (Thing thing in map.listerThings.AllThings)
			{
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null
					|| (requireWater ? container.IsEmpty : container.IsFull)
					|| thing.IsForbidden(pawn)
					|| thing.IsBurning())
				{
					continue;
				}
				float distance = thing.Position.DistanceToSquared(pawn.Position);
				if (distance > bestDistance)
				{
					continue;
				}
				if (pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					bestDistance = distance;
					best = thing;
				}
			}
			return best;
		}

		/// <summary>自动取水用的水桶：背包里先找，其次地上找。</summary>
		public static Thing FindBucketForJob(Pawn pawn)
		{
			return FindBucketsForJob(pawn, 1).FirstOrDefault();
		}

		/// <summary>
		/// 一次派活最多要几只空/半满水桶：先拿背包里的，再找地上可达的。
		/// 批量取水用；背包里的桶不需要再走去拾取。
		/// </summary>
		public static List<Thing> FindBucketsForJob(Pawn pawn, int max)
		{
			List<Thing> result = new List<Thing>();
			if (pawn?.Map == null || max <= 0)
			{
				return result;
			}
			if (pawn.inventory?.innerContainer != null)
			{
				foreach (Thing thing in pawn.inventory.innerContainer)
				{
					CompBasinWater container = thing.TryGetComp<CompBasinWater>();
					if (container == null || !IsBucket(container) || container.IsFull)
					{
						continue;
					}
					result.Add(thing);
					if (result.Count >= max)
					{
						return result;
					}
				}
			}
			if (BucketDef == null)
			{
				return result;
			}
			foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(BucketDef))
			{
				if (result.Count >= max)
				{
					break;
				}
				CompBasinWater container = thing.TryGetComp<CompBasinWater>();
				if (container == null
					|| container.IsFull
					|| result.Contains(thing)
					|| thing.IsForbidden(pawn)
					|| thing.IsBurning())
				{
					continue;
				}
				if (pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					result.Add(thing);
				}
			}
			return result;
		}

		/// <summary>地图上 + 殖民者物品栏里的满水桶数；给「维持 X 个满桶」账单计数。</summary>
		public static int CountFullBuckets(Map map)
		{
			if (map == null || BucketDef == null)
			{
				return 0;
			}
			int count = 0;
			foreach (Thing thing in map.listerThings.ThingsOfDef(BucketDef))
			{
				if (IsFullBucket(thing))
				{
					count++;
				}
			}
			foreach (Pawn pawn in map.mapPawns.FreeColonists)
			{
				if (pawn.inventory?.innerContainer == null)
				{
					continue;
				}
				foreach (Thing thing in pawn.inventory.innerContainer)
				{
					if (thing.def == BucketDef && IsFullBucket(thing))
					{
						count++;
					}
				}
			}
			return count;
		}

		private static bool IsFullBucket(Thing thing)
		{
			CompBasinWater container = thing.TryGetComp<CompBasinWater>();
			return container != null && container.Water >= 59f;
		}
	}
}
