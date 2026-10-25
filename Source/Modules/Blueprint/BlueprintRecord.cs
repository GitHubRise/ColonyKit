using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>蓝图里的一个建筑条目：相对坐标 + Def + 材质 + 朝向。</summary>
	/// <remarks>
	/// 只存 <c>defName</c>/<c>stuffDefName</c>，不存 Def 引用：模板要跨存档、跨 Mod 列表使用，
	/// 读档时缺 Mod 只会让这一条失效，不会让整个库反序列化失败。
	/// </remarks>
	public class BlueprintEntry : IExposable
	{
		public string defName;
		public string stuffDefName;
		public int rot;
		public int x;
		public int z;

		public void ExposeData()
		{
			Scribe_Values.Look(ref defName, "ck_blueprint_def");
			Scribe_Values.Look(ref stuffDefName, "ck_blueprint_stuff");
			Scribe_Values.Look(ref rot, "ck_blueprint_rot", 0);
			Scribe_Values.Look(ref x, "ck_blueprint_x", 0);
			Scribe_Values.Look(ref z, "ck_blueprint_z", 0);
		}

		public BlueprintEntry Clone()
		{
			return new BlueprintEntry
			{
				defName = defName,
				stuffDefName = stuffDefName,
				rot = rot,
				x = x,
				z = z
			};
		}
	}

	/// <summary>
	/// 一张蓝图：名称、尺寸、建筑条目。坐标系以捕获范围的左下角为原点，
	/// 每个条目的 <see cref="BlueprintEntry.x"/>/<see cref="BlueprintEntry.z"/> 是建筑
	/// <c>Position</c> 相对原点的偏移。
	/// </summary>
	public class BlueprintRecord : IExposable, IRenameable
	{
		public const int CurrentVersion = 1;

		/// <summary>存档内唯一编号；模板读入时由库重新分配。</summary>
		public int id;

		/// <summary>数据版本；将来改字段时用于迁移。</summary>
		public int version = CurrentVersion;

		public string name;

		/// <summary>捕获范围宽度（格）。</summary>
		public int sizeX = 1;

		/// <summary>捕获范围高度（格）。</summary>
		public int sizeZ = 1;

		/// <summary>捕获时的游戏 tick，仅用于显示/排序。</summary>
		public int createdTick;

		public List<BlueprintEntry> entries = new List<BlueprintEntry>();

		public string RenamableLabel
		{
			get => name;
			set => name = value;
		}

		public string BaseLabel => name;

		public string InspectLabel => name;

		/// <summary>条目数（不含缺失 Def 的条目；UI 用）。</summary>
		public int EntryCount => entries?.Count ?? 0;

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "ck_blueprint_id", 0);
			Scribe_Values.Look(ref version, "ck_blueprint_version", CurrentVersion);
			Scribe_Values.Look(ref name, "ck_blueprint_name");
			Scribe_Values.Look(ref sizeX, "ck_blueprint_sizeX", 1);
			Scribe_Values.Look(ref sizeZ, "ck_blueprint_sizeZ", 1);
			Scribe_Values.Look(ref createdTick, "ck_blueprint_createdTick", 0);
			Scribe_Collections.Look(ref entries, "ck_blueprint_entries", LookMode.Deep);

			if (entries == null)
			{
				entries = new List<BlueprintEntry>();
			}

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				Normalize();
			}
		}

		/// <summary>读档/导入后修正非法值；不会补回缺失建筑。</summary>
		public void Normalize()
		{
			if (name.NullOrEmpty())
			{
				name = "CK.Blueprint.DefaultName".Translate(0);
			}

			if (entries == null)
			{
				entries = new List<BlueprintEntry>();
			}

			entries.RemoveAll(entry => entry == null || entry.defName.NullOrEmpty());

			if (sizeX < 1 || sizeZ < 1)
			{
				// 旧数据/手改模板没写尺寸时，用条目的占用范围反推。
				int maxX = 0;
				int maxZ = 0;
				foreach (BlueprintEntry entry in entries)
				{
					maxX = Math.Max(maxX, entry.x);
					maxZ = Math.Max(maxZ, entry.z);
				}
				sizeX = Math.Max(1, maxX + 1);
				sizeZ = Math.Max(1, maxZ + 1);
			}

			if (version < CurrentVersion)
			{
				version = CurrentVersion;
			}
		}

		public BlueprintRecord Clone()
		{
			BlueprintRecord copy = new BlueprintRecord
			{
				id = 0,
				version = version,
				name = name,
				sizeX = sizeX,
				sizeZ = sizeZ,
				createdTick = createdTick,
				entries = new List<BlueprintEntry>(entries?.Count ?? 0)
			};

			if (entries != null)
			{
				foreach (BlueprintEntry entry in entries)
				{
					copy.entries.Add(entry.Clone());
				}
			}
			return copy;
		}

		/// <summary>
		/// 从地图上的矩形范围捕获玩家建筑。
		/// </summary>
		/// <returns>成功返回记录；失败返回 null，并在 <paramref name="error"/> 里给出可读原因。</returns>
		public static BlueprintRecord Capture(
			Map map,
			CellRect rect,
			string name,
			int maxSide,
			int maxEntries,
			out string error)
		{
			error = null;
			if (map == null || rect.IsEmpty)
			{
				error = "CK.Blueprint.Capture.Empty".Translate();
				return null;
			}

			if (rect.Width > maxSide || rect.Height > maxSide)
			{
				error = "CK.Blueprint.Capture.TooBig".Translate(rect.Width, rect.Height, maxSide);
				return null;
			}

			HashSet<Thing> seen = new HashSet<Thing>();
			List<Building> buildings = new List<Building>();
			CellRect bounds = CellRect.Empty;
			bool hasBounds = false;

			foreach (IntVec3 cell in rect)
			{
				if (!cell.InBounds(map) || cell.Fogged(map))
				{
					continue;
				}

				foreach (Thing thing in cell.GetThingList(map))
				{
					if (!(thing is Building building) || !IsCapturableBuilding(building) || seen.Contains(building))
					{
						continue;
					}

					seen.Add(building);
					buildings.Add(building);
					CellRect occupied = building.OccupiedRect();
					bounds = hasBounds ? bounds.Encapsulate(occupied) : occupied;
					hasBounds = true;
				}
			}

			if (buildings.Count == 0)
			{
				error = "CK.Blueprint.Capture.NoBuildings".Translate();
				return null;
			}

			if (buildings.Count > maxEntries)
			{
				error = "CK.Blueprint.Capture.TooMany".Translate(buildings.Count, maxEntries);
				return null;
			}

			BlueprintRecord record = new BlueprintRecord
			{
				name = name,
				sizeX = bounds.Width,
				sizeZ = bounds.Height,
				createdTick = Find.TickManager?.TicksGame ?? 0,
				entries = new List<BlueprintEntry>(buildings.Count)
			};

			// 按位置排序，保证同一块区域每次捕获的条目顺序稳定（便于对比/测试）。
			buildings.Sort((left, right) =>
			{
				int compare = left.Position.z.CompareTo(right.Position.z);
				return compare != 0 ? compare : left.Position.x.CompareTo(right.Position.x);
			});

			foreach (Building building in buildings)
			{
				record.entries.Add(new BlueprintEntry
				{
					defName = building.def.defName,
					stuffDefName = building.Stuff?.defName,
					rot = building.Rotation.AsInt,
					x = building.Position.x - bounds.minX,
					z = building.Position.z - bounds.minZ
				});
			}

			if (record.name.NullOrEmpty())
			{
				record.name = "CK.Blueprint.DefaultName".Translate(0);
			}
			return record;
		}

		/// <summary>只收玩家建造的、已经完工的建筑；蓝图/框架/矿物/物品都不收。</summary>
		public static bool IsCapturableBuilding(Building building)
		{
			if (building == null)
			{
				return false;
			}

			ThingDef def = building.def;
			if (def == null || def.building == null || def.category != ThingCategory.Building)
			{
				return false;
			}

			if (def.IsBlueprint || def.IsFrame || def.mineable || def.building.isNaturalRock)
			{
				return false;
			}

			// 玩家阵营：排除地图边缘的遗迹、敌人的沙袋、商队货箱等。
			return building.Faction == Faction.OfPlayer;
		}
	}
}
