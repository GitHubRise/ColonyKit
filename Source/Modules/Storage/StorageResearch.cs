using RimWorld;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>立体库科技等级：T1/T2/T3 链式研究。</summary>
	public static class StorageResearch
	{
		public static bool Tier1Finished => IsFinished("CK_StorageTier1");
		public static bool Tier2Finished => IsFinished("CK_StorageTier2");
		public static bool Tier3Finished => IsFinished("CK_StorageTier3");

		/// <summary>最高已完成科技对应的堆叠倍率；未研究返回 0。</summary>
		public static int StackMultiplier
		{
			get
			{
				if (Tier3Finished)
				{
					return 60;
				}
				if (Tier2Finished)
				{
					return 30;
				}
				if (Tier1Finished)
				{
					return 10;
				}
				return 0;
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
