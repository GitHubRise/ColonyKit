using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>污水污染网格：向地形/水体扩散、衰减，并让水源变脏。</summary>
	public class MapComponent_SewagePollution : MapComponent
	{
		private float[] pollution;
		private bool initialized;
		private bool waterPolluted;

		public MapComponent_SewagePollution(Map map) : base(map)
		{
			pollution = new float[map.cellIndices.NumGridCells];
			initialized = true;
		}

		public float PollutionAt(IntVec3 cell)
		{
			return cell.InBounds(map) ? pollution[map.cellIndices.CellToIndex(cell)] : 0f;
		}

		public void AddPollution(IntVec3 cell, float amount)
		{
			if (!cell.InBounds(map) || amount <= 0f)
			{
				return;
			}
			int index = map.cellIndices.CellToIndex(cell);
			pollution[index] = Mathf.Min(20f, pollution[index] + amount);
			if (pollution[index] > 1f && cell.GetTerrain(map).IsWater)
			{
				waterPolluted = true;
			}
		}

		/// <summary>
		/// 缓存结果。水塔每个 tick 都会读这个值，不能在这里扫描全图。
		/// </summary>
		public bool WaterPolluted => waterPolluted;

		private int tickCounter;

		public override void MapComponentTick()
		{
			if (++tickCounter < 250)
			{
				return;
			}
			tickCounter = 0;
			TickPollution();
		}

		private void TickPollution()
		{
			if (!initialized || pollution == null)
			{
				return;
			}
			int numCells = map.cellIndices.NumGridCells;
			float[] next = new float[numCells];
			for (int i = 0; i < numCells; i++)
			{
				float value = pollution[i];
				if (value <= 0.001f)
				{
					continue;
				}
				IntVec3 cell = map.cellIndices.IndexToCell(i);
				next[i] += value * 0.82f;
				float spread = value * 0.045f;
				foreach (IntVec3 neighbor in GenAdj.CardinalDirections)
				{
					IntVec3 target = cell + neighbor;
					if (target.InBounds(map))
					{
						next[map.cellIndices.CellToIndex(target)] += spread;
					}
				}
				if (value > 1.5f && !cell.GetTerrain(map).IsWater)
				{
					ThingDef filth = DefDatabase<ThingDef>.GetNamedSilentFail("FilthFaeces");
					if (filth != null && cell.GetFirstThing(map, filth) == null)
					{
						FilthMaker.TryMakeFilth(cell, map, filth);
					}
				}
			}
			pollution = next;
			RefreshWaterPolluted();
		}

		private void RefreshWaterPolluted()
		{
			waterPolluted = false;
			for (int i = 0; i < pollution.Length; i++)
			{
				if (pollution[i] > 1f
					&& map.cellIndices.IndexToCell(i).GetTerrain(map).IsWater)
				{
					waterPolluted = true;
					return;
				}
			}
		}
	}
}
