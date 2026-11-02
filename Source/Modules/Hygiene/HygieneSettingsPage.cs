using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 卫生设置页：拆成 需求 / 水质与污染 / 瓶装水 / 污水 / 清洁与农业 / 兼容与豁免 六个子页签，
	/// 每组参数一个显式框，开关统一用行式 <see cref="SettingsWidgets.ToggleRow"/>。
	/// </summary>
	public sealed class HygieneSettingsPage : SettingsPage
	{
		private static readonly HygieneDbhPolicy[] DbhPolicies =
		{
			HygieneDbhPolicy.SoftOff,
			HygieneDbhPolicy.AlwaysOn,
			HygieneDbhPolicy.Disabled
		};

		private static readonly string[] SubTabs =
		{
			"ColonyKit.Settings.Hygiene.Sub.Needs",
			"ColonyKit.Settings.Hygiene.Sub.Water",
			"ColonyKit.Settings.Hygiene.Sub.Bottled",
			"ColonyKit.Settings.Hygiene.Sub.Sewage",
			"ColonyKit.Settings.Hygiene.Sub.CleaningFarming",
			"ColonyKit.Settings.Hygiene.Sub.Compat"
		};

		public override string Id => "hygiene";

		public override string TitleKey => "ColonyKit.Settings.Tab.Hygiene";

		public override int Order => 20;

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
			HygieneSettings data = settings.GetModuleSettings<HygieneSettings>();
			switch (subTab)
			{
				case 1:
					DrawWater(listing, data);
					break;
				case 2:
					DrawBottledWater(listing, data);
					break;
				case 3:
					DrawSewage(listing, data);
					break;
				case 4:
					DrawCleaningAndFarming(listing, data);
					break;
				case 5:
					DrawCompat(listing, data);
					break;
				default:
					DrawNeeds(listing, data);
					break;
			}
		}

		// ---------------------------------------------------------------- 需求

		private static void DrawNeeds(Listing_Standard listing, HygieneSettings data)
		{
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Hint".Loc());

			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Needs".Loc());
			data.hygieneRateMultiplier = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.NeedsRate",
				data.hygieneRateMultiplier,
				HygieneSettings.MinRate,
				HygieneSettings.MaxRate,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.bladderRateMultiplier = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.BladderRate",
				data.bladderRateMultiplier,
				HygieneSettings.MinRate,
				HygieneSettings.MaxRate,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.thirstHoursToEmpty = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.ThirstHours",
				data.thirstHoursToEmpty,
				HygieneSettings.MinThirstHours,
				HygieneSettings.MaxThirstHours,
				1f,
				value => value.ToString("0") + " h");
			data.animalThirstRate = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.AnimalThirst",
				data.animalThirstRate,
				HygieneSettings.MinAnimalThirstRate,
				HygieneSettings.MaxAnimalThirstRate,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.FrameEnd(listing, frame);
		}

		// ------------------------------------------------------- 水质与污染

		private static void DrawWater(Listing_Standard listing, HygieneSettings data)
		{
			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Water".Loc());
			data.foodWaterFactor = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FoodWater",
				data.foodWaterFactor,
				HygieneSettings.MinFoodWater,
				HygieneSettings.MaxFoodWater,
				0.01f,
				value => "×" + value.ToString("0.00"));
			data.herbivorePlantThirst = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.AnimalPlantThirst",
				data.herbivorePlantThirst,
				0f,
				HygieneSettings.MaxAnimalFoodThirst,
				0.01f,
				value => "+" + value.ToString("0.00"));
			data.carnivoreMeatThirst = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.AnimalMeatThirst",
				data.carnivoreMeatThirst,
				0f,
				HygieneSettings.MaxAnimalFoodThirst,
				0.01f,
				value => "+" + value.ToString("0.00"));
			data.waterUseMultiplier = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.WaterUse",
				data.waterUseMultiplier,
				HygieneSettings.MinWaterUse,
				HygieneSettings.MaxWaterUse,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.hotWaterUseMultiplier = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.HotWaterUse",
				data.hotWaterUseMultiplier,
				HygieneSettings.MinHotWaterUse,
				HygieneSettings.MaxHotWaterUse,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.contaminationChanceMultiplier = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Contamination",
				data.contaminationChanceMultiplier,
				HygieneSettings.MinContamination,
				HygieneSettings.MaxContamination,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.FrameEnd(listing, frame);
		}

		// ------------------------------------------------------------ 瓶装水

		private static void DrawBottledWater(Listing_Standard listing, HygieneSettings data)
		{
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.BottledWater.Hint".Loc());

			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Water.PerLiter".Loc());
			data.thirstPerLiter = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.PerLiter".Loc(),
				data.thirstPerLiter,
				HygieneSettings.MinThirstPerLiter,
				HygieneSettings.MaxThirstPerLiter,
				0.01f,
				value => value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Water.PerLiter.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Hygiene.Water.Mineral".Loc());
			data.mineralWaterSatiety = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Satiety".Loc(),
				data.mineralWaterSatiety,
				0f,
				0.2f,
				0.005f,
				value => value.ToString("0.000"));
			data.mineralWaterCanPoison = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.Water.MineralPoison".Loc(),
				ref data.mineralWaterCanPoison,
				"ColonyKit.Settings.Hygiene.Water.MineralPoison.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Hygiene.Water.Purified".Loc());
			data.purifiedWaterSatiety = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Satiety".Loc(),
				data.purifiedWaterSatiety,
				0f,
				0.2f,
				0.005f,
				value => value.ToString("0.000"));
			data.purifiedWaterMood = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Mood".Loc(),
				data.purifiedWaterMood,
				0f,
				20f,
				1f,
				value => "+" + value.ToString("0")));
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(listing, "ColonyKit.Settings.Hygiene.Water.Boiled".Loc());
			data.boiledWaterSatiety = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Satiety".Loc(),
				data.boiledWaterSatiety,
				0f,
				0.2f,
				0.005f,
				value => value.ToString("0.000"));
			data.boiledWaterMood = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Mood".Loc(),
				data.boiledWaterMood,
				0f,
				20f,
				1f,
				value => "+" + value.ToString("0")));
			int beforeBoiledWork = data.boiledWaterWorkTicks;
			data.boiledWaterWorkTicks = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.WorkTicks".Loc(),
				data.boiledWaterWorkTicks,
				20f,
				2000f,
				10f,
				value => value.ToString("0") + " tick"));
			if (data.boiledWaterWorkTicks != beforeBoiledWork)
			{
				HygieneUtility.ApplyWaterRecipeSettings();
			}
			SettingsWidgets.FrameEnd(listing, frame);
		}

		// -------------------------------------------------------------- 污水

		private static void DrawSewage(Listing_Standard listing, HygieneSettings data)
		{
			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Sewage".Loc());
			data.pumpCapacityMultiplier = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.PumpCapacity",
				data.pumpCapacityMultiplier,
				HygieneSettings.MinPumpCapacity,
				HygieneSettings.MaxPumpCapacity,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.groundWaterRequired = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.GroundWaterRequired".Loc(),
				ref data.groundWaterRequired,
				"ColonyKit.Settings.Hygiene.GroundWaterRequired.Desc".Loc());
			data.sewageGridCapacity = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.SewageGrid",
				data.sewageGridCapacity,
				HygieneSettings.MinSewageGrid,
				HygieneSettings.MaxSewageGrid,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.sewageCleanupRate = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.SewageCleanup",
				data.sewageCleanupRate,
				HygieneSettings.MinSewageCleanup,
				HygieneSettings.MaxSewageCleanup,
				0.05f,
				value => "×" + value.ToString("0.00"));
			data.sewageProcRate = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.SewageProc",
				data.sewageProcRate,
				HygieneSettings.MinSewageProc,
				HygieneSettings.MaxSewageProc,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.FrameEnd(listing, frame);
		}

		// ------------------------------------------------------- 清洁与农业

		private static void DrawCleaningAndFarming(Listing_Standard listing, HygieneSettings data)
		{
			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Cleaning".Loc());
			data.priorityIndoorCleaning = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.PriorityIndoorCleaning".Loc(),
				ref data.priorityIndoorCleaning,
				"ColonyKit.Settings.Hygiene.PriorityIndoorCleaning.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Agriculture".Loc());
			data.showAgricultureOverlay = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.ShowOverlay".Loc(),
				ref data.showAgricultureOverlay,
				"ColonyKit.Settings.Hygiene.ShowOverlay.Desc".Loc());
			data.terrainFertilityFactor = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.TerrainFertility",
				data.terrainFertilityFactor,
				HygieneSettings.MinTerrainFertility,
				HygieneSettings.MaxTerrainFertility,
				0.01f,
				value => (value * 100f).ToString("0") + "%");
			data.fertilizerStrength = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerStrength",
				data.fertilizerStrength,
				HygieneSettings.MinFertilizerStrength,
				HygieneSettings.MaxFertilizerStrength,
				0.01f,
				value => (value * 100f).ToString("0") + "%");
			data.fertilizerMinFertility = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerFloor",
				data.fertilizerMinFertility,
				HygieneSettings.MinFertilizerFloor,
				HygieneSettings.MaxFertilizerFloor,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			data.fertilizerMaxFertility = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerCap",
				data.fertilizerMaxFertility,
				HygieneSettings.MinFertilizerCap,
				HygieneSettings.MaxFertilizerCap,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			data.fertilizerLifespanDays = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerLifespan",
				data.fertilizerLifespanDays,
				HygieneSettings.MinFertilizerLifespan,
				HygieneSettings.MaxFertilizerLifespan,
				5f,
				value => value.ToString("0") + " d");
			data.irrigationStrength = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.IrrigationStrength",
				data.irrigationStrength,
				HygieneSettings.MinIrrigationStrength,
				HygieneSettings.MaxIrrigationStrength,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Bath".Loc());
			data.doubleBathInviteChance = Slider(
				listing,
				"ColonyKit.Settings.Hygiene.InviteChance",
				data.doubleBathInviteChance,
				HygieneSettings.MinInviteChance,
				HygieneSettings.MaxInviteChance,
				0.01f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.FrameEnd(listing, frame);
		}

		// ------------------------------------------------------- 兼容与豁免

		private static void DrawCompat(Listing_Standard listing, HygieneSettings data)
		{
			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Dbh".Loc());
			HygieneDbhPolicy policy = data.dbhPolicy;
			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Hygiene.Dbh.Policy".Loc(),
				policy,
				DbhPolicies,
				DbhPolicyLabel,
				value =>
				{
					data.dbhPolicy = value;
					HygieneUtility.RefreshAllPawns();
				});
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Dbh.Policy.Desc".Loc());
			SettingsWidgets.Label(
				listing,
				(data.DbhDetected
					? "ColonyKit.Settings.Hygiene.Dbh.Detected"
					: "ColonyKit.Settings.Hygiene.Dbh.NotDetected").Loc());
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Hygiene.Section.Exemptions".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Exemptions.Hint".Loc());

			bool beforePrisoners = data.prisonersGetHygiene;
			data.prisonersGetHygiene = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.Prisoners".Loc(),
				ref data.prisonersGetHygiene,
				"ColonyKit.Settings.Hygiene.Prisoners.Desc".Loc());
			bool beforeGuests = data.guestsGetHygiene;
			data.guestsGetHygiene = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.Guests".Loc(),
				ref data.guestsGetHygiene,
				"ColonyKit.Settings.Hygiene.Guests.Desc".Loc());
			bool beforeNonHuman = data.allowNonHuman;
			data.allowNonHuman = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Hygiene.NonHuman".Loc(),
				ref data.allowNonHuman,
				"ColonyKit.Settings.Hygiene.NonHuman.Desc".Loc());

			data.exemptRaces = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Hygiene.ExemptRaces".Loc(),
				data.exemptRaces,
				512,
				"ColonyKit.Settings.Hygiene.ExemptRaces.Desc".Loc());
			data.exemptBodies = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Hygiene.ExemptBodies".Loc(),
				data.exemptBodies,
				512,
				"ColonyKit.Settings.Hygiene.ExemptBodies.Desc".Loc());
			data.exemptHediffs = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Hygiene.ExemptHediffs".Loc(),
				data.exemptHediffs,
				512,
				"ColonyKit.Settings.Hygiene.ExemptHediffs.Desc".Loc());

			if (data.prisonersGetHygiene != beforePrisoners
				|| data.guestsGetHygiene != beforeGuests
				|| data.allowNonHuman != beforeNonHuman)
			{
				HygieneUtility.RefreshAllPawns();
			}

			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Hygiene.ApplyExemptions".Loc()))
			{
				HygieneUtility.RefreshAllPawns();
				Messages.Message(
					"ColonyKit.Settings.Hygiene.Applied".Loc(),
					MessageTypeDefOf.NeutralEvent,
					false);
			}
			SettingsWidgets.FrameEnd(listing, frame);
		}

		/// <summary>标签 + 描述（Desc 键可选）的滑块，减少重复代码。</summary>
		private static float Slider(
			Listing_Standard listing,
			string labelKey,
			float value,
			float min,
			float max,
			float step,
			System.Func<float, string> format)
		{
			float result = SettingsWidgets.Slider(
				listing, labelKey.Loc(), value, min, max, step, format);
			string descKey = labelKey + ".Desc";
			if (descKey.CanTranslate())
			{
				SettingsWidgets.Description(listing, descKey.Loc());
			}
			return result;
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.GetModuleSettings<HygieneSettings>().Reset();
			HygieneUtility.RefreshAllPawns();
		}

		private static string DbhPolicyLabel(HygieneDbhPolicy policy)
		{
			switch (policy)
			{
				case HygieneDbhPolicy.AlwaysOn:
					return "ColonyKit.Settings.Hygiene.Dbh.AlwaysOn".Loc();
				case HygieneDbhPolicy.Disabled:
					return "ColonyKit.Settings.Hygiene.Dbh.Disabled".Loc();
				default:
					return "ColonyKit.Settings.Hygiene.Dbh.SoftOff".Loc();
			}
		}
	}
}
