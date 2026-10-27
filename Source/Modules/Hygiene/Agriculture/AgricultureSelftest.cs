using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>
	/// 施肥/灌溉自检：只在带 -colonykit-agriculture-test 启动时跑一次，
	/// 验证 FertilityGrid 补丁、肥力网格、堆肥厂与洒水器的 Def 是否真的接上。
	/// 用法：RimWorldWin64.exe -quicktest -colonykit-agriculture-test -logfile -
	/// </summary>
	internal static class AgricultureSelftest
	{
		public const string CommandLineKey = "-colonykit-agriculture-test";

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
			catch (System.Exception ex)
			{
				ModLog.Error("Hygiene", "agriculture selftest crashed: " + ex);
			}
		}

		private static void Run(Game game)
		{
			Map map = Find.AnyPlayerHomeMap ?? (game.Maps.Count > 0 ? game.Maps[0] : null);
			if (map == null)
			{
				ModLog.Warn("Hygiene", "agriculture selftest skipped: no map.");
				return;
			}
			MapComponent_Agriculture comp = MapComponent_Agriculture.Get(map);
			if (comp == null)
			{
				ModLog.Error("Hygiene", "agriculture selftest failed: map component missing.");
				return;
			}

			IntVec3 cell = FindFertileCell(map);
			float before = map.fertilityGrid.FertilityAt(cell);
			comp.AddFertilizer(cell);
			float fertilized = map.fertilityGrid.FertilityAt(cell);
			comp.AddIrrigation(cell, 100f);
			float irrigated = map.fertilityGrid.FertilityAt(cell);

			ModLog.Info("Hygiene",
				$"agriculture selftest: cell={cell} terrain={cell.GetTerrain(map).defName} " +
				$"fertility before={before:F3} afterFertilizer={fertilized:F3} afterIrrigation={irrigated:F3}; " +
				$"settings strength={HygieneSettings.Current.fertilizerStrength:F2} " +
				$"irrigation={HygieneSettings.Current.irrigationStrength:F2} " +
				$"lifespan={HygieneSettings.Current.fertilizerLifespanDays:F0}d");

			// 覆盖层：确认 section 能拿到我们的 layer，并且网格能重建（顶点/索引写错会在这里炸）。
			// 刚生成地图时 MapDrawer 还没建 section，SectionAt 会 NRE，那就交给 tick 里的 FlushOverlay 验证。
			try
			{
				Section section = map.mapDrawer.SectionAt(cell);
				SectionLayer layer = section?.GetLayer(typeof(SectionLayer_Agriculture));
				layer?.Regenerate();
				ModLog.Info("Hygiene",
					$"agriculture selftest: overlay section={section != null} " +
					$"layer={layer?.GetType().Name ?? "null"} visible={layer?.Visible}");
			}
			catch (System.Exception ex)
			{
				ModLog.Info("Hygiene", "agriculture selftest: overlay deferred to tick: " + ex.GetType().Name);
			}

			// 堆肥厂：塞 100 个粪渣，看容量与进度有没有动。
			ThingDef composterDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_BiosolidsComposter");
			ThingDef sludgeDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_FecalSludge");
			if (composterDef != null && sludgeDef != null)
			{
				IntVec3 spot = CellFinder.RandomClosewalkCellNear(map.Center, map, 8, c => c.Standable(map));
				Building_Composter composter = ThingMaker.MakeThing(composterDef) as Building_Composter;
				if (composter != null)
				{
					GenSpawn.Spawn(composter, spot, map, Rot4.North);
					Thing sludge = ThingMaker.MakeThing(sludgeDef);
					sludge.stackCount = 100;
					composter.AddMaterial(sludge);
					ModLog.Info("Hygiene",
						$"agriculture selftest: composter spaceLeft={composter.SpaceLeft} " +
						$"fermented={composter.Fermented} inspect=\"{composter.GetInspectString().Replace('\n', '|')}\"");
				}
			}
			else
			{
				ModLog.Error("Hygiene",
					$"agriculture selftest failed: composter={composterDef != null} sludge={sludgeDef != null}");
			}

			// 洒水器：Def + comp 是否齐。
			ThingDef sprinklerDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_IrrigationSprinkler");
			if (sprinklerDef != null)
			{
				IntVec3 spot = CellFinder.RandomClosewalkCellNear(map.Center, map, 8, c => c.Standable(map));
				Thing sprinkler = ThingMaker.MakeThing(sprinklerDef);
				GenSpawn.Spawn(sprinkler, spot, map, Rot4.North);
				CompSprinkler sprinklerComp = sprinkler.TryGetComp<CompSprinkler>();
				ModLog.Info("Hygiene",
					$"agriculture selftest: sprinkler comp={sprinklerComp != null} " +
					$"radius={(sprinklerComp?.radius ?? -1f):F1} usage={(sprinklerComp?.Usage ?? -1f):F1} " +
					$"water={sprinklerComp?.HasWater}");
			}
			else
			{
				ModLog.Error("Hygiene", "agriculture selftest failed: sprinkler def missing.");
			}

			ModLog.Info("Hygiene",
				$"agriculture selftest: defs biosolids={DefDatabase<ThingDef>.GetNamedSilentFail("CK_Biosolids") != null} " +
				$"fertilizeDesignation={DefDatabase<DesignationDef>.GetNamedSilentFail("CK_Fertilize") != null} " +
				$"placeJob={DefDatabase<JobDef>.GetNamedSilentFail("CK_PlaceFertilizer") != null} " +
				$"research=compost:{DefDatabase<ResearchProjectDef>.GetNamedSilentFail("CK_SewageSludgeComposting") != null}" +
				$"/irrigation:{DefDatabase<ResearchProjectDef>.GetNamedSilentFail("CK_Irrigation") != null}");
		}

		private static IntVec3 FindFertileCell(Map map)
		{
			foreach (IntVec3 cell in map.AllCells)
			{
				TerrainDef terrain = cell.GetTerrain(map);
				if (terrain != null && terrain.fertility > 0f && cell.Standable(map))
				{
					return cell;
				}
			}
			return map.Center;
		}
	}
}
