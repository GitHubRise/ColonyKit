using RimWorld;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 立体库科技等级：T1/T2/T3 链式研究。
	/// T1 只负责全局物品堆叠（全局固定 ×10）；T2/T3 只放大立体库设备内部每堆。
	/// </summary>
	public static class StorageResearch
	{
		public const int Tier1GlobalMultiplier = 10;
		public const int Tier2WarehouseMultiplier = 3;
		public const int Tier3WarehouseMultiplier = 2;

		public static bool Tier1Finished => IsFinished("CK_StorageTier1");
		public static bool Tier2Finished => IsFinished("CK_StorageTier2");
		public static bool Tier3Finished => IsFinished("CK_StorageTier3");

		/// <summary>全局堆叠倍率：完成一级后固定 ×10；未完成返回 0，表示沿用设置页倍率。</summary>
		public static int GlobalStackMultiplier => Tier1Finished ? Tier1GlobalMultiplier : 0;

		/// <summary>立体库设备内部每堆的额外倍率：二级 ×3，三级再 ×2（合计 ×6）。</summary>
		public static int WarehouseStackMultiplier
		{
			get
			{
				int multiplier = 1;
				if (Tier2Finished)
				{
					multiplier *= Tier2WarehouseMultiplier;
				}
				if (Tier3Finished)
				{
					multiplier *= Tier3WarehouseMultiplier;
				}
				return multiplier;
			}
		}

		private static bool IsFinished(string defName)
		{
			if (Current.Game == null || Find.ResearchManager == null)
			{
				return false;
			}
			return DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName)?.IsFinished ?? false;
		}
	}
}
