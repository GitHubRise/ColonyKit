using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	/// <summary>
	/// DBH CompSewageOutlet 的移植：把污水倒到附近的空地上，交给 CK 的污染网格去扩散/降解。
	/// 附近没有能倒的格子时置 Blocked，污水会留在缓冲里并继续往上游积。
	/// </summary>
	public class CompSewageOutlet : CompSewageHandler
	{
		/// <summary>倒污水的范围（按朝向在建筑前方 3×5）。</summary>
		public static CellRect GetDumpCellRect(IntVec3 position, Rot4 rotation)
		{
			if (rotation == Rot4.North)
			{
				return new CellRect(position.x - 1, position.z + 1, 3, 5);
			}
			if (rotation == Rot4.South)
			{
				return new CellRect(position.x - 1, position.z - 5, 3, 5);
			}
			if (rotation == Rot4.East)
			{
				return new CellRect(position.x + 1, position.z - 1, 5, 3);
			}
			return new CellRect(position.x - 5, position.z - 1, 5, 3);
		}

		public bool GetSewageDumpSpot(out IntVec3 spot)
		{
			spot = IntVec3.Invalid;
			Map map = parent.Map;
			MapComponent_SewagePollution pollution = map?.GetComponent<MapComponent_SewagePollution>();
			if (pollution == null)
			{
				return false;
			}
			foreach (IntVec3 cell in GetDumpCellRect(parent.Position, parent.Rotation).Cells)
			{
				if (!cell.InBounds(map) || pollution.PollutionAt(cell) > 3f)
				{
					continue;
				}
				if (parent.Position.DistanceTo(cell) < parent.Position.DistanceTo(spot))
				{
					spot = cell;
				}
			}
			return spot.IsValid;
		}

		public override void CompTick()
		{
			base.CompTick();
			if (parent.Map == null || !parent.IsHashIntervalTick(60))
			{
				return;
			}
			MapComponent_SewagePollution pollution = parent.Map.GetComponent<MapComponent_SewagePollution>();
			if (!GetSewageDumpSpot(out IntVec3 spot) || pollution == null)
			{
				Blocked = true;
				return;
			}
			Blocked = false;
			if (sewageBuffer <= 1f)
			{
				return;
			}
			int dumped = Mathf.RoundToInt(sewageBuffer);
			sewageBuffer -= dumped;
			pollution.AddPollution(spot, dumped);
		}
	}
}
