using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>玩家淋浴器断水、堵塞或电热水淋浴器断电时提示。</summary>
	public sealed class Alert_HygieneShower : Alert
	{
		private readonly List<Thing> culprits = new List<Thing>();

		public Alert_HygieneShower()
		{
			defaultPriority = AlertPriority.Medium;
			defaultLabel = "CK.Hygiene.Alert.ShowerBlocked".Translate();
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
				foreach (Building_Shower shower in map.listerBuildings
					.AllBuildingsColonistOfClass<Building_Shower>())
				{
					if (!shower.HasWaterSupply || !shower.Working().Accepted)
					{
						culprits.Add(shower);
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
				Building_Shower shower = culprits[i] as Building_Shower;
				if (shower == null)
				{
					continue;
				}
				string reason = shower.HasWaterSupply
					? shower.Working().Reason.ToString()
					: "Nowater".Translate().ToString();
				builder.AppendLine("  - " + shower.LabelShort + ": " + reason);
			}
			return "CK.Hygiene.Alert.ShowerBlockedDesc".Translate(
				builder.ToString().TrimEndNewlines());
		}
	}
}
