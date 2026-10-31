using Verse;

namespace ColonyKit.Modules.Robotics
{
	public enum RobotAreaMode
	{
		Unrestricted,
		Home,
		Custom
	}

	/// <summary>机器人模块设置：所有家务开关、阈值、电池容量、活动范围。</summary>
	public class RoboticsSettings : IColonyKitModuleSettings
	{
		public const int MinBatteryCapacity = 1000;
		public const int MaxBatteryCapacity = 20000;
		public const int DefaultBatteryCapacity = 4000;

		public const float MinChargeThreshold = 0.05f;
		public const float MaxChargeThreshold = 0.95f;
		public const float DefaultChargeThreshold = 0.25f;

		public const float MinRefuelThreshold = 0.05f;
		public const float MaxRefuelThreshold = 0.95f;
		public const float DefaultRefuelThreshold = 0.30f;

		public const float MinWorkRadius = 0f;
		public const float MaxWorkRadius = 200f;
		public const float DefaultWorkRadius = 40f;

		public const float MinEngageRange = 1f;
		public const float MaxEngageRange = 65f;
		public const float DefaultEngageRange = 45f;
		public const int MinMkvBurstShotCount = 1;
		public const int MaxMkvBurstShotCount = 8;
		public const int DefaultMkvBurstShotCount = 5;
		public const float MinMkvBurstCooldown = 0.4f;
		public const float MaxMkvBurstCooldown = 3f;
		public const float DefaultMkvBurstCooldown = 1.4f;
		public const float MinRobotPowerEfficiency = 10f;
		public const float MaxRobotPowerEfficiency = 300f;
		public const float DefaultRobotPowerEfficiency = 150f;

		public const float MinReturnRange = 1f;
		public const float MaxReturnRange = 12f;
		public const float DefaultReturnRange = 2f;

		public const float MinWeaponPickupRadius = 4f;
		public const float MaxWeaponPickupRadius = 45f;
		public const float DefaultWeaponPickupRadius = 28f;

		public const float MinChargePower = 100f;
		public const float MaxChargePower = 10000f;
		public const float DefaultChargePower = 1000f;

		public const float MinChargeEfficiency = 10f;
		public const float MaxChargeEfficiency = 1000f;
		public const float DefaultChargeEfficiency = 100f;
		public const float MinPowerConversionRate = 10f;
		public const float MaxPowerConversionRate = 1000f;
		public const float DefaultPowerConversionRate = 100f;

		public const float MinMkvChargePower = 100f;
		public const float MaxMkvChargePower = 10000f;
		public const float DefaultMkvChargePower = 1000f;
		public const float MinMkvChargeEfficiency = 10f;
		public const float MaxMkvChargeEfficiency = 1000f;
		public const float DefaultMkvChargeEfficiency = 100f;
		public const float MinMkvPowerConversionRate = 10f;
		public const float MaxMkvPowerConversionRate = 1000f;
		public const float DefaultMkvPowerConversionRate = 100f;

		public const float MinMkvAttackSpeed = 0.5f;
		public const float MaxMkvAttackSpeed = 2f;
		public const float DefaultMkvAttackSpeed = 1f;

		public const float MinMkvHitChance = 0.5f;
		public const float MaxMkvHitChance = 1.5f;
		public const float DefaultMkvHitChance = 1f;

		// 电弧刃的命中特效。
		public const float MinArcBladeChance = 0f;
		public const float MaxArcBladeChance = 1f;
		public const float DefaultArcBladeParalysisChance = 0.5f;
		public const float DefaultArcBladeChainChance = 0.2f;
		public const float MinArcBladeParalysisHours = 1f;
		public const float MaxArcBladeParalysisHours = 24f;
		public const float DefaultArcBladeParalysisHours = 10f;
		public const float MinArcBladeChainRadius = 1f;
		public const float MaxArcBladeChainRadius = 10f;
		public const float DefaultArcBladeChainRadius = 5f;

		// 电弧刃的面板数值（伤害 / 攻击间隔 / 护甲穿透），运行时同步到 ThingDef.tools。
		public const float MinArcBladeDamage = 10f;
		public const float MaxArcBladeDamage = 150f;
		public const float DefaultArcBladeDamage = 50f;
		public const float MinArcBladeCooldown = 0.4f;
		public const float MaxArcBladeCooldown = 4f;
		public const float DefaultArcBladeCooldown = 1.3f;
		public const float MinArcBladeArmorPenetration = 0f;
		public const float MaxArcBladeArmorPenetration = 1f;
		public const float DefaultArcBladeArmorPenetration = 0.4f;

		public const float MinMkvIncomingDamageFactor = 0.1f;
		public const float MaxMkvIncomingDamageFactor = 5f;
		public const float DefaultMkvIncomingDamageFactor = 1f;

		public const float MinMkvInsulation = 0f;
		public const float MaxMkvInsulation = 100f;
		public const float DefaultMkvInsulationCold = 20f;
		public const float DefaultMkvInsulationHeat = 30f;

		public const float MinMkvSolarEfficiency = 10f;
		public const float MaxMkvSolarEfficiency = 1000f;
		public const float DefaultMkvSolarEfficiency = 100f;
		public const float MinMkvWindEfficiency = 10f;
		public const float MaxMkvWindEfficiency = 1000f;
		public const float DefaultMkvWindEfficiency = 100f;
		public const string DefaultMkvTexturePath = "Things/Pawn/MkV/MkV";
		public const float MinMkvDrawSize = 0.5f;
		public const float MaxMkvDrawSize = 6f;
		public const float DefaultMkvDrawSize = 2.2f;
		public const float MinMkvSkinScale = 0.5f;
		public const float MaxMkvSkinScale = 2f;
		public const float DefaultMkvSkinScale = 1f;

		public const float MinMkvLightThreshold = 0f;
		public const float MaxMkvLightThreshold = 1f;
		public const float DefaultMkvLightThreshold = 0.25f;
		public const float MinMkvWindThreshold = 0f;
		public const float MaxMkvWindThreshold = 1f;
		public const float DefaultMkvWindThreshold = 0.2f;

		public string SettingsKey => "robotics";

		// 家务总开关。
		public bool sweep = true;
		public bool scrub = true;
		public bool haul = true;
		public bool firefight = true;
		public bool butcher = true;
		public bool refuel = true;
		public bool feedPrisoners = true;
		public bool hygiene = true;
		public bool ropeAnimals = true;
		public bool storage = true;
		public bool rescue = true;
		public bool cook = true;
		public bool doctor = true;
		public bool research = true;
		public bool corpseDisposal = true;
		public bool crafting = true;
		public bool dangerAvoidance = true;

		// 表现。
		public bool sounds = true;
		public bool chargingGlow = true;

		// 参数。
		public float chargeThreshold = DefaultChargeThreshold;
		public float refuelThreshold = DefaultRefuelThreshold;
		public int batteryCapacity = DefaultBatteryCapacity;
		public float workRadius = DefaultWorkRadius;
		public float chargePower = DefaultChargePower;
		public float chargeEfficiency = DefaultChargeEfficiency;
		public float powerConversionRate = DefaultPowerConversionRate;
		public float mkvChargePower = DefaultMkvChargePower;
		public float mkvChargeEfficiency = DefaultMkvChargeEfficiency;
		public float mkvPowerConversionRate = DefaultMkvPowerConversionRate;
		public float mkvAttackSpeed = DefaultMkvAttackSpeed;
		public float mkvHitChance = DefaultMkvHitChance;
		public float mkvArcBladeParalysisChance = DefaultArcBladeParalysisChance;
		public float mkvArcBladeParalysisHours = DefaultArcBladeParalysisHours;
		public float mkvArcBladeChainChance = DefaultArcBladeChainChance;
		public float mkvArcBladeChainRadius = DefaultArcBladeChainRadius;
		public float mkvArcBladeDamage = DefaultArcBladeDamage;
		public float mkvArcBladeCooldown = DefaultArcBladeCooldown;
		public float mkvArcBladeArmorPenetration = DefaultArcBladeArmorPenetration;
		public float mkvIncomingDamageFactor = DefaultMkvIncomingDamageFactor;
		public float mkvInsulationCold = DefaultMkvInsulationCold;
		public float mkvInsulationHeat = DefaultMkvInsulationHeat;
		public bool mkvRandomTraits = false;
		public int mkvBurstShotCount = DefaultMkvBurstShotCount;
		public float mkvBurstCooldown = DefaultMkvBurstCooldown;
		public float robotPowerEfficiency = DefaultRobotPowerEfficiency;
		public bool mkvSolarCharging = true;
		public bool mkvWindCharging = true;
		public float mkvSolarEfficiency = DefaultMkvSolarEfficiency;
		public float mkvWindEfficiency = DefaultMkvWindEfficiency;
		public float mkvLightThreshold = DefaultMkvLightThreshold;
		public float mkvWindThreshold = DefaultMkvWindThreshold;
		public RobotAreaMode areaMode = RobotAreaMode.Unrestricted;
		public string allowedAreaLabel = "";

		// Mk-V 作战与专用组装舱。
		public bool startWithMkV = true;
		public bool mkvCombatEnabled = true;
		public bool mkvReturnToBay = true;
		public bool mkvAutoPickupWeapons = true;
		public bool mkvDualWield = true;
		public bool mkvBayAutoStart = false;
		public float mkvEngageRange = DefaultEngageRange;
		public float mkvReturnRange = DefaultReturnRange;
		public float mkvWeaponPickupRadius = DefaultWeaponPickupRadius;

		public void ExposeData()
		{
			Scribe_Values.Look(ref sweep, "sweep", true);
			Scribe_Values.Look(ref scrub, "scrub", true);
			Scribe_Values.Look(ref haul, "haul", true);
			Scribe_Values.Look(ref firefight, "firefight", true);
			Scribe_Values.Look(ref butcher, "butcher", true);
			Scribe_Values.Look(ref refuel, "refuel", true);
			Scribe_Values.Look(ref feedPrisoners, "feedPrisoners", true);
			Scribe_Values.Look(ref hygiene, "hygiene", true);
			Scribe_Values.Look(ref ropeAnimals, "ropeAnimals", true);
			Scribe_Values.Look(ref storage, "storage", true);
			Scribe_Values.Look(ref rescue, "rescue", true);
			Scribe_Values.Look(ref cook, "cook", true);
			Scribe_Values.Look(ref doctor, "doctor", true);
			Scribe_Values.Look(ref research, "research", true);
			Scribe_Values.Look(ref corpseDisposal, "corpseDisposal", true);
			Scribe_Values.Look(ref crafting, "crafting", true);
			Scribe_Values.Look(ref dangerAvoidance, "dangerAvoidance", true);
			Scribe_Values.Look(ref sounds, "sounds", true);
			Scribe_Values.Look(ref chargingGlow, "chargingGlow", true);
			Scribe_Values.Look(ref chargeThreshold, "chargeThreshold", DefaultChargeThreshold);
			Scribe_Values.Look(ref refuelThreshold, "refuelThreshold", DefaultRefuelThreshold);
			Scribe_Values.Look(ref batteryCapacity, "batteryCapacity", DefaultBatteryCapacity);
			Scribe_Values.Look(ref workRadius, "workRadius", DefaultWorkRadius);
			Scribe_Values.Look(ref chargePower, "chargePower", DefaultChargePower);
			Scribe_Values.Look(ref chargeEfficiency, "chargeEfficiency", DefaultChargeEfficiency);
			Scribe_Values.Look(ref powerConversionRate, "powerConversionRate", DefaultPowerConversionRate);
			Scribe_Values.Look(ref mkvChargePower, "mkvChargePower", DefaultMkvChargePower);
			Scribe_Values.Look(ref mkvChargeEfficiency, "mkvChargeEfficiency", DefaultMkvChargeEfficiency);
			Scribe_Values.Look(ref mkvPowerConversionRate, "mkvPowerConversionRate", DefaultMkvPowerConversionRate);
			Scribe_Values.Look(ref mkvAttackSpeed, "mkvAttackSpeed", DefaultMkvAttackSpeed);
			Scribe_Values.Look(ref mkvHitChance, "mkvHitChance", DefaultMkvHitChance);
			Scribe_Values.Look(ref mkvArcBladeParalysisChance, "mkvArcBladeParalysisChance", DefaultArcBladeParalysisChance);
			Scribe_Values.Look(ref mkvArcBladeParalysisHours, "mkvArcBladeParalysisHours", DefaultArcBladeParalysisHours);
			Scribe_Values.Look(ref mkvArcBladeChainChance, "mkvArcBladeChainChance", DefaultArcBladeChainChance);
			Scribe_Values.Look(ref mkvArcBladeChainRadius, "mkvArcBladeChainRadius", DefaultArcBladeChainRadius);
			Scribe_Values.Look(ref mkvArcBladeDamage, "mkvArcBladeDamage", DefaultArcBladeDamage);
			Scribe_Values.Look(ref mkvArcBladeCooldown, "mkvArcBladeCooldown", DefaultArcBladeCooldown);
			Scribe_Values.Look(ref mkvArcBladeArmorPenetration, "mkvArcBladeArmorPenetration", DefaultArcBladeArmorPenetration);
			Scribe_Values.Look(ref mkvIncomingDamageFactor, "mkvIncomingDamageFactor", DefaultMkvIncomingDamageFactor);
			Scribe_Values.Look(ref mkvInsulationCold, "mkvInsulationCold", DefaultMkvInsulationCold);
			Scribe_Values.Look(ref mkvInsulationHeat, "mkvInsulationHeat", DefaultMkvInsulationHeat);
			Scribe_Values.Look(ref mkvRandomTraits, "mkvRandomTraits", false);
			Scribe_Values.Look(ref mkvBurstShotCount, "mkvBurstShotCount", DefaultMkvBurstShotCount);
			Scribe_Values.Look(ref mkvBurstCooldown, "mkvBurstCooldown", DefaultMkvBurstCooldown);
			Scribe_Values.Look(ref robotPowerEfficiency, "robotPowerEfficiency", DefaultRobotPowerEfficiency);
			Scribe_Values.Look(ref mkvSolarCharging, "mkvSolarCharging", true);
			Scribe_Values.Look(ref mkvWindCharging, "mkvWindCharging", true);
			Scribe_Values.Look(ref mkvSolarEfficiency, "mkvSolarEfficiency", DefaultMkvSolarEfficiency);
			Scribe_Values.Look(ref mkvWindEfficiency, "mkvWindEfficiency", DefaultMkvWindEfficiency);
			Scribe_Values.Look(ref mkvLightThreshold, "mkvLightThreshold", DefaultMkvLightThreshold);
			Scribe_Values.Look(ref mkvWindThreshold, "mkvWindThreshold", DefaultMkvWindThreshold);
			Scribe_Values.Look(ref areaMode, "areaMode", RobotAreaMode.Unrestricted);
			Scribe_Values.Look(ref allowedAreaLabel, "allowedAreaLabel", "");
			Scribe_Values.Look(ref startWithMkV, "startWithMkV", true);
			Scribe_Values.Look(ref mkvCombatEnabled, "mkvCombatEnabled", true);
			Scribe_Values.Look(ref mkvReturnToBay, "mkvReturnToBay", true);
			Scribe_Values.Look(ref mkvAutoPickupWeapons, "mkvAutoPickupWeapons", true);
			Scribe_Values.Look(ref mkvDualWield, "mkvDualWield", true);
			Scribe_Values.Look(ref mkvBayAutoStart, "mkvBayAutoStart", false);
			Scribe_Values.Look(ref mkvEngageRange, "mkvEngageRange", DefaultEngageRange);
			Scribe_Values.Look(ref mkvReturnRange, "mkvReturnRange", DefaultReturnRange);
			Scribe_Values.Look(ref mkvWeaponPickupRadius, "mkvWeaponPickupRadius", DefaultWeaponPickupRadius);
		}

		public bool Validate()
		{
			bool changed = false;
			int battery = batteryCapacity;
			batteryCapacity = ClampInt(batteryCapacity, MinBatteryCapacity, MaxBatteryCapacity);
			changed |= battery != batteryCapacity;

			float charge = chargeThreshold;
			chargeThreshold = ClampFloat(chargeThreshold, MinChargeThreshold, MaxChargeThreshold);
			changed |= !Approximately(charge, chargeThreshold);

			float refuel = refuelThreshold;
			refuelThreshold = ClampFloat(refuelThreshold, MinRefuelThreshold, MaxRefuelThreshold);
			changed |= !Approximately(refuel, refuelThreshold);

			float radius = workRadius;
			workRadius = ClampFloat(workRadius, MinWorkRadius, MaxWorkRadius);
			changed |= !Approximately(radius, workRadius);

			float chargePowerValue = chargePower;
			chargePower = ClampFloat(chargePower, MinChargePower, MaxChargePower);
			changed |= !Approximately(chargePowerValue, chargePower);

			float chargeEfficiencyValue = chargeEfficiency;
			chargeEfficiency = ClampFloat(chargeEfficiency, MinChargeEfficiency, MaxChargeEfficiency);
			changed |= !Approximately(chargeEfficiencyValue, chargeEfficiency);

			float conversionValue = powerConversionRate;
			powerConversionRate = ClampFloat(powerConversionRate, MinPowerConversionRate, MaxPowerConversionRate);
			changed |= !Approximately(conversionValue, powerConversionRate);

			float mkvPowerValue = mkvChargePower;
			mkvChargePower = ClampFloat(mkvChargePower, MinMkvChargePower, MaxMkvChargePower);
			changed |= !Approximately(mkvPowerValue, mkvChargePower);

			float mkvEfficiencyValue = mkvChargeEfficiency;
			mkvChargeEfficiency = ClampFloat(mkvChargeEfficiency, MinMkvChargeEfficiency, MaxMkvChargeEfficiency);
			changed |= !Approximately(mkvEfficiencyValue, mkvChargeEfficiency);

			float mkvConversionValue = mkvPowerConversionRate;
			mkvPowerConversionRate = ClampFloat(mkvPowerConversionRate,
				MinMkvPowerConversionRate, MaxMkvPowerConversionRate);
			changed |= !Approximately(mkvConversionValue, mkvPowerConversionRate);

			float attackSpeed = mkvAttackSpeed;
			mkvAttackSpeed = ClampFloat(mkvAttackSpeed, MinMkvAttackSpeed, MaxMkvAttackSpeed);
			changed |= !Approximately(attackSpeed, mkvAttackSpeed);

			float hitChance = mkvHitChance;
			mkvHitChance = ClampFloat(mkvHitChance, MinMkvHitChance, MaxMkvHitChance);
			changed |= !Approximately(hitChance, mkvHitChance);

			float paralysisChance = mkvArcBladeParalysisChance;
			mkvArcBladeParalysisChance = ClampFloat(mkvArcBladeParalysisChance, MinArcBladeChance, MaxArcBladeChance);
			changed |= !Approximately(paralysisChance, mkvArcBladeParalysisChance);

			float paralysisHours = mkvArcBladeParalysisHours;
			mkvArcBladeParalysisHours = ClampFloat(mkvArcBladeParalysisHours,
				MinArcBladeParalysisHours, MaxArcBladeParalysisHours);
			changed |= !Approximately(paralysisHours, mkvArcBladeParalysisHours);

			float chainChance = mkvArcBladeChainChance;
			mkvArcBladeChainChance = ClampFloat(mkvArcBladeChainChance, MinArcBladeChance, MaxArcBladeChance);
			changed |= !Approximately(chainChance, mkvArcBladeChainChance);

			float chainRadius = mkvArcBladeChainRadius;
			mkvArcBladeChainRadius = ClampFloat(mkvArcBladeChainRadius,
				MinArcBladeChainRadius, MaxArcBladeChainRadius);
			changed |= !Approximately(chainRadius, mkvArcBladeChainRadius);

			float bladeDamage = mkvArcBladeDamage;
			mkvArcBladeDamage = ClampFloat(mkvArcBladeDamage, MinArcBladeDamage, MaxArcBladeDamage);
			changed |= !Approximately(bladeDamage, mkvArcBladeDamage);

			float bladeCooldown = mkvArcBladeCooldown;
			mkvArcBladeCooldown = ClampFloat(mkvArcBladeCooldown, MinArcBladeCooldown, MaxArcBladeCooldown);
			changed |= !Approximately(bladeCooldown, mkvArcBladeCooldown);

			float bladeAp = mkvArcBladeArmorPenetration;
			mkvArcBladeArmorPenetration = ClampFloat(mkvArcBladeArmorPenetration,
				MinArcBladeArmorPenetration, MaxArcBladeArmorPenetration);
			changed |= !Approximately(bladeAp, mkvArcBladeArmorPenetration);

			float incomingDamage = mkvIncomingDamageFactor;
			mkvIncomingDamageFactor = ClampFloat(mkvIncomingDamageFactor,
				MinMkvIncomingDamageFactor, MaxMkvIncomingDamageFactor);
			changed |= !Approximately(incomingDamage, mkvIncomingDamageFactor);

			float insulationCold = mkvInsulationCold;
			mkvInsulationCold = ClampFloat(mkvInsulationCold, MinMkvInsulation, MaxMkvInsulation);
			changed |= !Approximately(insulationCold, mkvInsulationCold);

			float insulationHeat = mkvInsulationHeat;
			mkvInsulationHeat = ClampFloat(mkvInsulationHeat, MinMkvInsulation, MaxMkvInsulation);
			changed |= !Approximately(insulationHeat, mkvInsulationHeat);

			int burst = mkvBurstShotCount;
			mkvBurstShotCount = ClampInt(mkvBurstShotCount, MinMkvBurstShotCount, MaxMkvBurstShotCount);
			changed |= burst != mkvBurstShotCount;

			float burstCooldown = mkvBurstCooldown;
			mkvBurstCooldown = ClampFloat(mkvBurstCooldown, MinMkvBurstCooldown, MaxMkvBurstCooldown);
			changed |= !Approximately(burstCooldown, mkvBurstCooldown);

			float powerEfficiency = robotPowerEfficiency;
			robotPowerEfficiency = ClampFloat(robotPowerEfficiency,
				MinRobotPowerEfficiency, MaxRobotPowerEfficiency);
			changed |= !Approximately(powerEfficiency, robotPowerEfficiency);



			float solar = mkvSolarEfficiency;
			mkvSolarEfficiency = ClampFloat(mkvSolarEfficiency, MinMkvSolarEfficiency, MaxMkvSolarEfficiency);
			changed |= !Approximately(solar, mkvSolarEfficiency);

			float wind = mkvWindEfficiency;
			mkvWindEfficiency = ClampFloat(mkvWindEfficiency, MinMkvWindEfficiency, MaxMkvWindEfficiency);
			changed |= !Approximately(wind, mkvWindEfficiency);

			float light = mkvLightThreshold;
			mkvLightThreshold = ClampFloat(mkvLightThreshold, MinMkvLightThreshold, MaxMkvLightThreshold);
			changed |= !Approximately(light, mkvLightThreshold);

			float windThreshold = mkvWindThreshold;
			mkvWindThreshold = ClampFloat(mkvWindThreshold, MinMkvWindThreshold, MaxMkvWindThreshold);
			changed |= !Approximately(windThreshold, mkvWindThreshold);

			float engage = mkvEngageRange;
			mkvEngageRange = ClampFloat(mkvEngageRange, MinEngageRange, MaxEngageRange);
			changed |= !Approximately(engage, mkvEngageRange);

			float returnRange = mkvReturnRange;
			mkvReturnRange = ClampFloat(mkvReturnRange, MinReturnRange, MaxReturnRange);
			changed |= !Approximately(returnRange, mkvReturnRange);

			float pickupRadius = mkvWeaponPickupRadius;
			mkvWeaponPickupRadius = ClampFloat(mkvWeaponPickupRadius, MinWeaponPickupRadius, MaxWeaponPickupRadius);
			changed |= !Approximately(pickupRadius, mkvWeaponPickupRadius);

			if (areaMode != RobotAreaMode.Unrestricted
				&& areaMode != RobotAreaMode.Home
				&& areaMode != RobotAreaMode.Custom)
			{
				areaMode = RobotAreaMode.Unrestricted;
				changed = true;
			}
			if (allowedAreaLabel == null)
			{
				allowedAreaLabel = "";
				changed = true;
			}
			return changed;
		}

		public void Reset()
		{
			sweep = true;
			scrub = true;
			haul = true;
			firefight = true;
			butcher = true;
			refuel = true;
			feedPrisoners = true;
			hygiene = true;
			ropeAnimals = true;
			storage = true;
			rescue = true;
			cook = true;
			doctor = true;
			research = true;
			corpseDisposal = true;
			crafting = true;
			dangerAvoidance = true;
			sounds = true;
			chargingGlow = true;
			chargeThreshold = DefaultChargeThreshold;
			refuelThreshold = DefaultRefuelThreshold;
			batteryCapacity = DefaultBatteryCapacity;
			workRadius = DefaultWorkRadius;
			chargePower = DefaultChargePower;
			chargeEfficiency = DefaultChargeEfficiency;
			powerConversionRate = DefaultPowerConversionRate;
			mkvChargePower = DefaultMkvChargePower;
			mkvChargeEfficiency = DefaultMkvChargeEfficiency;
			mkvPowerConversionRate = DefaultMkvPowerConversionRate;
			mkvAttackSpeed = DefaultMkvAttackSpeed;
			mkvHitChance = DefaultMkvHitChance;
			mkvArcBladeParalysisChance = DefaultArcBladeParalysisChance;
			mkvArcBladeParalysisHours = DefaultArcBladeParalysisHours;
			mkvArcBladeChainChance = DefaultArcBladeChainChance;
			mkvArcBladeChainRadius = DefaultArcBladeChainRadius;
			mkvArcBladeDamage = DefaultArcBladeDamage;
			mkvArcBladeCooldown = DefaultArcBladeCooldown;
			mkvArcBladeArmorPenetration = DefaultArcBladeArmorPenetration;
			mkvIncomingDamageFactor = DefaultMkvIncomingDamageFactor;
			mkvInsulationCold = DefaultMkvInsulationCold;
			mkvInsulationHeat = DefaultMkvInsulationHeat;
			mkvRandomTraits = false;
			mkvBurstShotCount = DefaultMkvBurstShotCount;
			mkvBurstCooldown = DefaultMkvBurstCooldown;
			robotPowerEfficiency = DefaultRobotPowerEfficiency;
			mkvSolarCharging = true;
			mkvWindCharging = true;
			mkvSolarEfficiency = DefaultMkvSolarEfficiency;
			mkvWindEfficiency = DefaultMkvWindEfficiency;
			mkvLightThreshold = DefaultMkvLightThreshold;
			mkvWindThreshold = DefaultMkvWindThreshold;
			areaMode = RobotAreaMode.Unrestricted;
			allowedAreaLabel = "";
			startWithMkV = true;
			mkvCombatEnabled = true;
			mkvReturnToBay = true;
			mkvAutoPickupWeapons = true;
			mkvDualWield = true;
			mkvBayAutoStart = false;
			mkvEngageRange = DefaultEngageRange;
			mkvReturnRange = DefaultReturnRange;
			mkvWeaponPickupRadius = DefaultWeaponPickupRadius;
		}

		private static int ClampInt(int value, int min, int max)
		{
			return value < min ? min : value > max ? max : value;
		}

		private static float ClampFloat(float value, float min, float max)
		{
			return value < min ? min : value > max ? max : value;
		}

		private static bool Approximately(float a, float b)
		{
			return UnityEngine.Mathf.Abs(a - b) < 0.0001f;
		}
	}
}
