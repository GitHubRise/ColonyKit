using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>蓝图材料策略：决定复刻时每个建筑用哪种材料。</summary>
	public enum BlueprintMaterialMode
	{
		/// <summary>优先用捕获时的原材质；原材质不可用时退回当前可用材料。</summary>
		PreferSource = 0,

		/// <summary>优先用地图上现有库存的原材质，没有库存就挑现有库存里最便宜的。</summary>
		Available = 1,

		/// <summary>始终挑该建筑能用的最便宜材料。</summary>
		Cheapest = 2
	}

	/// <summary>蓝图模块设置：材料策略、捕获尺寸上限、调试日志。</summary>
	/// <remarks>
	/// 数据由 <see cref="ColonyKitSettings.moduleSettings"/> 持久化；
	/// 蓝图库本身随存档保存，不放在这里。
	/// </remarks>
	public class BlueprintSettings : IColonyKitModuleSettings
	{
		public const int MinMaxSide = 8;
		public const int MaxMaxSide = 200;
		public const int DefaultMaxSide = 80;

		public const int MinMaxEntries = 50;
		public const int MaxMaxEntries = 10000;
		public const int DefaultMaxEntries = 3000;

		public const int MinMaxRecords = 16;
		public const int MaxMaxRecords = 2048;
		public const int DefaultMaxRecords = 256;

		public string SettingsKey => "blueprint";

		/// <summary>默认材料策略：先按原材质，库存没有就换现有材料，保证布局能先落地。</summary>
		public BlueprintMaterialMode materialMode = BlueprintMaterialMode.Available;

		/// <summary>单张蓝图允许的最大边长（格）；超过则拒绝捕获。</summary>
		public int maxSide = DefaultMaxSide;

		/// <summary>单张蓝图允许的最大建筑数；超过则拒绝捕获。</summary>
		public int maxEntries = DefaultMaxEntries;

		/// <summary>每个存档最多保存多少张蓝图。</summary>
		public int maxRecords = DefaultMaxRecords;

		/// <summary>调试日志：记录每次捕获/放置的明细。</summary>
		public bool verboseLog;

		public void ExposeData()
		{
			Scribe_Values.Look(ref materialMode, "ck_blueprint_materialMode", BlueprintMaterialMode.Available);
			Scribe_Values.Look(ref maxSide, "ck_blueprint_maxSide", DefaultMaxSide);
			Scribe_Values.Look(ref maxEntries, "ck_blueprint_maxEntries", DefaultMaxEntries);
			Scribe_Values.Look(ref maxRecords, "ck_blueprint_maxRecords", DefaultMaxRecords);
			Scribe_Values.Look(ref verboseLog, "ck_blueprint_verboseLog", false);
		}

		public bool Validate()
		{
			bool changed = false;

			if (materialMode != BlueprintMaterialMode.PreferSource
				&& materialMode != BlueprintMaterialMode.Available
				&& materialMode != BlueprintMaterialMode.Cheapest)
			{
				materialMode = BlueprintMaterialMode.Available;
				changed = true;
			}

			if (maxSide < MinMaxSide || maxSide > MaxMaxSide)
			{
				maxSide = DefaultMaxSide;
				changed = true;
			}

			if (maxEntries < MinMaxEntries || maxEntries > MaxMaxEntries)
			{
				maxEntries = DefaultMaxEntries;
				changed = true;
			}

			if (maxRecords < MinMaxRecords || maxRecords > MaxMaxRecords)
			{
				maxRecords = DefaultMaxRecords;
				changed = true;
			}

			if (changed)
			{
				ModLog.Warn("Blueprint", "invalid blueprint settings corrected.");
			}
			return changed;
		}

		public void Reset()
		{
			materialMode = BlueprintMaterialMode.Available;
			maxSide = DefaultMaxSide;
			maxEntries = DefaultMaxEntries;
			maxRecords = DefaultMaxRecords;
			verboseLog = false;
		}
	}

	/// <summary>取蓝图设置；设置根尚未就绪时返回一个临时默认对象，避免 UI 空引用。</summary>
	public static class BlueprintSettingsUtility
	{
		public static BlueprintSettings Current =>
			ColonyKitMod.Settings?.GetModuleSettings<BlueprintSettings>() ?? new BlueprintSettings();
	}
}
