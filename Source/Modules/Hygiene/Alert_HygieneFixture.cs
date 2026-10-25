using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>玩家卫生洁具断水、堵塞或电热水淋浴器断电时提示。</summary>
	public sealed class Alert_HygieneFixture : Alert
	{
		private readonly List<Thing> culprits = new List<Thing>();

		public Alert_HygieneFixture()
		{
			defaultPriority = AlertPriority.Medium;
			defaultLabel = "CK.Hygiene.Alert.FixtureBlocked".Translate();
		}

		public override AlertReport GetReport()
		{
			culprits.Clear();
			foreach (Map map in Find.Maps)
			{
				if (!map.IsPlayerHome)
				{
					continue;
				}
				foreach (Building_AssignableFixture fixture in map.listerBuildings
					.AllBuildingsColonistOfClass<Building_AssignableFixture>())
				{
					if (!fixture.HasWaterSupply || !fixture.Working().Accepted)
					{
						culprits.Add(fixture);
					}
				}
			}
			return AlertReport.CulpritsAre(culprits);
		}

		public override TaggedString GetExplanation()
		{
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < culprits.Count; i++)
			{
				Building_AssignableFixture fixture = culprits[i] as Building_AssignableFixture;
				if (fixture == null)
				{
					continue;
				}
				string reason = fixture.HasWaterSupply
					? fixture.Working().Reason.ToString()
					: "Nowater".Translate().ToString();
				builder.AppendLine("  - " + fixture.LabelShort + ": " + reason);
			}
			return "CK.Hygiene.Alert.FixtureBlockedDesc".Translate(
				builder.ToString().TrimEndNewlines());
		}
	}
}
