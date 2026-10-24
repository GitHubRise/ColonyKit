using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>立体库模块：先提供全局堆叠规则，仓库建筑/传送带后续接入。</summary>
	public sealed class StorageModule : ColonyKitModule
	{
		public override string Id => "storage";

		public override string DisplayNameKey => "ColonyKit.Module.Storage.Name";

		public override string DescriptionKey => "ColonyKit.Module.Storage.Desc";

		public override int InitOrder => 110;

		public override void OnDefsLoaded()
		{
			StorageSettings settings = ColonyKitMod.Settings?.GetModuleSettings<StorageSettings>();
			if (settings == null)
			{
				ModLog.Warn("Storage", "settings unavailable; stack rules skipped.");
				return;
			}

			StorageStackLimitManager.Apply(settings);
			ModLog.Info(
				"Storage",
				$"module loaded; stack rules enabled={settings.enabled}, x{settings.stackMultiplier}, " +
				$"StackXXL active={StorageStackLimitManager.StackXxlActive}");
			ModLog.Info(
				"Storage",
				$"jobs: inbound={DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_HaulToWarehouse") != null}, " +
				$"outbound={DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_WithdrawFromWarehouse") != null}, " +
				$"jobDef={CKDefOf.CK_WithdrawFromWarehouse != null}");
		}

		public override void ResetRuntimeState()
		{
			StorageSettings settings = ColonyKitMod.Settings?.GetModuleSettings<StorageSettings>();
			if (settings == null || !ModuleRegistry.IsEnabled(Id) || !settings.enabled)
			{
				StorageStackLimitManager.Restore();
				return;
			}

			StorageStackLimitManager.Apply(settings);
		}
	}
}
