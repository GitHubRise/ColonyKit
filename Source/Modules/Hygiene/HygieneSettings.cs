using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>检测到 Dubs Bad Hygiene 时的处理策略。</summary>
	public enum HygieneDbhPolicy
	{
		/// <summary>检测到 DBH 时停用本模块的需求与自动工作；没装 DBH 时正常运行。</summary>
		SoftOff = 0,

		/// <summary>无视 DBH，始终运行本模块；两套卫生需求会同时存在。</summary>
		AlwaysOn = 1,

		/// <summary>完全停用本模块的需求与自动工作。</summary>
		Disabled = 2
	}

	/// <summary>
	/// 卫生模块设置：需求速率、耗水倍率、污染概率、DBH 策略与需求豁免。
	/// 由 <see cref="ColonyKitSettings.moduleSettings"/> 统一持久化。
	/// </summary>
	public class HygieneSettings : IColonyKitModuleSettings
	{
		public const float MinRate = 0f;
		public const float MaxRate = 5f;
		public const float MinWaterUse = 0.25f;
		public const float MaxWaterUse = 4f;
		public const float MinHotWaterUse = 0.1f;
		public const float MaxHotWaterUse = 2f;
		public const float MinContamination = 0f;
		public const float MaxContamination = 3f;
		public const float MinInviteChance = 0f;
		public const float MaxInviteChance = 1f;
		public const float DefaultInviteChance = 0.08f;
		public const float MinFertilizerStrength = 1f;
		public const float MaxFertilizerStrength = 2.5f;
		public const float MinFertilizerLifespan = 5f;
		public const float MaxFertilizerLifespan = 240f;
		public const float MinIrrigationStrength = 1f;
		public const float MaxIrrigationStrength = 3f;

		private const string DefaultExemptBodies = "HumanoidTerminator,AIRobot,AIPawn";
		private static HygieneSettings fallback;

		public string SettingsKey => "hygiene";

		/// <summary>卫生需求下降速度倍率；0 为不下降。</summary>
		public float hygieneRateMultiplier = 1f;

		/// <summary>膀胱需求下降速度倍率；0 为不下降。</summary>
		public float bladderRateMultiplier = 1f;

		/// <summary>淋浴/浴缸/洗手池/马桶的耗水倍率。</summary>
		public float waterUseMultiplier = 1f;

		/// <summary>热水消耗倍率，对应 DBH 的 WaterHeatingRate。</summary>
		public float hotWaterUseMultiplier = 1f;

		/// <summary>脏水造成污染的概率倍率；0 为关闭，1 为原版默认。</summary>
		public float contaminationChanceMultiplier = 1f;

		/// <summary>泡澡时邀请一名异性共用双人浴缸的概率；0 关闭。</summary>
		public float doubleBathInviteChance = DefaultInviteChance;

		/// <summary>施肥后的肥力倍率（DBH 默认 110%）。</summary>
		public float fertilizerStrength = 1.1f;

		/// <summary>一次施肥能维持的天数（DBH 默认 60 天）。</summary>
		public float fertilizerLifespanDays = 60f;

		/// <summary>灌溉满格时的肥力倍率（DBH 默认 180%）。</summary>
		public float irrigationStrength = 1.8f;

		/// <summary>检测到 DBH 时的处理策略。</summary>
		public HygieneDbhPolicy dbhPolicy = HygieneDbhPolicy.SoftOff;

		public bool prisonersGetHygiene = true;

		public bool guestsGetHygiene = true;

		public bool allowNonHuman = true;

		/// <summary>豁免的种族 defName，逗号/分号分隔。</summary>
		public string exemptRaces = string.Empty;

		/// <summary>豁免的体型 defName，逗号/分号分隔。</summary>
		public string exemptBodies = DefaultExemptBodies;

		/// <summary>豁免的 Hediff defName，逗号/分号分隔。</summary>
		public string exemptHediffs = string.Empty;

		public static HygieneSettings Current
		{
			get
			{
				HygieneSettings settings = ColonyKitMod.Settings?.GetModuleSettings<HygieneSettings>();
				if (settings != null)
				{
					return settings;
				}
				return fallback ?? (fallback = new HygieneSettings());
			}
		}

		public bool DbhDetected => ModsConfig.IsActive("dubwise.dubsbadhygiene");

		/// <summary>当前策略+模块开关是否允许本模块运行。</summary>
		public bool ShouldRunHygiene
		{
			get
			{
				if (!ModuleRegistry.IsEnabled("hygiene"))
				{
					return false;
				}
				switch (dbhPolicy)
				{
					case HygieneDbhPolicy.Disabled:
						return false;
					case HygieneDbhPolicy.AlwaysOn:
						return true;
					default:
						return !DbhDetected;
				}
			}
		}

		/// <summary>需求注入的统一判断：策略、模块开关、人形/机械体、智力与豁免。</summary>
		public bool ShouldInject(Pawn pawn, NeedDef def)
		{
			if (!ShouldRunHygiene || pawn?.needs == null || def == null)
			{
				return false;
			}
			if (!pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
			{
				return false;
			}
			if ((int)pawn.RaceProps.intelligence < (int)def.minIntelligence)
			{
				return false;
			}
			return !IsExempt(pawn);
		}

		public bool IsExempt(Pawn pawn)
		{
			if (pawn == null)
			{
				return true;
			}
			if (pawn.IsPrisoner && !prisonersGetHygiene)
			{
				return true;
			}
			if (IsGuest(pawn) && !guestsGetHygiene)
			{
				return true;
			}
			if (!allowNonHuman
				&& pawn.def?.race?.body != null
				&& pawn.def.race.body != BodyDefOf.Human
				&& !pawn.RaceProps.Animal)
			{
				return true;
			}
			if (ContainsName(exemptRaces, pawn.def?.defName))
			{
				return true;
			}
			if (ContainsName(exemptBodies, pawn.def?.race?.body?.defName))
			{
				return true;
			}
			if (!exemptHediffs.NullOrEmpty() && pawn.health?.hediffSet != null)
			{
				foreach (string hediffName in SplitNames(exemptHediffs))
				{
					HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
					if (hediff != null && pawn.health.hediffSet.HasHediff(hediff))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsGuest(Pawn pawn)
		{
			if (pawn.IsColonist || pawn.IsPrisonerOfColony || pawn.IsSlave)
			{
				return false;
			}
			if (pawn.IsQuestLodger())
			{
				return true;
			}
			return pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayerSilentFail;
		}

		private static bool ContainsName(string list, string name)
		{
			if (name.NullOrEmpty())
			{
				return false;
			}
			foreach (string item in SplitNames(list))
			{
				if (string.Equals(item, name, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		private static IEnumerable<string> SplitNames(string text)
		{
			if (text.NullOrEmpty())
			{
				yield break;
			}
			string[] parts = text.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				string part = parts[i].Trim();
				if (!part.NullOrEmpty())
				{
					yield return part;
				}
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref hygieneRateMultiplier, "hygieneRateMultiplier", 1f);
			Scribe_Values.Look(ref bladderRateMultiplier, "bladderRateMultiplier", 1f);
			Scribe_Values.Look(ref waterUseMultiplier, "waterUseMultiplier", 1f);
			Scribe_Values.Look(ref hotWaterUseMultiplier, "hotWaterUseMultiplier", 1f);
			Scribe_Values.Look(ref contaminationChanceMultiplier, "contaminationChanceMultiplier", 1f);
			Scribe_Values.Look(ref doubleBathInviteChance, "doubleBathInviteChance", DefaultInviteChance);
			Scribe_Values.Look(ref fertilizerStrength, "fertilizerStrength", 1.1f);
			Scribe_Values.Look(ref fertilizerLifespanDays, "fertilizerLifespanDays", 60f);
			Scribe_Values.Look(ref irrigationStrength, "irrigationStrength", 1.8f);
			Scribe_Values.Look(ref dbhPolicy, "dbhPolicy", HygieneDbhPolicy.SoftOff);
			Scribe_Values.Look(ref prisonersGetHygiene, "prisonersGetHygiene", true);
			Scribe_Values.Look(ref guestsGetHygiene, "guestsGetHygiene", true);
			Scribe_Values.Look(ref allowNonHuman, "allowNonHuman", true);
			Scribe_Values.Look(ref exemptRaces, "exemptRaces", string.Empty);
			Scribe_Values.Look(ref exemptBodies, "exemptBodies", DefaultExemptBodies);
			Scribe_Values.Look(ref exemptHediffs, "exemptHediffs", string.Empty);
		}

		public bool Validate()
		{
			bool changed = false;
			changed |= Clamp(ref hygieneRateMultiplier, MinRate, MaxRate, 1f);
			changed |= Clamp(ref bladderRateMultiplier, MinRate, MaxRate, 1f);
			changed |= Clamp(ref waterUseMultiplier, MinWaterUse, MaxWaterUse, 1f);
			changed |= Clamp(ref hotWaterUseMultiplier, MinHotWaterUse, MaxHotWaterUse, 1f);
			changed |= Clamp(ref contaminationChanceMultiplier, MinContamination, MaxContamination, 1f);
			changed |= Clamp(ref doubleBathInviteChance, MinInviteChance, MaxInviteChance, DefaultInviteChance);
			changed |= Clamp(ref fertilizerStrength, MinFertilizerStrength, MaxFertilizerStrength, 1.1f);
			changed |= Clamp(ref fertilizerLifespanDays, MinFertilizerLifespan, MaxFertilizerLifespan, 60f);
			changed |= Clamp(ref irrigationStrength, MinIrrigationStrength, MaxIrrigationStrength, 1.8f);

			if (dbhPolicy != HygieneDbhPolicy.SoftOff
				&& dbhPolicy != HygieneDbhPolicy.AlwaysOn
				&& dbhPolicy != HygieneDbhPolicy.Disabled)
			{
				dbhPolicy = HygieneDbhPolicy.SoftOff;
				changed = true;
			}
			if (exemptRaces == null)
			{
				exemptRaces = string.Empty;
				changed = true;
			}
			if (exemptBodies == null)
			{
				exemptBodies = DefaultExemptBodies;
				changed = true;
			}
			if (exemptHediffs == null)
			{
				exemptHediffs = string.Empty;
				changed = true;
			}
			if (changed)
			{
				ModLog.Warn("Hygiene", "invalid hygiene settings corrected.");
			}
			return changed;
		}

		private static bool Clamp(ref float value, float min, float max, float defaultValue)
		{
			if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
			{
				value = Mathf.Clamp(float.IsNaN(value) || float.IsInfinity(value) ? defaultValue : value, min, max);
				return true;
			}
			return false;
		}

		public void Reset()
		{
			hygieneRateMultiplier = 1f;
			bladderRateMultiplier = 1f;
			waterUseMultiplier = 1f;
			hotWaterUseMultiplier = 1f;
			contaminationChanceMultiplier = 1f;
			doubleBathInviteChance = DefaultInviteChance;
			fertilizerStrength = 1.1f;
			fertilizerLifespanDays = 60f;
			irrigationStrength = 1.8f;
			dbhPolicy = HygieneDbhPolicy.SoftOff;
			prisonersGetHygiene = true;
			guestsGetHygiene = true;
			allowNonHuman = true;
			exemptRaces = string.Empty;
			exemptBodies = DefaultExemptBodies;
			exemptHediffs = string.Empty;
		}
	}
}
