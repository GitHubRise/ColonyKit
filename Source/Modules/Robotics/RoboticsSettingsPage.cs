using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>机器人设置页：家务开关、阈值、电池容量、工作范围与表现。</summary>
	public sealed class RoboticsSettingsPage : SettingsPage
	{
		public override string Id => "robotics";

		public override string TitleKey => "ColonyKit.Settings.Tab.Robotics";

		public override int Order => 28;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			RoboticsSettings data = settings.GetModuleSettings<RoboticsSettings>();

			SettingsWidgets.Label(listing, "ColonyKit.Settings.Robotics.Hint".Loc());
			listing.Gap();

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.Chores".Loc());
			bool beforeSweep = data.sweep;
			bool beforeScrub = data.scrub;
			bool beforeHaul = data.haul;

			data.sweep = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Sweep".Loc(),
				ref data.sweep,
				"ColonyKit.Settings.Robotics.Sweep.Desc".Loc());
			data.scrub = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Scrub".Loc(),
				ref data.scrub,
				"ColonyKit.Settings.Robotics.Scrub.Desc".Loc());
			data.haul = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Haul".Loc(),
				ref data.haul,
				"ColonyKit.Settings.Robotics.Haul.Desc".Loc());
			data.firefight = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Firefight".Loc(),
				ref data.firefight,
				"ColonyKit.Settings.Robotics.Firefight.Desc".Loc());
			data.butcher = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Butcher".Loc(),
				ref data.butcher,
				"ColonyKit.Settings.Robotics.Butcher.Desc".Loc());
			data.refuel = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Refuel".Loc(),
				ref data.refuel,
				"ColonyKit.Settings.Robotics.Refuel.Desc".Loc());
			data.feedPrisoners = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.FeedPrisoners".Loc(),
				ref data.feedPrisoners,
				"ColonyKit.Settings.Robotics.FeedPrisoners.Desc".Loc());
			data.hygiene = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Hygiene".Loc(),
				ref data.hygiene,
				"ColonyKit.Settings.Robotics.Hygiene.Desc".Loc());
			data.ropeAnimals = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.RopeAnimals".Loc(),
				ref data.ropeAnimals,
				"ColonyKit.Settings.Robotics.RopeAnimals.Desc".Loc());
			data.storage = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Storage".Loc(),
				ref data.storage,
				"ColonyKit.Settings.Robotics.Storage.Desc".Loc());
			data.rescue = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Rescue".Loc(),
				ref data.rescue,
				"ColonyKit.Settings.Robotics.Rescue.Desc".Loc());
			data.dangerAvoidance = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.DangerAvoidance".Loc(),
				ref data.dangerAvoidance,
				"ColonyKit.Settings.Robotics.DangerAvoidance.Desc".Loc());

			if (data.sweep != beforeSweep || data.scrub != beforeScrub || data.haul != beforeHaul)
			{
				NannyBotUtility.SyncLegacyToggles();
			}

			listing.Gap();
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.Parameters".Loc());

			data.chargeThreshold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.ChargeThreshold".Loc(),
				data.chargeThreshold,
				RoboticsSettings.MinChargeThreshold,
				RoboticsSettings.MaxChargeThreshold,
				0.01f,
				value => value.ToStringPercent());

			int beforeCapacity = data.batteryCapacity;
			data.batteryCapacity = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.BatteryCapacity".Loc(),
				data.batteryCapacity,
				RoboticsSettings.MinBatteryCapacity,
				RoboticsSettings.MaxBatteryCapacity,
				100f,
				value => "CK.Robotics.Watts".Loc(value.ToString("0"))));
			if (data.batteryCapacity != beforeCapacity)
			{
				NannyBotUtility.ApplyBatteryCapacity(beforeCapacity, data.batteryCapacity);
			}

			data.refuelThreshold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.RefuelThreshold".Loc(),
				data.refuelThreshold,
				RoboticsSettings.MinRefuelThreshold,
				RoboticsSettings.MaxRefuelThreshold,
				0.01f,
				value => value.ToStringPercent());

			data.workRadius = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.WorkRadius".Loc(),
				data.workRadius,
				RoboticsSettings.MinWorkRadius,
				RoboticsSettings.MaxWorkRadius,
				5f,
				value => value <= 0.1f
					? "ColonyKit.Settings.Robotics.Unlimited".Loc()
					: "CK.Robotics.Cells".Loc(value.ToString("0")));

			listing.Gap();
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.Area".Loc());

			List<RobotAreaMode> modes = new List<RobotAreaMode>
			{
				RobotAreaMode.Unrestricted,
				RobotAreaMode.Home,
				RobotAreaMode.Custom
			};
			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Robotics.AreaMode".Loc(),
				data.areaMode,
				modes,
				mode => ("ColonyKit.Settings.Robotics.Area." + mode).Loc(),
				mode => data.areaMode = mode);

			if (data.areaMode == RobotAreaMode.Custom)
			{
				if (Find.CurrentMap == null)
				{
					data.allowedAreaLabel = SettingsWidgets.TextField(
						listing,
						"ColonyKit.Settings.Robotics.AreaLabel".Loc(),
						data.allowedAreaLabel,
						48,
						"ColonyKit.Settings.Robotics.AreaLabel.Desc".Loc());
				}
				else
				{
					List<string> areaLabels = new List<string> { "" };
					foreach (Area area in Find.CurrentMap.areaManager.AllAreas)
					{
						if (area == null || area.Label.NullOrEmpty() || areaLabels.Contains(area.Label))
						{
							continue;
						}
						areaLabels.Add(area.Label);
					}
					if (!data.allowedAreaLabel.NullOrEmpty()
						&& !areaLabels.Contains(data.allowedAreaLabel))
					{
						areaLabels.Add(data.allowedAreaLabel);
					}
					SettingsWidgets.Dropdown(
						listing,
						"ColonyKit.Settings.Robotics.AreaLabel".Loc(),
						data.allowedAreaLabel,
						areaLabels,
						label => label.NullOrEmpty()
							? "ColonyKit.Settings.Robotics.Unlimited".Loc()
							: label,
						label => data.allowedAreaLabel = label);
					SettingsWidgets.Label(
						listing,
						"ColonyKit.Settings.Robotics.AreaLabel.Desc".Loc());
				}
			}

			listing.Gap();
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.Maintenance".Loc());
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Robotics.DurabilityHint".Loc());
			data.sounds = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Sounds".Loc(),
				ref data.sounds,
				"ColonyKit.Settings.Robotics.Sounds.Desc".Loc());
			data.chargingGlow = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.ChargingGlow".Loc(),
				ref data.chargingGlow,
				"ColonyKit.Settings.Robotics.ChargingGlow.Desc".Loc());
		}

		public override void Reset(ColonyKitSettings settings)
		{
			RoboticsSettings data = settings.GetModuleSettings<RoboticsSettings>();
			int oldCapacity = data.batteryCapacity;
			data.Reset();
			NannyBotUtility.SyncLegacyToggles();
			NannyBotUtility.ApplyBatteryCapacity(oldCapacity, data.batteryCapacity);
		}
	}
}
