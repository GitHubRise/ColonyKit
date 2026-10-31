namespace ColonyKit.Modules.Robotics
{
	/// <summary>保姆机器人可排优先级的工作类型；顺序即工作页签列顺序。</summary>
	public enum RobotChore
	{
		Rescue = 0,
		Firefight = 1,
		Sweep = 2,
		Scrub = 3,
		Haul = 4,
		Butcher = 5,
		Refuel = 6,
		FeedPrisoners = 7,
		Hygiene = 8,
		RopeAnimals = 9,
		Cook = 10,
		Doctor = 11,
		Research = 12,
		CorpseDisposal = 13,
		Craft = 14
	}

	public static class RobotChores
	{
		public const int Count = 15;

		public static readonly RobotChore[] All =
		{
			RobotChore.Rescue,
			RobotChore.Firefight,
			RobotChore.Sweep,
			RobotChore.Scrub,
			RobotChore.Haul,
			RobotChore.Butcher,
			RobotChore.Refuel,
			RobotChore.FeedPrisoners,
			RobotChore.Hygiene,
			RobotChore.RopeAnimals,
			RobotChore.Cook,
			RobotChore.Doctor,
			RobotChore.Research,
			RobotChore.CorpseDisposal,
			RobotChore.Craft
		};

		public static string LabelKey(RobotChore chore)
		{
			return "CK.Robotics.Chore." + chore + ".Label";
		}

		public static string DescKey(RobotChore chore)
		{
			return "CK.Robotics.Chore." + chore + ".Desc";
		}

		public static int DefaultPriority(RobotChore chore)
		{
			switch (chore)
			{
				case RobotChore.Rescue:
					return 1;
				case RobotChore.Firefight:
					return 2;
				default:
					return 3;
			}
		}

		public static bool IsEnabledBySettings(RobotChore chore)
		{
			RoboticsSettings settings = NannyBotUtility.Settings;
			switch (chore)
			{
				case RobotChore.Rescue:
					return settings.rescue;
				case RobotChore.Firefight:
					return settings.firefight;
				case RobotChore.Sweep:
					return settings.sweep;
				case RobotChore.Scrub:
					return settings.scrub;
				case RobotChore.Haul:
					return settings.haul;
				case RobotChore.Butcher:
					return settings.butcher;
				case RobotChore.Refuel:
					return settings.refuel;
				case RobotChore.FeedPrisoners:
					return settings.feedPrisoners;
				case RobotChore.Hygiene:
					return settings.hygiene;
				case RobotChore.RopeAnimals:
					return settings.ropeAnimals;
				case RobotChore.Cook:
					return settings.cook;
				case RobotChore.Doctor:
					return settings.doctor;
				case RobotChore.Research:
					return settings.research;
				case RobotChore.CorpseDisposal:
					return settings.corpseDisposal;
				case RobotChore.Craft:
					return settings.crafting;
				default:
					return false;
			}
		}
	}
}
