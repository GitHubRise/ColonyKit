using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>水网首版：管道/水箱/水泵按相邻连通，淋浴从连通储量取水。</summary>
	public static class WaterNetwork
	{
		public static bool HasInfrastructure(Thing root)
		{
			return CollectStores(root).Count > 0;
		}

		public static bool TryConsume(Thing root, float liters)
		{
			List<IWaterStore> stores = CollectStores(root);
			float total = 0f;
			for (int i = 0; i < stores.Count; i++)
			{
				total += stores[i].WaterStored;
			}
			if (total < liters)
			{
				return false;
			}

			float remaining = liters;
			for (int i = 0; i < stores.Count && remaining > 0.0001f; i++)
			{
				IWaterStore store = stores[i];
				float take = Mathf.Min(store.WaterStored, remaining);
				store.WaterStored -= take;
				remaining -= take;
			}
			return true;
		}

		public static float TotalWater(Thing root)
		{
			float total = 0f;
			foreach (IWaterStore store in CollectStores(root))
			{
				total += store.WaterStored;
			}
			return total;
		}

		public static List<IWaterStore> CollectStores(Thing root)
		{
			var stores = new List<IWaterStore>();
			if (root == null || root.Map == null)
			{
				return stores;
			}

			var visited = new HashSet<Thing>();
			var queue = new Queue<Thing>();
			foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(root))
			{
				Enqueue(cell, root.Map, queue, visited);
			}

			while (queue.Count > 0)
			{
				Thing thing = queue.Dequeue();
				if (thing is IWaterStore store)
				{
					stores.Add(store);
				}

				foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(thing))
				{
					Enqueue(cell, root.Map, queue, visited);
				}
			}
			return stores;
		}

		public static void FillConnectedTanks(Building_WaterPump pump)
		{
			if (pump == null || pump.WaterStored <= 0.0001f)
			{
				return;
			}

			foreach (IWaterStore store in CollectStores(pump))
			{
				if (store == pump || store is Building_WaterPump)
				{
					continue;
				}
				float space = store.Capacity - store.WaterStored;
				float amount = Mathf.Min(space, pump.WaterStored);
				if (amount <= 0f)
				{
					continue;
				}
				store.WaterStored += amount;
				pump.WaterStored -= amount;
				if (pump.WaterStored <= 0.0001f)
				{
					break;
				}
			}
		}

		private static void Enqueue(IntVec3 cell, Map map, Queue<Thing> queue, HashSet<Thing> visited)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			foreach (Thing thing in map.thingGrid.ThingsListAt(cell))
			{
				if (IsWaterThing(thing) && visited.Add(thing))
				{
					queue.Enqueue(thing);
				}
			}
		}

		private static bool IsWaterThing(Thing thing)
		{
			return thing is Building_WaterPipe
				|| thing is Building_WaterTank
				|| thing is Building_WaterPump;
		}
	}

	public interface IWaterStore
	{
		float WaterStored { get; set; }
		float Capacity { get; }
	}
}
