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
		public bool storage = true;
		public bool rescue = true;
		public bool dangerAvoidance = true;

		// 表现。
		public bool sounds = true;
		public bool chargingGlow = true;

		// 参数。
		public float chargeThreshold = DefaultChargeThreshold;
		public float refuelThreshold = DefaultRefuelThreshold;
		public int batteryCapacity = DefaultBatteryCapacity;
		public float workRadius = DefaultWorkRadius;
		public RobotAreaMode areaMode = RobotAreaMode.Unrestricted;
		public string allowedAreaLabel = "";

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
			Scribe_Values.Look(ref storage, "storage", true);
			Scribe_Values.Look(ref rescue, "rescue", true);
			Scribe_Values.Look(ref dangerAvoidance, "dangerAvoidance", true);
			Scribe_Values.Look(ref sounds, "sounds", true);
			Scribe_Values.Look(ref chargingGlow, "chargingGlow", true);
			Scribe_Values.Look(ref chargeThreshold, "chargeThreshold", DefaultChargeThreshold);
			Scribe_Values.Look(ref refuelThreshold, "refuelThreshold", DefaultRefuelThreshold);
			Scribe_Values.Look(ref batteryCapacity, "batteryCapacity", DefaultBatteryCapacity);
			Scribe_Values.Look(ref workRadius, "workRadius", DefaultWorkRadius);
			Scribe_Values.Look(ref areaMode, "areaMode", RobotAreaMode.Unrestricted);
			Scribe_Values.Look(ref allowedAreaLabel, "allowedAreaLabel", "");
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
			storage = true;
			rescue = true;
			dangerAvoidance = true;
			sounds = true;
			chargingGlow = true;
			chargeThreshold = DefaultChargeThreshold;
			refuelThreshold = DefaultRefuelThreshold;
			batteryCapacity = DefaultBatteryCapacity;
			workRadius = DefaultWorkRadius;
			areaMode = RobotAreaMode.Unrestricted;
			allowedAreaLabel = "";
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
