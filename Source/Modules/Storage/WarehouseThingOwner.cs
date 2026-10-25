using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 立体库内部库存：只在本容器内允许超过全局 ThingDef.stackLimit 的堆叠。
	/// 二级 ×3、三级再 ×2；库外仍使用全局 stackLimit，不受影响。
	/// </summary>
	public class WarehouseThingOwner : ThingOwner<Thing>
	{
		private bool bypassLimits;

		public WarehouseThingOwner()
		{
		}

		public WarehouseThingOwner(IThingHolder owner)
			: base(owner)
		{
		}

		/// <summary>当前实际生效的库内倍率；模块关闭或 Stack XXL 共存时为 1。</summary>
		public static int EffectiveMultiplier
		{
			get
			{
				StorageSettings settings = ColonyKitMod.Settings?.GetModuleSettings<StorageSettings>();
				if (settings == null || !settings.enabled || StorageStackLimitManager.StackXxlActive)
				{
					return 1;
				}
				return StorageResearch.WarehouseStackMultiplier;
			}
		}

		/// <summary>库内每堆的件数上限：全局上限 × 设备倍率；武器/衣物等不参与。</summary>
		public static int StackLimitFor(ThingDef def)
		{
			if (def == null)
			{
				return 1;
			}

			int global = Math.Max(1, def.stackLimit);
			if (!StorageStackRules.CanMultiply(def))
			{
				return global;
			}

			int multiplier = EffectiveMultiplier;
			if (multiplier <= 1)
			{
				return global;
			}

			long limit = (long)global * multiplier;
			return (int)Math.Min(int.MaxValue, Math.Max(1L, limit));
		}

		public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
		{
			if (item == null || item.stackCount <= 0)
			{
				return 0;
			}
			if (bypassLimits)
			{
				return item.stackCount;
			}

			int maxStacks = Building_VerticalWarehouse.MaxStacks;
			int limit = StackLimitFor(item.def);
			long num = 0;

			if (Count < maxStacks)
			{
				num += (long)(maxStacks - Count) * limit;
			}
			if (num >= item.stackCount)
			{
				return item.stackCount;
			}

			if (canMergeWithExistingStacks)
			{
				for (int i = 0; i < Count; i++)
				{
					Thing stored = GetAt(i);
					if (stored == null || stored.stackCount >= limit || !stored.CanStackWith(item))
					{
						continue;
					}
					num += limit - stored.stackCount;
					if (num >= item.stackCount)
					{
						return item.stackCount;
					}
				}
			}

			return (int)Math.Min(num, item.stackCount);
		}

		public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
		{
			if (item == null)
			{
				return false;
			}
			if (Contains(item))
			{
				Log.Warning("Tried to add " + item.ToStringSafe() + " to warehouse but this item is already here.");
				return false;
			}
			if (item.holdingOwner != null && !bypassLimits)
			{
				Log.Warning(
					"Tried to add " + item.ToStringSafe() + " to warehouse but this thing is already in another container.");
				return false;
			}
			if (!CanAcceptAnyOf(item, canMergeWithExistingStacks))
			{
				return false;
			}

			if (canMergeWithExistingStacks)
			{
				int limit = StackLimitFor(item.def);
				for (int i = 0; i < Count; i++)
				{
					Thing stored = GetAt(i);
					if (stored == null || stored.stackCount >= limit || !stored.CanStackWith(item))
					{
						continue;
					}

					int room = limit - stored.stackCount;
					int num = Mathf.Min(item.stackCount, room);
					if (num <= 0)
					{
						continue;
					}

					Thing part = item.SplitOff(num);
					stored.TryAbsorbStack(part, respectStackLimit: false);
					if (item.Destroyed || item.stackCount <= 0)
					{
						return true;
					}
				}
			}

			return base.TryAdd(item, canMergeWithExistingStacks: false);
		}

		/// <summary>读档时把旧版 ThingOwner 的内容搬进本容器；不按上限截断。</summary>
		internal void AddRestored(Thing thing)
		{
			if (thing == null)
			{
				return;
			}

			TryAddForced(thing);
		}

		/// <summary>回滚用：绕过堆数/上限检查，把取出的堆放回原容器。</summary>
		internal bool TryAddForced(Thing thing)
		{
			if (thing == null)
			{
				return false;
			}

			bypassLimits = true;
			try
			{
				if (!base.TryAdd(thing, canMergeWithExistingStacks: false))
				{
					Log.Error("[ColonyKit] failed to restore warehouse stack: " + thing.ToStringSafe());
					return false;
				}
				return true;
			}
			finally
			{
				bypassLimits = false;
			}
		}
	}
}
