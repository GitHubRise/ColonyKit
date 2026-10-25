using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>一次放置计算里的单个条目结果。</summary>
	public sealed class BlueprintPlacement
	{
		public BlueprintEntry entry;
		public ThingDef def;
		public ThingDef stuff;
		public IntVec3 position;
		public Rot4 rotation;
		public AcceptanceReport report;
		public string failure;
		public bool missingDef;
		public bool substituted;

		public bool CanPlace => def != null && report.Accepted;
	}

	/// <summary>
	/// 蓝图几何与放置：旋转/镜像变换、逐条校验、材料汇总、落地生成原版蓝图。
	/// </summary>
	/// <remarks>
	/// 坐标系：蓝图以捕获范围左下角为 (0,0)，尺寸 <c>sizeX × sizeZ</c>。
	/// 建筑条目存的是建筑 <c>Position</c> 相对原点的偏移；多格建筑必须用
	/// <c>GenAdj.OccupiedRect</c> 反推旋转后的新 Position，不能直接把 Position 当格子旋转。
	/// </remarks>
	public static class BlueprintPlacementUtility
	{
		/// <summary>旋转后的蓝图外框尺寸。</summary>
		public static IntVec2 RotatedSize(BlueprintRecord record, int rotation)
		{
			int steps = GenMath.PositiveMod(rotation, 4);
			return steps % 2 == 0
				? new IntVec2(record.sizeX, record.sizeZ)
				: new IntVec2(record.sizeZ, record.sizeX);
		}

		/// <summary>鼠标格 → 蓝图左下角；让蓝图大致以鼠标为中心。</summary>
		public static IntVec3 AnchorFor(IntVec3 mouse, BlueprintRecord record, int rotation)
		{
			IntVec2 size = RotatedSize(record, rotation);
			return new IntVec3(mouse.x - (size.x - 1) / 2, 0, mouse.z - (size.z - 1) / 2);
		}

		/// <summary>计算整张蓝图在指定位置/朝向/镜像下的所有条目。</summary>
		public static void Build(
			BlueprintRecord record,
			IntVec3 mouse,
			int rotation,
			bool mirrored,
			Map map,
			BlueprintMaterialMode mode,
			List<BlueprintPlacement> output,
			out bool canPlace,
			out string failure,
			out List<IntVec3> footprint)
		{
			output.Clear();
			canPlace = true;
			failure = null;
			footprint = new List<IntVec3>();

			if (record == null || map == null)
			{
				canPlace = false;
				failure = "CK.Blueprint.NoRecord".Translate();
				return;
			}

			int steps = GenMath.PositiveMod(rotation, 4);
			IntVec3 anchor = AnchorFor(mouse, record, steps);
			IntVec2 size = RotatedSize(record, steps);

			for (int x = 0; x < size.x; x++)
			{
				for (int z = 0; z < size.z; z++)
				{
					IntVec3 cell = new IntVec3(anchor.x + x, 0, anchor.z + z);
					if (cell.InBounds(map))
					{
						footprint.Add(cell);
					}
				}
			}

			foreach (BlueprintEntry entry in record.entries)
			{
				BlueprintPlacement placement = new BlueprintPlacement { entry = entry };
				output.Add(placement);

				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry.defName);
				if (!IsPlaceableDef(def))
				{
					// 缺 Mod/缺 Def：跳过这一条，不阻塞整张蓝图。
					placement.missingDef = true;
					placement.failure = "CK.Blueprint.Place.MissingDef".Translate(entry.defName);
					continue;
				}

				placement.def = def;
				ThingDef stuff = BlueprintMaterialPolicy.Resolve(def, entry.stuffDefName, mode, map);
				placement.stuff = stuff;
				placement.substituted = !entry.stuffDefName.NullOrEmpty()
					&& (stuff == null || stuff.defName != entry.stuffDefName);

				if (def.MadeFromStuff && def.CostStuffCount > 0 && stuff == null)
				{
					canPlace = false;
					failure = failure ?? "CK.Blueprint.Place.NoStuff".Translate(def.LabelCap);
					placement.failure = "CK.Blueprint.Place.NoStuff".Translate(def.LabelCap);
					continue;
				}

				if (!def.IsResearchFinished && !DebugSettings.godMode)
				{
					canPlace = false;
					failure = failure ?? "CK.Blueprint.Place.NeedsResearch".Translate(def.LabelCap);
					placement.failure = "CK.Blueprint.Place.NeedsResearch".Translate(def.LabelCap);
					continue;
				}

				Rot4 newRot = Rotated(entry.rot, steps, mirrored);
				placement.rotation = newRot;
				placement.position = anchor + TransformedOrigin(record, entry, def, steps, mirrored, newRot);

				if (!placement.position.InBounds(map))
				{
					canPlace = false;
					failure = failure ?? "CK.Blueprint.Place.OutOfBounds".Translate();
					placement.failure = "CK.Blueprint.Place.OutOfBounds".Translate();
					continue;
				}

				AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(
					def, placement.position, newRot, map, DebugSettings.godMode, null, null, stuff);
				placement.report = report;
				if (!report.Accepted)
				{
					canPlace = false;
					string reason = report.Reason.NullOrEmpty()
						? "CK.Blueprint.Place.Blocked".Translate()
						: report.Reason;
					failure = failure ?? "CK.Blueprint.Place.EntryFailed".Translate(def.LabelCap, reason);
					placement.failure = failure;
				}
			}

			if (output.Count == 0)
			{
				canPlace = false;
				failure = "CK.Blueprint.Capture.NoBuildings".Translate();
			}
		}

		private static bool IsPlaceableDef(ThingDef def)
		{
			return def != null
				&& def.category == ThingCategory.Building
				&& def.building != null
				&& def.blueprintDef != null;
		}

		private static Rot4 Rotated(int originalRot, int steps, bool mirrored)
		{
			int rot = originalRot;
			if (mirrored)
			{
				// 左右镜像：东西对调，南北不变。
				if (rot == Rot4.East.AsInt)
				{
					rot = Rot4.West.AsInt;
				}
				else if (rot == Rot4.West.AsInt)
				{
					rot = Rot4.East.AsInt;
				}
			}
			return new Rot4(GenMath.PositiveMod(rot + steps, 4));
		}

		private static IntVec3 TransformedOrigin(
			BlueprintRecord record,
			BlueprintEntry entry,
			ThingDef def,
			int steps,
			bool mirrored,
			Rot4 newRot)
		{
			CellRect oldRect = GenAdj.OccupiedRect(
				new IntVec3(entry.x, 0, entry.z),
				new Rot4(entry.rot),
				def.size);

			CellRect transformed = TransformRect(oldRect, steps, record.sizeX, record.sizeZ, mirrored);
			CellRect newOffset = GenAdj.OccupiedRect(IntVec3.Zero, newRot, def.size);
			return transformed.Min - newOffset.Min;
		}

		private static CellRect TransformRect(
			CellRect rect,
			int steps,
			int width,
			int height,
			bool mirrored)
		{
			int minX = int.MaxValue;
			int minZ = int.MaxValue;
			int maxX = int.MinValue;
			int maxZ = int.MinValue;

			foreach (IntVec3 cell in rect)
			{
				IntVec3 transformed = TransformCell(cell, steps, width, height, mirrored);
				minX = Math.Min(minX, transformed.x);
				minZ = Math.Min(minZ, transformed.z);
				maxX = Math.Max(maxX, transformed.x);
				maxZ = Math.Max(maxZ, transformed.z);
			}

			return new CellRect(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
		}

		/// <summary>把蓝图内的一格旋转/镜像到新坐标系；宽度/高度随每次 90° 旋转交换。</summary>
		private static IntVec3 TransformCell(IntVec3 cell, int steps, int width, int height, bool mirrored)
		{
			int x = cell.x;
			int z = cell.z;
			int w = width;
			int h = height;

			if (mirrored)
			{
				x = w - 1 - x;
			}

			for (int index = 0; index < steps; index++)
			{
				int rotatedX = z;
				int rotatedZ = w - 1 - x;
				x = rotatedX;
				z = rotatedZ;

				int temp = w;
				w = h;
				h = temp;
			}

			return new IntVec3(x, 0, z);
		}

		/// <summary>整张蓝图的材料总需求（已按策略替换材质）。</summary>
		public static List<ThingDefCountClass> TotalCost(List<BlueprintPlacement> placements)
		{
			List<ThingDefCountClass> totals = new List<ThingDefCountClass>();
			foreach (BlueprintPlacement placement in placements)
			{
				if (placement.def == null)
				{
					continue;
				}
				AddCost(totals, placement.def, placement.stuff);
			}
			SortCosts(totals);
			return totals;
		}

		/// <summary>库 UI 用：不校验位置，只按策略汇总材料和替换项。</summary>
		public static List<ThingDefCountClass> CostSummary(
			BlueprintRecord record,
			Map map,
			BlueprintMaterialMode mode,
			List<BlueprintSubstitution> substitutions)
		{
			List<ThingDefCountClass> totals = new List<ThingDefCountClass>();
			if (record == null)
			{
				return totals;
			}

			foreach (BlueprintEntry entry in record.entries)
			{
				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry.defName);
				if (def == null)
				{
					continue;
				}

				ThingDef stuff = BlueprintMaterialPolicy.Resolve(def, entry.stuffDefName, mode, map);
				AddCost(totals, def, stuff);

				if (substitutions != null
					&& !entry.stuffDefName.NullOrEmpty()
					&& stuff != null
					&& stuff.defName != entry.stuffDefName)
				{
					ThingDef source = DefDatabase<ThingDef>.GetNamedSilentFail(entry.stuffDefName);
					if (source != null
						&& !substitutions.Any(existing => existing.source == source && existing.target == stuff))
					{
						substitutions.Add(new BlueprintSubstitution { source = source, target = stuff });
					}
				}
			}

			SortCosts(totals);
			return totals;
		}

		/// <summary>把校验通过的条目逐条变成原版蓝图。</summary>
		public static int PlaceAll(List<BlueprintPlacement> placements, Map map, out int skipped, out int substituted)
		{
			int placed = 0;
			skipped = 0;
			substituted = 0;

			foreach (BlueprintPlacement placement in placements)
			{
				if (placement.def == null)
				{
					skipped++;
					continue;
				}
				if (!placement.report.Accepted)
				{
					continue;
				}

				if (placement.substituted)
				{
					substituted++;
				}

				// 与原版建造设计器一致：允许替换（replaceTags）时先清掉旧建筑/框架。
				GenSpawn.WipeExistingThings(
					placement.position,
					placement.rotation,
					placement.def.blueprintDef,
					map,
					DestroyMode.Deconstruct);

				// God mode skips the build job entirely: spawn the finished building.
				if (DebugSettings.godMode)
				{
					Thing thing = ThingMaker.MakeThing(placement.def, placement.stuff);
					if (thing.def.CanHaveFaction)
					{
						thing.SetFactionDirect(Faction.OfPlayer);
					}
					GenSpawn.Spawn(thing, placement.position, map, placement.rotation);
					placed++;
					continue;
				}

				GenConstruct.PlaceBlueprintForBuild(
					placement.def,
					placement.position,
					map,
					placement.rotation,
					Faction.OfPlayer,
					placement.stuff);
				placed++;
			}

			return placed;
		}

		public static void AddCost(List<ThingDefCountClass> totals, ThingDef def, ThingDef stuff)
		{
			if (def == null)
			{
				return;
			}

			if (def.MadeFromStuff && def.CostStuffCount > 0 && stuff != null)
			{
				AddOrMerge(totals, stuff, def.CostStuffCount);
			}

			List<ThingDefCountClass> costList = def.CostList;
			if (costList == null)
			{
				return;
			}

			foreach (ThingDefCountClass cost in costList)
			{
				if (cost?.thingDef != null)
				{
					AddOrMerge(totals, cost.thingDef, cost.count);
				}
			}
		}

		private static void AddOrMerge(List<ThingDefCountClass> totals, ThingDef thingDef, int count)
		{
			if (thingDef == null || count <= 0)
			{
				return;
			}

			for (int index = 0; index < totals.Count; index++)
			{
				if (totals[index].thingDef == thingDef)
				{
					totals[index].count += count;
					return;
				}
			}

			totals.Add(new ThingDefCountClass(thingDef, count));
		}

		private static void SortCosts(List<ThingDefCountClass> totals)
		{
			totals.Sort((left, right) => right.count.CompareTo(left.count));
		}
	}
}
