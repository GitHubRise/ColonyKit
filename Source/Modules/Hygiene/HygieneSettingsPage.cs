using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>卫生设置页：需求速率、耗水、污染、DBH 策略与需求豁免。</summary>
	public sealed class HygieneSettingsPage : SettingsPage
	{
		private static readonly HygieneDbhPolicy[] DbhPolicies =
		{
			HygieneDbhPolicy.SoftOff,
			HygieneDbhPolicy.AlwaysOn,
			HygieneDbhPolicy.Disabled
		};

		public override string Id => "hygiene";

		public override string TitleKey => "ColonyKit.Settings.Tab.Hygiene";

		public override int Order => 20;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			HygieneSettings data = settings.GetModuleSettings<HygieneSettings>();

			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Hint".Loc());
			listing.Gap();

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Needs".Loc());
			data.hygieneRateMultiplier = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.NeedsRate".Loc(),
				data.hygieneRateMultiplier,
				HygieneSettings.MinRate,
				HygieneSettings.MaxRate,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.NeedsRate.Desc".Loc());

			data.bladderRateMultiplier = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.BladderRate".Loc(),
				data.bladderRateMultiplier,
				HygieneSettings.MinRate,
				HygieneSettings.MaxRate,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.BladderRate.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Water".Loc());
			data.waterUseMultiplier = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.WaterUse".Loc(),
				data.waterUseMultiplier,
				HygieneSettings.MinWaterUse,
				HygieneSettings.MaxWaterUse,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.WaterUse.Desc".Loc());

			data.hotWaterUseMultiplier = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.HotWaterUse".Loc(),
				data.hotWaterUseMultiplier,
				HygieneSettings.MinHotWaterUse,
				HygieneSettings.MaxHotWaterUse,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.HotWaterUse.Desc".Loc());

			data.contaminationChanceMultiplier = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Contamination".Loc(),
				data.contaminationChanceMultiplier,
				HygieneSettings.MinContamination,
				HygieneSettings.MaxContamination,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Contamination.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.BottledWater".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.BottledWater.Hint".Loc());
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Hygiene.Water.Mineral".Loc());
			data.mineralWaterThirst = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Thirst".Loc(),
				data.mineralWaterThirst,
				0f,
				1f,
				0.01f,
				value => value.ToString("0.00"));
			data.mineralWaterSatiety = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Satiety".Loc(),
				data.mineralWaterSatiety,
				0f,
				0.2f,
				0.005f,
				value => value.ToString("0.000"));
			data.mineralWaterCanPoison = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Hygiene.Water.MineralPoison".Loc(),
				ref data.mineralWaterCanPoison,
				"ColonyKit.Settings.Hygiene.Water.MineralPoison.Desc".Loc());

			SettingsWidgets.Label(listing, "ColonyKit.Settings.Hygiene.Water.Purified".Loc());
			data.purifiedWaterThirst = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Thirst".Loc(),
				data.purifiedWaterThirst,
				0f,
				1f,
				0.01f,
				value => value.ToString("0.00"));
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

			SettingsWidgets.Label(listing, "ColonyKit.Settings.Hygiene.Water.Boiled".Loc());
			data.boiledWaterThirst = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.Water.Thirst".Loc(),
				data.boiledWaterThirst,
				0f,
				1f,
				0.01f,
				value => value.ToString("0.00"));
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

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Sewage".Loc());
			data.pumpCapacityMultiplier = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.PumpCapacity".Loc(),
				data.pumpCapacityMultiplier,
				HygieneSettings.MinPumpCapacity,
				HygieneSettings.MaxPumpCapacity,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.PumpCapacity.Desc".Loc());

			data.sewageGridCapacity = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.SewageGrid".Loc(),
				data.sewageGridCapacity,
				HygieneSettings.MinSewageGrid,
				HygieneSettings.MaxSewageGrid,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.SewageGrid.Desc".Loc());

			data.sewageCleanupRate = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.SewageCleanup".Loc(),
				data.sewageCleanupRate,
				HygieneSettings.MinSewageCleanup,
				HygieneSettings.MaxSewageCleanup,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.SewageCleanup.Desc".Loc());

			data.sewageProcRate = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.SewageProc".Loc(),
				data.sewageProcRate,
				HygieneSettings.MinSewageProc,
				HygieneSettings.MaxSewageProc,
				0.05f,
				value => "×" + value.ToString("0.00"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.SewageProc.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Cleaning".Loc());
			data.priorityIndoorCleaning = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Hygiene.PriorityIndoorCleaning".Loc(),
				ref data.priorityIndoorCleaning,
				"ColonyKit.Settings.Hygiene.PriorityIndoorCleaning.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Agriculture".Loc());
			data.terrainFertilityFactor = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.TerrainFertility".Loc(),
				data.terrainFertilityFactor,
				HygieneSettings.MinTerrainFertility,
				HygieneSettings.MaxTerrainFertility,
				0.01f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.TerrainFertility.Desc".Loc());

			data.showAgricultureOverlay = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Hygiene.ShowOverlay".Loc(),
				ref data.showAgricultureOverlay,
				"ColonyKit.Settings.Hygiene.ShowOverlay.Desc".Loc());
			data.fertilizerStrength = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerStrength".Loc(),
				data.fertilizerStrength,
				HygieneSettings.MinFertilizerStrength,
				HygieneSettings.MaxFertilizerStrength,
				0.01f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.FertilizerStrength.Desc".Loc());

			data.fertilizerMinFertility = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerFloor".Loc(),
				data.fertilizerMinFertility,
				HygieneSettings.MinFertilizerFloor,
				HygieneSettings.MaxFertilizerFloor,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.FertilizerFloor.Desc".Loc());

			data.fertilizerMaxFertility = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerCap".Loc(),
				data.fertilizerMaxFertility,
				HygieneSettings.MinFertilizerCap,
				HygieneSettings.MaxFertilizerCap,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.FertilizerCap.Desc".Loc());

			data.fertilizerLifespanDays = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.FertilizerLifespan".Loc(),
				data.fertilizerLifespanDays,
				HygieneSettings.MinFertilizerLifespan,
				HygieneSettings.MaxFertilizerLifespan,
				5f,
				value => value.ToString("0") + " d");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.FertilizerLifespan.Desc".Loc());

			data.irrigationStrength = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.IrrigationStrength".Loc(),
				data.irrigationStrength,
				HygieneSettings.MinIrrigationStrength,
				HygieneSettings.MaxIrrigationStrength,
				0.05f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.IrrigationStrength.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Bath".Loc());
			data.doubleBathInviteChance = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Hygiene.InviteChance".Loc(),
				data.doubleBathInviteChance,
				HygieneSettings.MinInviteChance,
				HygieneSettings.MaxInviteChance,
				0.01f,
				value => (value * 100f).ToString("0") + "%");
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.InviteChance.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Dbh".Loc());
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

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Hygiene.Section.Exemptions".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Hygiene.Exemptions.Hint".Loc());

			bool beforePrisoners = data.prisonersGetHygiene;
			data.prisonersGetHygiene = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Hygiene.Prisoners".Loc(),
				ref data.prisonersGetHygiene,
				"ColonyKit.Settings.Hygiene.Prisoners.Desc".Loc());

			bool beforeGuests = data.guestsGetHygiene;
			data.guestsGetHygiene = SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Hygiene.Guests".Loc(),
				ref data.guestsGetHygiene,
				"ColonyKit.Settings.Hygiene.Guests.Desc".Loc());

			bool beforeNonHuman = data.allowNonHuman;
			data.allowNonHuman = SettingsWidgets.Checkbox(
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

			listing.Gap();
			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Hygiene.ApplyExemptions".Loc()))
			{
				HygieneUtility.RefreshAllPawns();
				Messages.Message(
					"ColonyKit.Settings.Hygiene.Applied".Loc(),
					MessageTypeDefOf.NeutralEvent,
					false);
			}
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
