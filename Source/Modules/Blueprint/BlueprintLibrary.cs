using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>每存档一份的蓝图库。</summary>
	/// <remarks>
	/// 库随存档保存；模板是另一条路径（<see cref="BlueprintTemplateStorage"/>），
	/// 不进存档，避免把玩家全局收藏复制进每一份存档。
	/// </remarks>
	public class BlueprintLibrary : IExposable
	{
		public const int CurrentSchema = 1;

		public int schemaVersion = CurrentSchema;

		private int nextId = 1;

		public List<BlueprintRecord> records = new List<BlueprintRecord>();

		public IReadOnlyList<BlueprintRecord> Records => records;

		public void ExposeData()
		{
			Scribe_Values.Look(ref schemaVersion, "ck_blueprint_schema", CurrentSchema);
			Scribe_Values.Look(ref nextId, "ck_blueprint_nextId", 1);
			Scribe_Collections.Look(ref records, "ck_blueprint_records", LookMode.Deep);

			if (records == null)
			{
				records = new List<BlueprintRecord>();
			}

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				records.RemoveAll(record => record == null);
				foreach (BlueprintRecord record in records)
				{
					record.Normalize();
				}

				int maxId = records.Count == 0 ? 0 : records.Max(record => record.id);
				nextId = System.Math.Max(nextId, maxId + 1);
				schemaVersion = CurrentSchema;
			}
		}

		public BlueprintRecord Add(BlueprintRecord record)
		{
			if (record == null)
			{
				return null;
			}

			record.id = nextId++;
			record.Normalize();
			records.Add(record);
			return record;
		}

		public bool Remove(BlueprintRecord record)
		{
			return record != null && records.Remove(record);
		}

		public BlueprintRecord Find(int id)
		{
			return records.FirstOrDefault(record => record.id == id);
		}

		/// <summary>给新捕获/导入的蓝图起一个不重名的默认名。</summary>
		public string NextDefaultName()
		{
			for (int index = records.Count + 1; index < records.Count + 1000; index++)
			{
				string candidate = "CK.Blueprint.DefaultName".Translate(index);
				if (!records.Any(record => record.name == candidate))
				{
					return candidate;
				}
			}
			return "CK.Blueprint.DefaultName".Translate(records.Count + 1);
		}

		/// <summary>重名时追加「(2)」「(3)」；导入模板用。</summary>
		public string UniqueName(string desired)
		{
			string baseName = desired.NullOrEmpty() ? NextDefaultName() : desired;
			if (!records.Any(record => record.name == baseName))
			{
				return baseName;
			}

			for (int index = 2; index < 1000; index++)
			{
				string candidate = baseName + " (" + index + ")";
				if (!records.Any(record => record.name == candidate))
				{
					return candidate;
				}
			}
			return baseName;
		}

		/// <summary>把模板克隆进当前存档库；返回新记录。</summary>
		public BlueprintRecord Import(BlueprintRecord template)
		{
			if (template == null)
			{
				return null;
			}

			BlueprintRecord copy = template.Clone();
			copy.name = UniqueName(template.name);
			return Add(copy);
		}
	}

	/// <summary>
	/// 对局级组件：只负责持有蓝图库并随存档保存。
	/// </summary>
	/// <remarks>
	/// <see cref="Verse.Game.FillComponents"/> 会反射实例化所有 <see cref="GameComponent"/> 子类，
	/// 构造函数必须接收 <see cref="Game"/>；不需要 XML 注册。
	/// </remarks>
	public class BlueprintGameComponent : GameComponent
	{
		public BlueprintLibrary library = new BlueprintLibrary();

		public BlueprintGameComponent(Game game)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref library, "ck_blueprint_library");
			if (library == null)
			{
				library = new BlueprintLibrary();
			}
		}

		/// <summary>当前存档的蓝图库；无对局/组件未就绪时返回 null。</summary>
		public static BlueprintLibrary CurrentLibrary =>
			Current.Game?.GetComponent<BlueprintGameComponent>()?.library;
	}
}
