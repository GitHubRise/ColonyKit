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

			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.Hint".Loc());
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
			data.corpseDisposal = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.CorpseDisposal".Loc(),
				ref data.corpseDisposal,
				"ColonyKit.Settings.Robotics.CorpseDisposal.Desc".Loc());
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
			data.cook = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Cook".Loc(),
				ref data.cook,
				"ColonyKit.Settings.Robotics.Cook.Desc".Loc());
			data.doctor = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Doctor".Loc(),
				ref data.doctor,
				"ColonyKit.Settings.Robotics.Doctor.Desc".Loc());
			data.research = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Research".Loc(),
				ref data.research,
				"ColonyKit.Settings.Robotics.Research.Desc".Loc());
			data.crafting = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.Crafting".Loc(),
				ref data.crafting,
				"ColonyKit.Settings.Robotics.Crafting.Desc".Loc());
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

			data.robotPowerEfficiency = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.RobotPowerEfficiency".Loc(),
				data.robotPowerEfficiency,
				RoboticsSettings.MinRobotPowerEfficiency,
				RoboticsSettings.MaxRobotPowerEfficiency,
				5f,
				value => value.ToString("0") + "%");

			data.chargePower = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.ChargePower".Loc(),
				data.chargePower,
				RoboticsSettings.MinChargePower,
				RoboticsSettings.MaxChargePower,
				50f,
				value => "CK.Robotics.Watts".Loc(value.ToString("0")));
			data.chargeEfficiency = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.ChargeEfficiency".Loc(),
				data.chargeEfficiency,
				RoboticsSettings.MinChargeEfficiency,
				RoboticsSettings.MaxChargeEfficiency,
				1f,
				value => value.ToString("0") + "%");
			data.powerConversionRate = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.PowerConversionRate".Loc(),
				data.powerConversionRate,
				RoboticsSettings.MinPowerConversionRate,
				RoboticsSettings.MaxPowerConversionRate,
				1f,
				value => value.ToString("0") + "%");

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
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.Combat".Loc());
			data.startWithMkV = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.StartWithMkV".Loc(),
				ref data.startWithMkV,
				"ColonyKit.Settings.Robotics.StartWithMkV.Desc".Loc());
			data.mkvCombatEnabled = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.MkVCombatEnabled".Loc(),
				ref data.mkvCombatEnabled,
				"ColonyKit.Settings.Robotics.MkVCombatEnabled.Desc".Loc());
			data.mkvReturnToBay = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.MkVReturnToBay".Loc(),
				ref data.mkvReturnToBay,
				"ColonyKit.Settings.Robotics.MkVReturnToBay.Desc".Loc());
			data.mkvAutoPickupWeapons = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.MkVAutoPickupWeapons".Loc(),
				ref data.mkvAutoPickupWeapons,
				"ColonyKit.Settings.Robotics.MkVAutoPickupWeapons.Desc".Loc());
			data.mkvEngageRange = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVEngageRange".Loc(),
				data.mkvEngageRange,
				RoboticsSettings.MinEngageRange,
				RoboticsSettings.MaxEngageRange,
				1f,
				value => "CK.Robotics.Cells".Loc(value.ToString("0")));
			data.mkvReturnRange = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVReturnRange".Loc(),
				data.mkvReturnRange,
				RoboticsSettings.MinReturnRange,
				RoboticsSettings.MaxReturnRange,
				1f,
				value => "CK.Robotics.Cells".Loc(value.ToString("0")));
			data.mkvWeaponPickupRadius = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVWeaponPickupRadius".Loc(),
				data.mkvWeaponPickupRadius,
				RoboticsSettings.MinWeaponPickupRadius,
				RoboticsSettings.MaxWeaponPickupRadius,
				1f,
				value => "CK.Robotics.Cells".Loc(value.ToString("0")));
			data.mkvAttackSpeed = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVAttackSpeed".Loc(),
				data.mkvAttackSpeed,
				RoboticsSettings.MinMkvAttackSpeed,
				RoboticsSettings.MaxMkvAttackSpeed,
				0.05f,
				value => "x" + value.ToString("0.00"));
			data.mkvHitChance = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVHitChance".Loc(),
				data.mkvHitChance,
				RoboticsSettings.MinMkvHitChance,
				RoboticsSettings.MaxMkvHitChance,
				0.05f,
				value => value.ToStringPercent());
			data.mkvBurstShotCount = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvBurstShotCount".Loc(),
				data.mkvBurstShotCount,
				RoboticsSettings.MinMkvBurstShotCount,
				RoboticsSettings.MaxMkvBurstShotCount,
				1f,
				value => "CK.Robotics.BurstShots".Loc(value.ToString("0"))));
			data.mkvBurstCooldown = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvBurstCooldown".Loc(),
				data.mkvBurstCooldown,
				RoboticsSettings.MinMkvBurstCooldown,
				RoboticsSettings.MaxMkvBurstCooldown,
				0.1f,
				value => value.ToString("0.0") + " s");
			data.mkvBayAutoStart = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.MkVBayAutoStart".Loc(),
				ref data.mkvBayAutoStart,
				"ColonyKit.Settings.Robotics.MkVBayAutoStart.Desc".Loc());

			listing.Gap();
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.PassiveCharging".Loc());

			// 太阳能/风能放在最上面：这两项是 Mk-V 在野外的主要补能方式。
			data.mkvSolarCharging = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.MkVSolarCharging".Loc(),
				ref data.mkvSolarCharging,
				"ColonyKit.Settings.Robotics.MkVSolarCharging.Desc".Loc());
			data.mkvWindCharging = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Robotics.MkVWindCharging".Loc(),
				ref data.mkvWindCharging,
				"ColonyKit.Settings.Robotics.MkVWindCharging.Desc".Loc());
			data.mkvSolarEfficiency = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVSolarEfficiency".Loc(),
				data.mkvSolarEfficiency,
				RoboticsSettings.MinMkvSolarEfficiency,
				RoboticsSettings.MaxMkvSolarEfficiency,
				1f,
				value => value.ToString("0") + "%");
			data.mkvWindEfficiency = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVWindEfficiency".Loc(),
				data.mkvWindEfficiency,
				RoboticsSettings.MinMkvWindEfficiency,
				RoboticsSettings.MaxMkvWindEfficiency,
				1f,
				value => value.ToString("0") + "%");
			data.mkvLightThreshold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVLightThreshold".Loc(),
				data.mkvLightThreshold,
				RoboticsSettings.MinMkvLightThreshold,
				RoboticsSettings.MaxMkvLightThreshold,
				0.01f,
				value => value.ToStringPercent());
			data.mkvWindThreshold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVWindThreshold".Loc(),
				data.mkvWindThreshold,
				RoboticsSettings.MinMkvWindThreshold,
				RoboticsSettings.MaxMkvWindThreshold,
				0.01f,
				value => value.ToStringPercent());

			listing.Gap();
			data.mkvChargePower = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVChargePower".Loc(),
				data.mkvChargePower,
				RoboticsSettings.MinMkvChargePower,
				RoboticsSettings.MaxMkvChargePower,
				50f,
				value => "CK.Robotics.Watts".Loc(value.ToString("0")));
			data.mkvChargeEfficiency = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVChargeEfficiency".Loc(),
				data.mkvChargeEfficiency,
				RoboticsSettings.MinMkvChargeEfficiency,
				RoboticsSettings.MaxMkvChargeEfficiency,
				1f,
				value => value.ToString("0") + "%");
			data.mkvPowerConversionRate = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVPowerConversionRate".Loc(),
				data.mkvPowerConversionRate,
				RoboticsSettings.MinMkvPowerConversionRate,
				RoboticsSettings.MaxMkvPowerConversionRate,
				1f,
				value => value.ToString("0") + "%");

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
					SettingsWidgets.Description(
						listing,
						"ColonyKit.Settings.Robotics.AreaLabel.Desc".Loc());
				}
			}

			listing.Gap();
			SettingsWidgets.Section(listing, "ColonyKit.Settings.Robotics.Section.Maintenance".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.DurabilityHint".Loc());
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
