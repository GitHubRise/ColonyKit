using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>立体库设置页：堆叠开关、倍率、调试日志。</summary>
	public sealed class StorageSettingsPage : SettingsPage
	{
		public override string Id => "storage";

		public override string TitleKey => "ColonyKit.Settings.Tab.Storage";

		public override int Order => 25;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			StorageSettings data = settings.GetModuleSettings<StorageSettings>();

			SettingsWidgets.Label(listing, "ColonyKit.Settings.Storage.Hint".Loc());
			listing.Gap();

			bool beforeEnabled = data.enabled;
			data.enabled = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Storage.Enabled".Loc(),
				ref data.enabled,
				"ColonyKit.Settings.Storage.Enabled.Desc".Loc());

			if (data.enabled != beforeEnabled)
			{
				if (data.enabled)
				{
					StorageStackLimitManager.Apply(data);
				}
				else
				{
					StorageStackLimitManager.Restore();
				}
			}

			if (StorageStackLimitManager.StackXxlActive)
			{
				SettingsWidgets.Label(listing, "ColonyKit.Settings.Storage.StackXXLWarn".Loc());
			}

			int beforeMultiplier = data.stackMultiplier;
			int multiplier = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Storage.Multiplier".Loc(),
				data.stackMultiplier,
				StorageSettings.MinMultiplier,
				StorageSettings.MaxMultiplier,
				1f,
				value => "×" + value.ToString("0")));

			if (multiplier != beforeMultiplier)
			{
				data.stackMultiplier = Mathf.Clamp(multiplier, StorageSettings.MinMultiplier, StorageSettings.MaxMultiplier);
				if (data.enabled)
				{
					StorageStackLimitManager.Apply(data);
				}
			}

			int beforeCapacity = data.warehouseStackCapacity;
			int capacity = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Storage.WarehouseStacks".Loc(),
				data.warehouseStackCapacity,
				StorageSettings.MinWarehouseStacks,
				StorageSettings.MaxWarehouseStacks,
				1f,
				value => "ColonyKit.Settings.Storage.WarehouseStacksValue".Loc(value.ToString("0"))));

			if (capacity != beforeCapacity)
			{
				data.warehouseStackCapacity = capacity;
			}
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Storage.WarehouseStacks.Desc".Loc());

			data.logChanges = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Storage.LogChanges".Loc(),
				ref data.logChanges,
				"ColonyKit.Settings.Storage.LogChanges.Desc".Loc());

			listing.Gap();
			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Storage.Apply".Loc()))
			{
				if (data.enabled)
				{
					StorageStackLimitManager.Apply(data);
				}
				else
				{
					StorageStackLimitManager.Restore();
				}
				Messages.Message("ColonyKit.Settings.Storage.Applied".Loc(), MessageTypeDefOf.NeutralEvent, false);
			}
		}

		public override void Reset(ColonyKitSettings settings)
		{
			StorageSettings data = settings.GetModuleSettings<StorageSettings>();
			data.Reset();
			StorageStackLimitManager.Apply(data);
		}
	}
}
