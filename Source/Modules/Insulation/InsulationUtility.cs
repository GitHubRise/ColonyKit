using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 多墙体保温的几何计算：按房间四周的连续墙层数求平均保温系数。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 物理模型：一维稳态导热，总热阻 = 内外空气膜热阻 + 墙体热阻。
	/// 设单层墙热阻 R，空气膜合计 r·R，则 d 层墙的热流相对单层墙为：
	/// </para>
	/// <code>
	/// f(d) = (1 + r) / (1 + r · d)
	/// </code>
	/// <para>
	/// 因此 f(1) = 1（与 1.6 原版结算完全一致），f(2) 在 r=3 时约 0.571，
	/// f(3) 约 0.4。r 由设置页提供，默认 3，相当于“墙的热阻约是两侧空气膜合计的 3 倍”。
	/// </para>
	/// <para>
	/// 采样直接复用 <see cref="RoomTempTracker.EqualizeCellsForReading"/>：那正是原版
	/// 参与墙体均温的外圈格，已经随房间变化缓存好，避免每次重扫整间房。
	/// 对每个外圈格反推方向，再从房间往外数连续墙层；朝向房间内部的边不计入。
	/// </para>
	/// </remarks>
	public static class InsulationUtility
	{
		private const int MaxSamples = 256;

		/// <summary>房间墙体系数；未启用/无设置时返回 1（保持原版）。</summary>
		public static float FactorFor(Room room, Map map, List<IntVec3> equalizeCells)
		{
			InsulationSettings settings = InsulationSettingsUtility.Current;
			if (room == null || map == null || settings == null || !settings.enabled || equalizeCells == null)
			{
				return 1f;
			}

			float sum = 0f;
			int edges = 0;
			int step = equalizeCells.Count > MaxSamples
				? equalizeCells.Count / MaxSamples
				: 1;

			for (int index = 0; index < equalizeCells.Count; index += step)
			{
				IntVec3 equalizeCell = equalizeCells[index];

				for (int i = 0; i < 4; i++)
				{
					IntVec3 direction = GenAdj.CardinalDirections[i];
					IntVec3 roomCell = equalizeCell - direction * 2;
					if (!roomCell.InBounds(map) || roomCell.GetRoom(map) != room)
					{
						continue;
					}

					int layers = WallLayers(equalizeCell - direction, direction, map, settings);
					sum += layers > 0 ? Factor(layers, settings.wallResistanceRatio) : 1f;
					edges++;
					break;
				}
			}

			return edges > 0 ? sum / edges : 1f;
		}

		/// <summary>f(d) = (1 + r) / (1 + r·d)；d ≤ 1 时返回 1。</summary>
		public static float Factor(int layers, float resistanceRatio)
		{
			if (layers <= 1)
			{
				return 1f;
			}

			return (1f + resistanceRatio) / (1f + resistanceRatio * layers);
		}

		/// <summary>从 <paramref name="first"/> 开始沿方向连续数墙，最多 maxLayers 层。</summary>
		private static int WallLayers(IntVec3 first, IntVec3 direction, Map map, InsulationSettings settings)
		{
			int layers = 0;
			IntVec3 cell = first;

			while (layers < settings.maxLayers && cell.InBounds(map) && IsWallCell(cell, map, settings))
			{
				layers++;
				cell += direction;
			}

			return layers;
		}

		/// <summary>可计入保温的墙体：玩家建造的承重建筑；可选自然岩层。</summary>
		public static bool IsWallCell(IntVec3 cell, Map map, InsulationSettings settings)
		{
			Thing edifice = cell.GetEdifice(map);
			if (edifice != null
				&& edifice.def != null
				&& edifice.def.building != null
				&& edifice.def.holdsRoof
				&& !(edifice is Building_Door))
			{
				// 自然岩层默认不算；开启后才参与，避免山洞被无意叠成“无限保温”。
				if (edifice.def.building.isNaturalRock && !settings.includeNaturalRock)
				{
					return false;
				}

				return true;
			}

			if (settings.includeNaturalRock)
			{
				Mineable rock = cell.GetFirstThing<Mineable>(map);
				if (rock != null
					&& rock.def != null
					&& rock.def.building != null
					&& rock.def.building.isNaturalRock)
				{
					return true;
				}
			}

			return false;
		}
	}
}
