using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 立体库模块设置：先落地「全局堆叠倍率」，后续仓库核心/传送带再扩字段。
	/// </summary>
	public class StorageSettings : IColonyKitModuleSettings
	{
		public const int MinMultiplier = 1;
		public const int MaxMultiplier = 20;

		public const int MinWarehouseStacks = 1;
		public const int MaxWarehouseStacks = 200;
		public const int DefaultWarehouseStacks = 24;

		public string SettingsKey => "storage";

		/// <summary>是否启用堆叠增强。</summary>
		public bool enabled = true;

		/// <summary>所有可堆叠物品的堆叠上限倍率；1 等于原版。</summary>
		public int stackMultiplier = 10;

		/// <summary>每座立体库可存的堆数；对已建和新建仓库都生效。</summary>
		public int warehouseStackCapacity = DefaultWarehouseStacks;

		/// <summary>调试用：逐个 Def 打印堆叠上限变化。</summary>
		public bool logChanges;

		public void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "enabled", true);
			Scribe_Values.Look(ref stackMultiplier, "stackMultiplier", 10);
			Scribe_Values.Look(ref warehouseStackCapacity, "warehouseStackCapacity", DefaultWarehouseStacks);
			Scribe_Values.Look(ref logChanges, "logChanges", false);
		}

		public bool Validate()
		{
			bool changed = false;
			if (stackMultiplier < MinMultiplier || stackMultiplier > MaxMultiplier)
			{
				stackMultiplier = MinMultiplier;
				changed = true;
			}

			if (warehouseStackCapacity < MinWarehouseStacks || warehouseStackCapacity > MaxWarehouseStacks)
			{
				warehouseStackCapacity = DefaultWarehouseStacks;
				changed = true;
			}

			if (changed)
			{
				ModLog.Warn("Storage", "invalid stack settings corrected.");
			}
			return changed;
		}

		public void Reset()
		{
			enabled = true;
			stackMultiplier = 10;
			warehouseStackCapacity = DefaultWarehouseStacks;
			logChanges = false;
		}
	}
}
