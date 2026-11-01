using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene.GroundWater
{
	/// <summary>
	/// 地下水文自检：带 <c>-colonykit-groundwater-test</c> 启动时，检查
	/// 1) 网格真的生成了（有富格也有贫格）、2) 没水的地方放不下井、
	/// 3) 井的产量跟着水脉走、4) 两口井压在同一个水脉上会分水。
	/// </summary>
	internal static class GroundWaterSelftest
	{
		public const string CommandLineKey = "-colonykit-groundwater-test";

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
				ModLog.Error("Hygiene", "groundwater selftest crashed: " + exception);
				Root.Shutdown();
			}
		}

		private static void Run(Game game)
		{
			Map map = Find.AnyPlayerHomeMap ?? game.Maps.FirstOrDefault();
			if (map == null)
			{
				Log("no map", false);
				return;
			}
			MapComponent_GroundWater groundWater = map.GetComponent<MapComponent_GroundWater>();
			if (groundWater == null)
			{
				Log("no map component", false);
				return;
			}
			groundWater.EnsureGenerated();
			if (groundWater.Water == null || groundWater.Deep == null)
			{
				Log("no grids", false);
				return;
			}

			// 设置被别人关过时先打开，测完还原。
			HygieneSettings settings = HygieneSettings.Current;
			bool settingBefore = settings.groundWaterRequired;

			int wet = 0;
			int dry = 0;
			float minValue = float.MaxValue;
			float maxValue = 0f;
			foreach (IntVec3 cell in map.AllCells)
			{
				float value = groundWater.Water.ValueAt(cell);
				if (value > 0.5f)
				{
					wet++;
				}
				else
				{
					dry++;
				}
				if (value < minValue)
				{
					minValue = value;
				}
				if (value > maxValue)
				{
					maxValue = value;
				}
			}
			minValue = minValue == float.MaxValue ? 0f : minValue;

			ThingDef wellDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_DbhWaterWell");
			if (wellDef == null)
			{
				Log("no well def", false);
				return;
			}

			// 富水格：足够高、没雾、没有建筑。
			IntVec3 wetCell = IntVec3.Invalid;
			foreach (IntVec3 cell in map.AllCells)
			{
				if (groundWater.Water.ValueAt(cell) >= 40f
					&& !cell.Fogged(map)
					&& cell.GetEdifice(map) == null
					&& !cell.GetThingList(map).Any(t => t is Pawn))
				{
					wetCell = cell;
					break;
				}
			}
			if (!wetCell.IsValid)
			{
				Log("no dry land wet cell", false);
				return;
			}

			settings.groundWaterRequired = true;
			try
			{
				var placeWorker = new PlaceWorker_GroundWater();
				bool placeWet = placeWorker.AllowsPlacing(wellDef, wetCell, Rot4.North, map).Accepted;

				// 把这一格的网格临时压成 0，验证「没水不能放井」；测完还原。
				float saved = groundWater.Water.ValueAt(wetCell);
				groundWater.SetWaterForTest(wetCell, 0f);
				bool placeDry = placeWorker.AllowsPlacing(wellDef, wetCell, Rot4.North, map).Accepted;
				groundWater.SetWaterForTest(wetCell, saved);

				Thing wellA = ThingMaker.MakeThing(wellDef);
				GenSpawn.Spawn(wellA, wetCell, map, Rot4.North);
				CompWaterInlet inletA = wellA.TryGetComp<CompWaterInlet>();
				float capA = inletA?.GroundWaterCapacity ?? -1f;
				float richnessA = inletA?.Richness ?? -1f;

				// 第二口井建在相邻格：覆盖范围重叠，同一条水脉要分水。
				Thing wellB = null;
				float capA2 = capA;
				float capB = -1f;
				foreach (IntVec3 neighbor in GenAdj.CardinalDirections)
				{
					IntVec3 cell = wetCell + neighbor;
					if (!cell.InBounds(map) || cell.Fogged(map) || cell.GetEdifice(map) != null)
					{
						continue;
					}
					wellB = ThingMaker.MakeThing(wellDef);
					GenSpawn.Spawn(wellB, cell, map, Rot4.North);
					CompWaterInlet inletB = wellB.TryGetComp<CompWaterInlet>();
					capA2 = inletA?.GroundWaterCapacity ?? -1f;
					capB = inletB?.GroundWaterCapacity ?? -1f;
					break;
				}

				bool pass = wet > 0
					&& maxValue - minValue > 5f
					&& placeWet
					&& !placeDry
					&& capA > 0f
					&& richnessA >= MapComponent_GroundWater.MinRichness - 0.01f
					&& richnessA <= MapComponent_GroundWater.MaxRichness + 0.01f
					&& (wellB == null || capA2 < capA * 0.95f);

				ModLog.Info(
					"Hygiene",
					$"groundwater selftest: pass={pass} wet={wet} dry={dry} " +
					$"min={minValue:F0} max={maxValue:F0} placeWet={placeWet} placeDry={placeDry} " +
					$"capA={capA:F0} richnessA={richnessA:F2} capA2={capA2:F0} capB={capB:F0}");

				if (wellA != null)
				{
					wellA.Destroy(DestroyMode.Vanish);
				}
				if (wellB != null)
				{
					wellB.Destroy(DestroyMode.Vanish);
				}
			}
			finally
			{
				settings.groundWaterRequired = settingBefore;
			}
			Root.Shutdown();
		}

		private static void Log(string reason, bool pass)
		{
			ModLog.Info("Hygiene", $"groundwater selftest: pass={pass} reason={reason}");
			Root.Shutdown();
		}
	}
}
