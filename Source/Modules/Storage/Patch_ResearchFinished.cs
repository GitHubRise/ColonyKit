using HarmonyLib;
using RimWorld;

namespace ColonyKit.Modules.Storage
{
	/// <summary>研究完成后立即按新科技等级重算堆叠倍率。</summary>
	[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
	internal static class Patch_ResearchFinished
	{
		private static void Postfix()
		{
			StorageSettings settings = ColonyKitMod.Settings?.GetModuleSettings<StorageSettings>();
			if (settings != null)
			{
				StorageStackLimitManager.Apply(settings);
			}
		}
	}
}
