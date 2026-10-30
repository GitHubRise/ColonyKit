using System.Collections.Generic;
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

		/// <summary>地上任意能用的容器（脸盆或水桶），给手动打水用。</summary>
		public static Thing FindSpawnedContainer(Pawn pawn, bool requireWater)
		{
			Map map = pawn?.Map;
			if (map == null)
			{
				return null;
			}
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
				if (pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					return thing;
				}
			}
			return null;
		}

		/// <summary>自动取水用的水桶：背包里先找，其次地上找。</summary>
		public static Thing FindBucketForJob(Pawn pawn)
		{
			CompBasinWater carried = CarriedBucket(pawn);
			if (carried != null)
			{
				return carried.parent;
			}
			Thing spawned = FindSpawnedBucket(pawn, requireWater: false);
			if (spawned != null && pawn.CanReserve(spawned))
			{
				return spawned;
			}
			return null;
		}
	}
}
