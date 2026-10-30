using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 级联冷室：把「三台（或更多）同向串联的空调」按理想级联结算。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 原版每台空调的效率是 <c>1 - (热侧温度 - 冷侧温度) / 130</c>，所以单段最多把房间压到
	/// 热侧以下 130°C；要压到 -273°C 只能靠多段串联，而且每段冷室都得是独立、漏热可控的小房间。
	/// </para>
	/// <para>
	/// 本类识别「同向、间隔 2 格、中间夹着四面封死的 1 格小室」的空调链（至少
	/// <see cref="MinChainLength"/> 台，即内置「低版本永恒温室」十字带那种走法）。识别成功后：
	/// </para>
	/// <list type="number">
	/// <item>第 k 段的冷室目标温度 = 热端温度 - 130 × (k + 1)，最低 -273.15°C；</item>
	/// <item>每 <c>TickRare</c> 把各段冷室温度向目标拉最多 <see cref="StepPerTick"/> 度；</item>
	/// <item>链上冷室（含链末端连通的门廊）在链正常工作时按理想保温腔结算，墙体 / 薄顶 / 无顶漏热为 0。</item>
	/// </list>
	/// <para>
	/// 任一段断电、或玩家把某段目标温度设得不够低，整条链立即退回原版结算（漏热恢复，冷室回温）。
	/// 链长不足 3、间隔不是 2 格、中间小室没封死、冷室超过 <see cref="MaxColdRoomCells"/> 格，
	/// 都不会触发，因此只对这类结构生效。
	/// </para>
	/// </remarks>
	public static class CascadeFreeze
	{
		/// <summary>触发级联所需的最少台数。</summary>
		public const int MinChainLength = 3;

		/// <summary>相邻两台空调的间距（格）：中间那格就是该段冷室。</summary>
		public const int ChainSpacing = 2;

		/// <summary>单段空调能撑住的温差上限，与原版效率公式里的 130 一致。</summary>
		public const float StageDrop = 130f;

		/// <summary>温度下限，与 <c>RoomTempTracker</c> 的取值一致。</summary>
		public const float FloorTemperature = -273.15f;

		/// <summary>每个 <c>TickRare</c> 最多拉低多少度，避免温度瞬间跳变。</summary>
		public const float StepPerTick = 60f;

		/// <summary>冷室超过这个格数就不按级联冷腔处理，避免把大厅也冻住。</summary>
		public const int MaxColdRoomCells = 32;

		/// <summary>末端门廊最多扫多少格。</summary>
		private const int MaxChamberCells = 128;

		private static int builtTick = -1;
		private static Map builtMap;

		/// <summary>自检专用：跳过通电判定，只验证链识别与温度结算。</summary>
		internal static bool TestBypassPower;
		private static readonly Dictionary<Room, float> targetByRoom = new Dictionary<Room, float>();
		private static readonly HashSet<Room> insulatedRooms = new HashSet<Room>();
		private static readonly List<Building_Cooler> coolers = new List<Building_Cooler>();
		private static readonly HashSet<Building_Cooler> visited = new HashSet<Building_Cooler>();
		private static readonly List<Building_Cooler> chain = new List<Building_Cooler>();
		private static readonly List<IntVec3> frontier = new List<IntVec3>();
		private static readonly HashSet<IntVec3> chamberSeen = new HashSet<IntVec3>();

		/// <summary>该房间此刻是否按「理想级联冷腔」结算漏热。</summary>
		public static bool IsInsulated(Room room)
		{
			if (room == null || room.Map != builtMap || builtTick < 0
				|| GenTicks.TicksGame - builtTick > 250)
			{
				return false;
			}

			if (!ModuleRegistry.IsEnabled(InsulationModule.ModuleId))
			{
				return false;
			}

			InsulationSettings settings = InsulationSettingsUtility.Current;
			if (settings == null || !settings.enabled || !settings.cascadeFreeze)
			{
				return false;
			}

			return insulatedRooms.Contains(room);
		}

		/// <summary>取该房间的级联目标温度；不是级联冷腔时返回 false。</summary>
		public static bool TryGetTarget(Room room, out float target)
		{
			target = 0f;
			return room != null && IsInsulated(room) && targetByRoom.TryGetValue(room, out target);
		}

		/// <summary>某个 <c>TickRare</c> 里重建链、写入目标温度并拉低冷室。</summary>
		public static void Tick(Map map)
		{
			if (map == null || builtTick == GenTicks.TicksGame)
			{
				return;
			}

			InsulationSettings settings = InsulationSettingsUtility.Current;
			if (!ModuleRegistry.IsEnabled(InsulationModule.ModuleId)
				|| settings == null
				|| !settings.enabled
				|| !settings.cascadeFreeze)
			{
				Clear();
				return;
			}

			builtTick = GenTicks.TicksGame;
			builtMap = map;
			targetByRoom.Clear();
			insulatedRooms.Clear();
			visited.Clear();
			coolers.Clear();

			foreach (Building building in map.listerBuildings.allBuildingsColonist)
			{
				if (building is Building_Cooler cooler)
				{
					coolers.Add(cooler);
				}
			}

			for (int i = 0; i < coolers.Count; i++)
			{
				Building_Cooler cooler = coolers[i];
				if (visited.Contains(cooler))
				{
					continue;
				}

				chain.Clear();
				if (!BuildChain(cooler, chain) || chain.Count < MinChainLength)
				{
					continue;
				}

				for (int j = 0; j < chain.Count; j++)
				{
					visited.Add(chain[j]);
				}

				ApplyChain(map, chain);
			}

			foreach (KeyValuePair<Room, float> pair in targetByRoom)
			{
				Room room = pair.Key;
				if (room != null && room.Map == builtMap)
				{
					room.Temperature = Mathf.MoveTowards(room.Temperature, pair.Value, StepPerTick);
				}
			}
		}

		/// <summary>
		/// 以 <paramref name="cooler"/> 为起点，沿冷侧方向（间隔 <see cref="ChainSpacing"/> 格）
		/// 向两端展开，收集同朝向、且中间小室封死的空调链。
		/// </summary>
		private static bool BuildChain(Building_Cooler cooler, List<Building_Cooler> result)
		{
			IntVec3 direction = IntVec3.South.RotatedBy(cooler.Rotation);

			IntVec3 head = cooler.Position;
			while (TryCoolerAt(cooler, head - direction * ChainSpacing, out Building_Cooler previous)
				&& PocketSealed(cooler.Map, head - direction))
			{
				head = previous.Position;
			}

			IntVec3 position = head;
			while (TryCoolerAt(cooler, position, out Building_Cooler current))
			{
				result.Add(current);

				IntVec3 next = position + direction * ChainSpacing;
				if (!TryCoolerAt(cooler, next, out _) || !PocketSealed(cooler.Map, position + direction))
				{
					break;
				}

				position = next;
			}

			return result.Contains(cooler);
		}

		/// <summary>两台空调之间那格是否被四面封死（墙、空调或其它不通行建筑）。</summary>
		private static bool PocketSealed(Map map, IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return false;
			}

			for (int i = 0; i < 4; i++)
			{
				IntVec3 neighbour = cell + GenAdj.CardinalDirections[i];
				if (!neighbour.InBounds(map) || !neighbour.Impassable(map))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>取同朝向的空调；没有或朝向不同返回 false。</summary>
		private static bool TryCoolerAt(Building_Cooler origin, IntVec3 position, out Building_Cooler cooler)
		{
			cooler = null;
			if (!position.InBounds(origin.Map))
			{
				return false;
			}

			cooler = position.GetFirstThing<Building_Cooler>(origin.Map);
			return cooler != null && cooler.Rotation == origin.Rotation;
		}

		/// <summary>写入一条链的目标温度与理想保温腔；任一段不满足条件就整条跳过。</summary>
		private static void ApplyChain(Map map, List<Building_Cooler> stages)
		{
			IntVec3 direction = IntVec3.South.RotatedBy(stages[0].Rotation);
			IntVec3 hotCell = stages[0].Position - direction;
			float ambient = hotCell.InBounds(map) ? hotCell.GetTemperature(map) : map.mapTemperature.OutdoorTemp;
			ambient = Mathf.Min(ambient, 1000f);

			for (int stage = 0; stage < stages.Count; stage++)
			{
				Building_Cooler cooler = stages[stage];
				if (!IsWorking(cooler))
				{
					Diag($"stage{stage} not working at {cooler.Position}");
					return;
				}

				float target = Mathf.Max(FloorTemperature, ambient - StageDrop * (stage + 1));
				CompTempControl control = cooler.TryGetComp<CompTempControl>();
				if (control?.TargetTemperature > target + 1f)
				{
					Diag($"stage{stage} target {control.TargetTemperature:F1} > {target:F1}");
					return;
				}

				IntVec3 coldCell = cooler.Position + direction;
				Room coldRoom = coldCell.InBounds(map) ? coldCell.GetRoom(map) : null;
				if (coldRoom != null && coldRoom.UsesOutdoorTemperature)
				{
					Diag($"stage{stage} cold={coldCell} is outdoor (needs a roof)");
					return;
				}

				if (coldRoom == null || coldRoom.CellCount > MaxColdRoomCells)
				{
					Diag($"stage{stage} cold={coldCell} room={(coldRoom == null ? "null" : coldRoom.CellCount.ToString())} outdoor={(coldRoom != null && coldRoom.UsesOutdoorTemperature)}");
					return;
				}


				targetByRoom[coldRoom] = target;
				insulatedRooms.Add(coldRoom);

				if (stage == stages.Count - 1)
				{
					AddDoorChamber(map, coldCell, target);
				}
			}
		}

		private static bool IsWorking(Building_Cooler cooler)
		{
			if (!cooler.Spawned || cooler.Destroyed)
			{
				return false;
			}

			if (TestBypassPower)
			{
				return true;
			}

			CompPowerTrader power = cooler.TryGetComp<CompPowerTrader>();
			return power == null || power.PowerOn;
		}

		/// <summary>从链末端冷室沿门格泛洪，把整段门廊并入同一冷腔。</summary>
		private static void AddDoorChamber(Map map, IntVec3 start, float target)
		{
			frontier.Clear();
			chamberSeen.Clear();
			frontier.Add(start);
			int scanned = 0;

			while (frontier.Count > 0 && scanned < MaxChamberCells)
			{
				IntVec3 cell = frontier[frontier.Count - 1];
				frontier.RemoveAt(frontier.Count - 1);
				scanned++;

				if (!chamberSeen.Add(cell))
				{
					continue;
				}

				if (!cell.InBounds(map) || !(cell.GetEdifice(map) is Building_Door))
				{
					continue;
				}

				Room room = cell.GetRoom(map);
				if (room == null || room.CellCount > MaxColdRoomCells)
				{
					continue;
				}

				if (!targetByRoom.ContainsKey(room))
				{
					targetByRoom[room] = target;
					insulatedRooms.Add(room);
				}

				for (int i = 0; i < 4; i++)
				{
					frontier.Add(cell + GenAdj.CardinalDirections[i]);
				}
			}
		}

		private static int diagBudget = 60;

		private static void Diag(string message)
		{
			if (!TestBypassPower || diagBudget <= 0)
			{
				return;
			}

			diagBudget--;
			ModLog.Info("Insulation", "cascade diag: " + message);
		}

		private static void Clear()
		{
			builtTick = -1;
			builtMap = null;
			targetByRoom.Clear();
			insulatedRooms.Clear();
		}
	}
}



