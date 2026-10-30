using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ColonyKit.Modules.Blueprint;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 级联冷室自检：带 <c>-colonykit-cascade-test</c> 启动时，在快速测试地图上搭一条
	/// 「三台同向空调 + 封闭小室 + 门廊」的最小级联，跑一段时间把各段温度写进日志。
	/// </summary>
	/// <remarks>
	/// 用法：<c>RimWorldWin64.exe -quicktest -colonykit-cascade-test -logfile -</c>
	/// 只验证识别与结算逻辑（跳过通电判定），不验证真实电网。
	/// </remarks>
	internal static class CascadeSelftest
	{
		public const string CommandLineKey = "-colonykit-cascade-test";

		private const int RunTicks = 40000;
		private const int LogInterval = 10000;

		private static bool ran;

		public static void RunIfRequested(Game game)
		{
			if (ran || game == null || !GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				return;
			}

			ran = true;
			try
			{
				Run(game);
			}
			catch (System.Exception exception)
			{
				ModLog.Error("Insulation", "cascade selftest crashed: " + exception);
			}
		}

		private static void Run(Game game)
		{
			Map map = Find.AnyPlayerHomeMap ?? (game.Maps.Count > 0 ? game.Maps[0] : null);
			if (map == null)
			{
				ModLog.Warn("Insulation", "cascade selftest skipped: no map.");
				return;
			}

			if (!TryFindClearRect(map, 14, 3, out IntVec3 origin))
			{
				ModLog.Warn("Insulation", "cascade selftest skipped: no clear 14x3 area.");
				return;
			}

			CascadeFreeze.TestBypassPower = true;

			if (TryBuildBlueprintRig(map))
			{
				ArmedMap = map;
				ArmedStartTick = Find.TickManager.TicksGame;
				Armed = true;
				Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
				ModLog.Info("Insulation", $"cascade selftest blueprint rig built at {ArmedOrigin}; waiting {RunTicks} ticks");
				return;
			}

			ClearRect(map, origin, 14, 3);

			// 冷却端（热侧）在 origin，向东依次是：空调 / 小室 / 空调 / 小室 / 空调 / 门廊 / 封头。
			BuildWalls(map, origin.x, origin.x + 12, origin.z - 1);
			BuildWalls(map, origin.x, origin.x + 12, origin.z + 1);

			for (int i = 0; i < 3; i++)
			{
				IntVec3 cell = new IntVec3(origin.x + 1 + i * 2, 0, origin.z);
				Thing cooler = ThingMaker.MakeThing(ThingDefOf.Cooler);
				GenSpawn.Spawn(cooler, cell, map, Rot4.West);
				cooler.SetFaction(Faction.OfPlayer);
				cooler.TryGetComp<CompTempControl>().TargetTemperature = CascadeFreeze.FloorTemperature;
			}

			for (int i = 0; i < 6; i++)
			{
				IntVec3 cell = new IntVec3(origin.x + 6 + i, 0, origin.z);
				Thing door = ThingMaker.MakeThing(ThingDefOf.Door, ThingDefOf.BlocksGranite);
				GenSpawn.Spawn(door, cell, map, Rot4.North);
				door.SetFaction(Faction.OfPlayer);
			}

			Thing cap = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.BlocksGranite);
			GenSpawn.Spawn(cap, new IntVec3(origin.x + 12, 0, origin.z), map);

			// 冷室必须有顶：无顶的 1 格房间会被判成「室外」，温度每 120 tick 被重置回室外温度。
			CellRect roofed = new CellRect(origin.x, origin.z - 1, 13, 3);
			foreach (IntVec3 cell in roofed.Cells)
			{
				if (cell.InBounds(map))
				{
					map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
				}
			}

			ArmedOrigin = origin;
			ArmedStartTick = Find.TickManager.TicksGame;
			ArmedMap = map;
			Armed = true;
			Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
			ModLog.Info("Insulation", $"cascade selftest rig built at {origin}; waiting {RunTicks} ticks");
		}

		/// <summary>
		/// 用内置「低版本永恒温室」模板搭出完整结构（只放墙 / 门 / 空调），给冷室加顶，
		/// 然后把四段门廊的探针位置记下来。模板缺失或找不到空地时返回 false。
		/// </summary>
		private static bool TryBuildBlueprintRig(Map map)
		{
			string root = ColonyKitMod.Instance?.Content?.RootDir;
			if (root.NullOrEmpty())
			{
				return false;
			}

			string path = Path.Combine(root, "Blueprints", "低版本永恒温室.xml");
			if (!File.Exists(path))
			{
				return false;
			}

			BlueprintTemplate template = BlueprintTemplateStorage.Load(path, true);
			if (template.record == null || template.record.entries.Count == 0)
			{
				return false;
			}

			int width = template.record.sizeX;
			int height = template.record.sizeZ;
			if (!TryFindClearRect(map, width, height, out IntVec3 origin))
			{
				return false;
			}

			ClearRect(map, origin, width, height);
			CellRect rect = new CellRect(origin.x, origin.z, width, height);
			foreach (IntVec3 cell in rect.Cells)
			{
				if (cell.InBounds(map))
				{
					map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
				}
			}

			foreach (BlueprintEntry entry in template.record.entries)
			{
				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry.defName);
				if (def == null || (def != ThingDefOf.Wall && def != ThingDefOf.Door && def != ThingDefOf.Cooler))
				{
					continue;
				}

				ThingDef stuff = entry.stuffDefName.NullOrEmpty()
					? null
					: DefDatabase<ThingDef>.GetNamedSilentFail(entry.stuffDefName);
				Thing thing = ThingMaker.MakeThing(def, stuff);
				IntVec3 cell = origin + new IntVec3(entry.x, 0, entry.z);
				GenSpawn.Spawn(thing, cell, map, new Rot4(entry.rot));
				thing.SetFaction(Faction.OfPlayer);
				if (def == ThingDefOf.Cooler)
				{
					thing.TryGetComp<CompTempControl>().TargetTemperature = CascadeFreeze.FloorTemperature;
				}
			}

			// 冷室必须有顶，否则会被判成室外房间、温度每 120 tick 重置。
			foreach (IntVec3 cell in rect.Cells)
			{
				if (cell.InBounds(map))
				{
					map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
				}
			}

			probes = new[]
			{
				origin + new IntVec3(13, 0, 20),
				origin + new IntVec3(25, 0, 20),
				origin + new IntVec3(20, 0, 13),
				origin + new IntVec3(20, 0, 25)
			};
			ArmedOrigin = origin;
			return true;
		}

		private static void BuildWalls(Map map, int fromX, int toX, int z)		{
			for (int x = fromX; x <= toX; x++)
			{
				Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.BlocksGranite);
				GenSpawn.Spawn(wall, new IntVec3(x, 0, z), map);
			}
		}

		/// <summary>找一块 14×3 的平整空地：地形可承重、无顶、无建筑、无迷雾；草木稍后清掉。</summary>
		private static bool TryFindClearRect(Map map, int width, int height, out IntVec3 origin)
		{
			for (int z = 2; z < map.Size.z - height - 2; z++)
			{
				for (int x = 2; x < map.Size.x - width - 2; x++)
				{
					bool free = true;
					for (int dz = 0; dz < height && free; dz++)
					{
						for (int dx = 0; dx < width && free; dx++)
						{
							IntVec3 cell = new IntVec3(x + dx, 0, z + dz);
							if (!cell.InBounds(map) || cell.GetEdifice(map) != null || cell.Fogged(map))
							{
								free = false;
							}
						}
					}

					if (free)
					{
						origin = new IntVec3(x, 0, z);
						return true;
					}
				}
			}

			origin = IntVec3.Invalid;
			return false;
		}

		/// <summary>清掉测试用地上的草木/杂物，避免挡住施工。</summary>
		private static void ClearRect(Map map, IntVec3 origin, int width, int height)
		{
			CellRect rect = new CellRect(origin.x, origin.z, width, height);
			foreach (IntVec3 cell in rect.Cells)
			{
				List<Thing> things = map.thingGrid.ThingsListAt(cell);
				for (int i = things.Count - 1; i >= 0; i--)
				{
					Thing thing = things[i];
					if (thing != null && !thing.Destroyed)
					{
						thing.Destroy(DestroyMode.Vanish);
					}
				}
			}
		}
		private static bool Armed;
		private static Map ArmedMap;
		private static IntVec3 ArmedOrigin;
		private static IntVec3[] probes;
		private static int ArmedStartTick;
		private static int nextLogTick = LogInterval;
		private static int pumpFrames;

		/// <summary>
		/// 每帧调用（挂 <c>Game.Update</c>）：quicktest 容易停在强制暂停窗口，这里把时间推起来，
		/// 并周期性把暂停原因写进日志，方便排查自检为什么不动。
		/// </summary>
		public static void Pump()
		{
			if (!Armed)
			{
				return;
			}

			pumpFrames++;
			TickManager ticks = Find.TickManager;
			if (ticks == null)
			{
				return;
			}

			if (ticks.CurTimeSpeed != TimeSpeed.Ultrafast)
			{
				ticks.CurTimeSpeed = TimeSpeed.Ultrafast;
			}

			if (Find.WindowStack != null && Find.WindowStack.WindowsForcePause)
			{
				IList<Window> windows = Find.WindowStack.Windows;
				for (int i = windows.Count - 1; i >= 0; i--)
				{
					Window window = windows[i];
					if (window != null && window.forcePause)
					{
						Find.WindowStack.TryRemove(window, false);
					}
				}
			}

			if (pumpFrames % 600 == 0)
			{
				ModLog.Info("Insulation",
					$"cascade pump: ticks={ticks.TicksGame} speed={ticks.CurTimeSpeed} " +
					$"forced={ticks.ForcePaused} longEvent={LongEventHandler.ForcePause}");
			}
		}

		/// <summary>由 <c>MapPostTick</c> 补丁每 250 tick 调用：加速时间、记录温度、结束判定。</summary>
		public static void DriveIfArmed(Map map)
		{
			if (!Armed || map == null || map != ArmedMap)
			{
				return;
			}

			Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;

			int elapsed = Find.TickManager.TicksGame - ArmedStartTick;
			if (elapsed <= 0)
			{
				return;
			}

			if (elapsed >= nextLogTick)
			{
				nextLogTick += LogInterval;
				ModLog.Info("Insulation", "cascade " + Snapshot(map));
			}

			if (elapsed < RunTicks)
			{
				return;
			}

			Armed = false;
			float ambient = ArmedOrigin.GetTemperature(map);
			bool pass;
			string detail;
			if (probes != null && probes.Length > 0)
			{
				pass = true;
				StringBuilder summary = new StringBuilder("probes=");
				foreach (IntVec3 cell in probes)
				{
					float temp = cell.GetTemperature(map);
					summary.Append('[').Append(cell).Append(':').Append(temp.ToString("F1")).Append(']');
					if (temp > -271f)
					{
						pass = false;
					}
				}

				detail = summary.ToString();
			}
			else
			{
				float pocketB = (ArmedOrigin + new IntVec3(4, 0, 0)).GetTemperature(map);
				float doorLast = (ArmedOrigin + new IntVec3(11, 0, 0)).GetTemperature(map);
				pass = doorLast <= -271f && pocketB <= ambient - 200f;
				detail = $"pocketB={pocketB:F1} doorLast={doorLast:F1}";
			}

			ModLog.Info("Insulation", $"cascade selftest: ambient={ambient:F1} {detail} pass={pass}");
			Root.Shutdown();
		}

		private static string Snapshot(Map map)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("tick=").Append(Find.TickManager.TicksGame - ArmedStartTick);
			builder.Append(" ambient=").Append(ArmedOrigin.GetTemperature(map).ToString("F1"));

			if (probes != null && probes.Length > 0)
			{
				foreach (IntVec3 cell in probes)
				{
					Room probeRoom = cell.GetRoom(map);
					string probeTarget = probeRoom != null && CascadeFreeze.TryGetTarget(probeRoom, out float probeValue)
						? probeValue.ToString("F0")
						: "none";
					builder.Append(' ').Append(cell).Append('=')
						.Append(cell.GetTemperature(map).ToString("F1"))
						.Append("/").Append(probeTarget);
				}

				return builder.ToString();
			}

			builder.Append(" pocketA=").Append((ArmedOrigin + new IntVec3(2, 0, 0)).GetTemperature(map).ToString("F1"));
			builder.Append(" pocketB=").Append((ArmedOrigin + new IntVec3(4, 0, 0)).GetTemperature(map).ToString("F1"));
			builder.Append(" doorFirst=").Append((ArmedOrigin + new IntVec3(6, 0, 0)).GetTemperature(map).ToString("F1"));
			builder.Append(" doorLast=").Append((ArmedOrigin + new IntVec3(11, 0, 0)).GetTemperature(map).ToString("F1"));
			builder.Append(" doors=");
			for (int i = 6; i <= 11; i++)
			{
				IntVec3 cell = ArmedOrigin + new IntVec3(i, 0, 0);
				Room room = cell.GetRoom(map);
				string target = room != null && CascadeFreeze.TryGetTarget(room, out float value)
					? value.ToString("F0")
					: "none";
				builder.Append('[').Append(cell.x).Append(':')
					.Append(cell.GetTemperature(map).ToString("F0")).Append('/')
					.Append(target).Append(']');
			}
			return builder.ToString();
		}
	}
}



