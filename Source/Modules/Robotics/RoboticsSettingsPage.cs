using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人设置页：按「家务 / 参数 / 作战 / 充电 / 区域与维护 / Mk-V AI」分成子页签，
	/// 每个参数组用显式框包起来，开关统一成「标签在左、勾在右」的行。
	/// </summary>
	public sealed class RoboticsSettingsPage : SettingsPage
	{
		private static readonly string[] SubTabs =
		{
			"ColonyKit.Settings.Robotics.Sub.Chores",
			"ColonyKit.Settings.Robotics.Sub.Parameters",
			"ColonyKit.Settings.Robotics.Sub.Combat",
			"ColonyKit.Settings.Robotics.Sub.Charging",
			"ColonyKit.Settings.Robotics.Sub.AreaMaintenance",
			"ColonyKit.Settings.Robotics.Sub.Ai"
		};

		public override string Id => "robotics";

		public override string TitleKey => "ColonyKit.Settings.Tab.Robotics";

		public override int Order => 28;

		public override IReadOnlyList<string> SubTabKeys => SubTabs;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			DoSubTabContents(0, listing, settings);
		}

		public override void DoSubTabContents(
			int subTab,
			Listing_Standard listing,
			ColonyKitSettings settings)
		{
			RoboticsSettings data = settings.GetModuleSettings<RoboticsSettings>();
			switch (subTab)
			{
				case 1:
					DrawParameters(listing, data);
					break;
				case 2:
					DrawCombat(listing, data);
					break;
				case 3:
					DrawCharging(listing, data);
					break;
				case 4:
					DrawAreaAndMaintenance(listing, data);
					break;
				case 5:
					DrawAi(listing, data);
					break;
				default:
					DrawChores(listing, data);
					break;
			}
		}

		// ---------------------------------------------------------------- 家务

		private void DrawChores(Listing_Standard listing, RoboticsSettings data)
		{
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.Hint".Loc());

			bool beforeSweep = data.sweep;
			bool beforeScrub = data.scrub;
			bool beforeHaul = data.haul;

			SettingsWidgets.FrameScope scope =
				SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Cleaning".Loc());
			Toggle(listing, data, "Sweep");
			Toggle(listing, data, "Scrub");
			Toggle(listing, data, "Hygiene");
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Hauling".Loc());
			Toggle(listing, data, "Haul");
			Toggle(listing, data, "Storage");
			Toggle(listing, data, "RopeAnimals");
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Safety".Loc());
			Toggle(listing, data, "Firefight");
			Toggle(listing, data, "DangerAvoidance");
			Toggle(listing, data, "Rescue");
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Livestock".Loc());
			Toggle(listing, data, "Butcher");
			Toggle(listing, data, "CorpseDisposal", tag: "Mk-V");
			Toggle(listing, data, "Refuel");
			Toggle(listing, data, "FeedPrisoners");
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Production".Loc());
			Toggle(listing, data, "Cook", tag: "Mk-V");
			Toggle(listing, data, "Doctor", tag: "Mk-V");
			Toggle(listing, data, "Research", tag: "Mk-V");
			Toggle(listing, data, "Crafting", tag: "Mk-V");
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Construction".Loc());
			Toggle(listing, data, "Construct");
			Toggle(listing, data, "Water");
			Toggle(listing, data, "Plants");
			SettingsWidgets.FrameEnd(listing, scope);

			if (data.sweep != beforeSweep || data.scrub != beforeScrub || data.haul != beforeHaul)
			{
				NannyBotUtility.SyncLegacyToggles();
			}
		}

		// ---------------------------------------------------------------- 参数

		private void DrawParameters(Listing_Standard listing, RoboticsSettings data)
		{
			SettingsWidgets.FrameScope scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Power".Loc());
			data.chargeThreshold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.ChargeThreshold".Loc(),
				data.chargeThreshold,
				RoboticsSettings.MinChargeThreshold,
				RoboticsSettings.MaxChargeThreshold,
				0.01f,
				value => value.ToStringPercent());
			data.refuelThreshold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.RefuelThreshold".Loc(),
				data.refuelThreshold,
				RoboticsSettings.MinRefuelThreshold,
				RoboticsSettings.MaxRefuelThreshold,
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
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Carrying".Loc());
			data.robotCarryCapacity = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.RobotCarryCapacity".Loc(),
				data.robotCarryCapacity,
				RoboticsSettings.MinRobotCarryCapacity,
				RoboticsSettings.MaxRobotCarryCapacity,
				1f,
				value => "CK.Robotics.Kilograms".Loc(value.ToString("0")));
			data.mkvCarryCapacity = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvCarryCapacity".Loc(),
				data.mkvCarryCapacity,
				RoboticsSettings.MinRobotCarryCapacity,
				RoboticsSettings.MaxRobotCarryCapacity,
				1f,
				value => "CK.Robotics.Kilograms".Loc(value.ToString("0")));
			SettingsWidgets.Description(
				listing, "ColonyKit.Settings.Robotics.CarryCapacityHint".Loc());
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Electricity".Loc());
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
			SettingsWidgets.FrameEnd(listing, scope);
		}

		// ---------------------------------------------------------------- 作战

		private void DrawCombat(Listing_Standard listing, RoboticsSettings data)
		{
			SettingsWidgets.FrameScope scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.CombatToggles".Loc());
			data.mkvCombatEnabled = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkVCombatEnabled".Loc(),
				ref data.mkvCombatEnabled,
				"ColonyKit.Settings.Robotics.MkVCombatEnabled.Desc".Loc());
			data.mkvReturnToBay = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkVReturnToBay".Loc(),
				ref data.mkvReturnToBay,
				"ColonyKit.Settings.Robotics.MkVReturnToBay.Desc".Loc());
			data.mkvAutoPickupWeapons = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkVAutoPickupWeapons".Loc(),
				ref data.mkvAutoPickupWeapons,
				"ColonyKit.Settings.Robotics.MkVAutoPickupWeapons.Desc".Loc());
			data.mkvDualWield = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkVDualWield".Loc(),
				ref data.mkvDualWield,
				"ColonyKit.Settings.Robotics.MkVDualWield.Desc".Loc());
			data.mkvBayAutoStart = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkVBayAutoStart".Loc(),
				ref data.mkvBayAutoStart,
				"ColonyKit.Settings.Robotics.MkVBayAutoStart.Desc".Loc());
			data.startWithMkVCount = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.StartWithMkV".Loc(),
				data.startWithMkVCount,
				RoboticsSettings.MinStartWithMkVCount,
				RoboticsSettings.MaxStartWithMkVCount,
				1f,
				value => "CK.Robotics.Robots".Loc(value.ToString("0"))));
			SettingsWidgets.Description(
				listing, "ColonyKit.Settings.Robotics.StartWithMkV.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Engagement".Loc());
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
			data.mkvIncomingDamageFactor = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVIncomingDamageFactor".Loc(),
				data.mkvIncomingDamageFactor,
				RoboticsSettings.MinMkvIncomingDamageFactor,
				RoboticsSettings.MaxMkvIncomingDamageFactor,
				0.05f,
				value => value.ToStringPercent());
			data.mkvInsulationCold = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVInsulationCold".Loc(),
				data.mkvInsulationCold,
				RoboticsSettings.MinMkvInsulation,
				RoboticsSettings.MaxMkvInsulation,
				1f,
				value => value.ToString("0") + "°C");
			data.mkvInsulationHeat = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkVInsulationHeat".Loc(),
				data.mkvInsulationHeat,
				RoboticsSettings.MinMkvInsulation,
				RoboticsSettings.MaxMkvInsulation,
				1f,
				value => value.ToString("0") + "°C");
			SettingsWidgets.Description(
				listing, "ColonyKit.Settings.Robotics.MkVInsulationHint".Loc());
			SettingsWidgets.FrameEnd(listing, scope);

			// 电弧刃与电弧发射器是两套独立参数，各用一个框。
			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.ArcBlade".Loc());
			data.mkvArcBladeDamage = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeDamage".Loc(),
				data.mkvArcBladeDamage,
				RoboticsSettings.MinArcBladeDamage,
				RoboticsSettings.MaxArcBladeDamage,
				1f,
				value => value.ToString("0"));
			data.mkvArcBladeCooldown = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeCooldown".Loc(),
				data.mkvArcBladeCooldown,
				RoboticsSettings.MinArcBladeCooldown,
				RoboticsSettings.MaxArcBladeCooldown,
				0.1f,
				value => value.ToString("0.0") + " s");
			data.mkvArcBladeArmorPenetration = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeArmorPenetration".Loc(),
				data.mkvArcBladeArmorPenetration,
				RoboticsSettings.MinArcBladeArmorPenetration,
				RoboticsSettings.MaxArcBladeArmorPenetration,
				0.05f,
				value => value.ToStringPercent());
			data.mkvArcBladeParalysisChance = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeParalysisChance".Loc(),
				data.mkvArcBladeParalysisChance,
				RoboticsSettings.MinArcBladeChance,
				RoboticsSettings.MaxArcBladeChance,
				0.05f,
				value => value.ToStringPercent());
			data.mkvArcBladeParalysisHours = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeParalysisHours".Loc(),
				data.mkvArcBladeParalysisHours,
				RoboticsSettings.MinArcBladeParalysisHours,
				RoboticsSettings.MaxArcBladeParalysisHours,
				1f,
				value => "CK.Robotics.Hours".Loc(value.ToString("0.#")));
			data.mkvArcBladeChainChance = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeChainChance".Loc(),
				data.mkvArcBladeChainChance,
				RoboticsSettings.MinArcBladeChance,
				RoboticsSettings.MaxArcBladeChance,
				0.05f,
				value => value.ToStringPercent());
			data.mkvArcBladeChainRadius = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvArcBladeChainRadius".Loc(),
				data.mkvArcBladeChainRadius,
				RoboticsSettings.MinArcBladeChainRadius,
				RoboticsSettings.MaxArcBladeChainRadius,
				1f,
				value => "CK.Robotics.Cells".Loc(value.ToString("0")));
			SettingsWidgets.Description(
				listing, "ColonyKit.Settings.Robotics.MkvArcBladeHint".Loc());
			// 面板数值改完立刻同步到 Def，武器信息卡和近战结算都读同一份数据。
			MkVArcBlade.ApplySettings();
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.ArcEmitter".Loc());
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
			SettingsWidgets.FrameEnd(listing, scope);
		}

		// ---------------------------------------------------------------- 充电

		private void DrawCharging(Listing_Standard listing, RoboticsSettings data)
		{
			SettingsWidgets.FrameScope scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.PassiveCharging".Loc());
			data.mkvSolarCharging = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkVSolarCharging".Loc(),
				ref data.mkvSolarCharging,
				"ColonyKit.Settings.Robotics.MkVSolarCharging.Desc".Loc());
			data.mkvWindCharging = SettingsWidgets.ToggleRow(
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
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.ActiveCharging".Loc());
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
			SettingsWidgets.FrameEnd(listing, scope);
		}

		// ------------------------------------------------------- 区域与维护

		private void DrawAreaAndMaintenance(Listing_Standard listing, RoboticsSettings data)
		{
			SettingsWidgets.FrameScope scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Area".Loc());
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
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Durability".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.DurabilityHint".Loc());
			data.mkvSelfRepair = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.MkvSelfRepair".Loc(),
				ref data.mkvSelfRepair,
				"ColonyKit.Settings.Robotics.MkvSelfRepair.Desc".Loc(),
				tag: "Mk-V");
			data.mkvSelfRepairRate = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.MkvSelfRepairRate".Loc(),
				data.mkvSelfRepairRate,
				RoboticsSettings.MinMkvSelfRepairRate,
				RoboticsSettings.MaxMkvSelfRepairRate,
				5f,
				value => "CK.Robotics.DurabilityPerDay".Loc(value.ToString("0")));
			SettingsWidgets.Description(
				listing, "ColonyKit.Settings.Robotics.MkvSelfRepairRate.Desc".Loc());
			data.robotBedRest = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.RobotBedRest".Loc(),
				ref data.robotBedRest,
				"ColonyKit.Settings.Robotics.RobotBedRest.Desc".Loc());
			data.robotBedRestRate = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.RobotBedRestRate".Loc(),
				data.robotBedRestRate,
				RoboticsSettings.MinRobotBedRestRate,
				RoboticsSettings.MaxRobotBedRestRate,
				10f,
				value => "CK.Robotics.DurabilityPerDay".Loc(value.ToString("0")));
			SettingsWidgets.Description(
				listing, "ColonyKit.Settings.Robotics.RobotBedRestRate.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Appearance".Loc());
			data.sounds = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Sounds".Loc(),
				ref data.sounds,
				"ColonyKit.Settings.Robotics.Sounds.Desc".Loc());
			data.chargingGlow = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.ChargingGlow".Loc(),
				ref data.chargingGlow,
				"ColonyKit.Settings.Robotics.ChargingGlow.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, scope);
		}

		// ---------------------------------------------------------------- AI

		private void DrawAi(Listing_Standard listing, RoboticsSettings data)
		{
			SettingsWidgets.FrameScope scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Connection".Loc());
			data.aiEnabled = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Enabled".Loc(),
				ref data.aiEnabled,
				"ColonyKit.Settings.Robotics.Ai.Enabled.Desc".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.Ai.Hint".Loc());
			data.aiApiKey = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Robotics.Ai.ApiKey".Loc(),
				data.aiApiKey,
				200,
				"ColonyKit.Settings.Robotics.Ai.ApiKey.Desc".Loc());
			data.aiBaseUrl = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Robotics.Ai.BaseUrl".Loc(),
				data.aiBaseUrl,
				200,
				"ColonyKit.Settings.Robotics.Ai.BaseUrl.Desc".Loc());
			data.aiModel = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Model".Loc(),
				data.aiModel,
				64,
				"ColonyKit.Settings.Robotics.Ai.Model.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Request".Loc());
			data.aiInterval = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Interval".Loc(),
				data.aiInterval,
				RoboticsSettings.MinAiInterval,
				RoboticsSettings.MaxAiInterval,
				1f,
				value => value.ToString("0") + " s");
			data.aiTimeout = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Timeout".Loc(),
				data.aiTimeout,
				RoboticsSettings.MinAiTimeout,
				RoboticsSettings.MaxAiTimeout,
				1f,
				value => value.ToString("0") + " s");
			data.aiMaxTokens = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.Ai.MaxTokens".Loc(),
				data.aiMaxTokens,
				RoboticsSettings.MinAiMaxTokens,
				RoboticsSettings.MaxAiMaxTokens,
				16f,
				value => value.ToString("0")));
			data.aiTemperature = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Temperature".Loc(),
				data.aiTemperature,
				0f,
				RoboticsSettings.MaxAiTemperature,
				0.05f,
				value => value.ToString("0.00"));
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Behavior".Loc());
			data.aiAllowCombat = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Combat".Loc(),
				ref data.aiAllowCombat,
				"ColonyKit.Settings.Robotics.Ai.Combat.Desc".Loc());
			data.aiSpeech = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.Speech".Loc(),
				ref data.aiSpeech,
				"ColonyKit.Settings.Robotics.Ai.Speech.Desc".Loc());
			data.aiThoughtLog = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.ThoughtLog".Loc(),
				ref data.aiThoughtLog,
				"ColonyKit.Settings.Robotics.Ai.ThoughtLog.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, scope);

			scope = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Robotics.Group.Autonomy".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.Ai.SelfHint".Loc());
			data.aiRaidAdvance = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.RaidAdvance".Loc(),
				ref data.aiRaidAdvance,
				"ColonyKit.Settings.Robotics.Ai.RaidAdvance.Desc".Loc());
			data.aiSelfResearch = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.SelfResearch".Loc(),
				ref data.aiSelfResearch,
				"ColonyKit.Settings.Robotics.Ai.SelfResearch.Desc".Loc());
			data.aiSelfAssemble = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Robotics.Ai.SelfAssemble".Loc(),
				ref data.aiSelfAssemble,
				"ColonyKit.Settings.Robotics.Ai.SelfAssemble.Desc".Loc());
			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Robotics.Ai.Test".Loc()))
			{
				MkVAiService.TestConnection(data);
			}
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Robotics.Ai.Cost".Loc());
			SettingsWidgets.FrameEnd(listing, scope);
		}

		// ------------------------------------------------------------- 辅助

		/// <summary>用字段名开关家务：统一走 <see cref="SettingsWidgets.ToggleRow"/>。</summary>
		private static void Toggle(
			Listing_Standard listing,
			RoboticsSettings data,
			string key,
			string tag = null)
		{
			string label = ("ColonyKit.Settings.Robotics." + key).Loc();
			string descKey = "ColonyKit.Settings.Robotics." + key + ".Desc";
			string tooltip = descKey.CanTranslate() ? descKey.Loc() : null;
			bool value = GetField(data, key);
			SettingsWidgets.ToggleRow(listing, label, ref value, tooltip, tag: tag);
			SetField(data, key, value);
		}

		private static bool GetField(RoboticsSettings data, string key)
		{
			return key switch
			{
				"Sweep" => data.sweep,
				"Scrub" => data.scrub,
				"Haul" => data.haul,
				"Firefight" => data.firefight,
				"Butcher" => data.butcher,
				"CorpseDisposal" => data.corpseDisposal,
				"Refuel" => data.refuel,
				"FeedPrisoners" => data.feedPrisoners,
				"Hygiene" => data.hygiene,
				"RopeAnimals" => data.ropeAnimals,
				"Storage" => data.storage,
				"Rescue" => data.rescue,
				"Cook" => data.cook,
				"Doctor" => data.doctor,
				"Research" => data.research,
				"Crafting" => data.crafting,
				"Construct" => data.construct,
				"Water" => data.water,
				"Plants" => data.plants,
				"DangerAvoidance" => data.dangerAvoidance,
				_ => false
			};
		}

		private static void SetField(RoboticsSettings data, string key, bool value)
		{
			switch (key)
			{
				case "Sweep": data.sweep = value; break;
				case "Scrub": data.scrub = value; break;
				case "Haul": data.haul = value; break;
				case "Firefight": data.firefight = value; break;
				case "Butcher": data.butcher = value; break;
				case "CorpseDisposal": data.corpseDisposal = value; break;
				case "Refuel": data.refuel = value; break;
				case "FeedPrisoners": data.feedPrisoners = value; break;
				case "Hygiene": data.hygiene = value; break;
				case "RopeAnimals": data.ropeAnimals = value; break;
				case "Storage": data.storage = value; break;
				case "Rescue": data.rescue = value; break;
				case "Cook": data.cook = value; break;
				case "Doctor": data.doctor = value; break;
				case "Research": data.research = value; break;
				case "Crafting": data.crafting = value; break;
				case "Construct": data.construct = value; break;
				case "Water": data.water = value; break;
				case "Plants": data.plants = value; break;
				case "DangerAvoidance": data.dangerAvoidance = value; break;
			}
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


