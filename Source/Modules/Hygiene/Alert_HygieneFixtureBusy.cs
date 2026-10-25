using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>淋浴器长时间被占用，或有人需要洗澡但没有空位时提示。</summary>
	public sealed class Alert_HygieneShowerBusy : Alert
	{
		private const int LongOccupiedTicks = 2500;

		private readonly List<Thing> culprits = new List<Thing>();
		private int waitingCount;

		public Alert_HygieneShowerBusy()
		{
			defaultPriority = AlertPriority.Medium;
			defaultLabel = "CK.Hygiene.Alert.ShowerBusy".Translate();
		}

		public override AlertReport GetReport()
		{
			culprits.Clear();
			waitingCount = 0;
			int now = Find.TickManager.TicksGame;
			foreach (Map map in Find.Maps)
			{
				if (!map.IsPlayerHome)
				{
					continue;
				}

				List<Building_Shower> showers = new List<Building_Shower>(
					map.listerBuildings.AllBuildingsColonistOfClass<Building_Shower>());
				for (int i = 0; i < showers.Count; i++)
				{
					Building_Shower shower = showers[i];
					if (shower.occupiedSinceTick >= 0
						&& now - shower.occupiedSinceTick >= LongOccupiedTicks)
					{
						culprits.Add(shower);
					}
				}

				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (!pawn.IsColonist
						|| pawn.Downed
						|| pawn.Dead
						|| pawn.InMentalState)
					{
						continue;
					}
					Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
					if (need == null || need.CurLevel >= 0.2f)
					{
						continue;
					}

					bool hasFreeShower = false;
					for (int i = 0; i < showers.Count; i++)
					{
						if (!showers[i].IsForbidden(pawn) && showers[i].CanUse(pawn))
						{
							hasFreeShower = true;
							break;
						}
					}
					if (hasFreeShower)
					{
						continue;
					}

					bool hasBath = false;
					foreach (Building_Bath bath in map.listerBuildings
						.AllBuildingsColonistOfClass<Building_Bath>())
					{
						if (!bath.IsForbidden(pawn) && bath.CanUse(pawn))
						{
							hasBath = true;
							break;
						}
					}
					if (!hasBath)
					{
						waitingCount++;
					}
				}
			}

			if (culprits.Count > 0)
			{
				return AlertReport.CulpritsAre(culprits);
			}
			return waitingCount > 0 ? AlertReport.Active : AlertReport.Inactive;
		}

		public override TaggedString GetExplanation()
		{
			StringBuilder builder = new StringBuilder();
			int now = Find.TickManager.TicksGame;
			for (int i = 0; i < culprits.Count; i++)
			{
				Building_Shower shower = culprits[i] as Building_Shower;
				if (shower == null || shower.occupiedSinceTick < 0)
				{
					continue;
				}
				float hours = (now - shower.occupiedSinceTick) / 2500f;
				builder.AppendLine("CK.Hygiene.Alert.ShowerBusyLine".Translate(
					shower.LabelShort,
					hours.ToString("0.0")));
			}
			return "CK.Hygiene.Alert.ShowerBusyDesc".Translate(
				waitingCount,
				builder.ToString().TrimEndNewlines());
		}
	}
}
