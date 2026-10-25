using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>卫生洁具长时间被占用，或有人需要洗澡/上厕所但没有空位时提示。</summary>
	public sealed class Alert_HygieneFixtureBusy : Alert
	{
		private const int LongOccupiedTicks = 2500;

		private readonly List<Thing> culprits = new List<Thing>();
		private int waitingCount;

		public Alert_HygieneFixtureBusy()
		{
			defaultPriority = AlertPriority.Medium;
			defaultLabel = "CK.Hygiene.Alert.FixtureBusy".Translate();
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

				List<Building_AssignableFixture> fixtures =
					new List<Building_AssignableFixture>(
						map.listerBuildings.AllBuildingsColonistOfClass<Building_AssignableFixture>());
				for (int i = 0; i < fixtures.Count; i++)
				{
					Building_AssignableFixture fixture = fixtures[i];
					if (fixture.occupiedSinceTick >= 0
						&& now - fixture.occupiedSinceTick >= LongOccupiedTicks)
					{
						culprits.Add(fixture);
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

					Need_Bladder bladder = pawn.needs?.TryGetNeed<Need_Bladder>();
					if (bladder != null && bladder.CurLevel < 0.15f)
					{
						if (!HasFreeToilet(map, pawn))
						{
							waitingCount++;
						}
						continue;
					}

					Need_Hygiene hygiene = pawn.needs?.TryGetNeed<Need_Hygiene>();
					if (hygiene != null && hygiene.CurLevel < 0.2f
						&& !HasFreeWash(map, pawn))
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
				Building_AssignableFixture fixture = culprits[i] as Building_AssignableFixture;
				if (fixture == null || fixture.occupiedSinceTick < 0)
				{
					continue;
				}
				float hours = (now - fixture.occupiedSinceTick) / 2500f;
				builder.AppendLine("CK.Hygiene.Alert.FixtureBusyLine".Translate(
					fixture.LabelShort,
					hours.ToString("0.0")));
			}
			return "CK.Hygiene.Alert.FixtureBusyDesc".Translate(
				waitingCount,
				builder.ToString().TrimEndNewlines());
		}

		private static bool HasFreeWash(Map map, Pawn pawn)
		{
			foreach (Building_Shower shower in map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Shower>())
			{
				if (!shower.IsForbidden(pawn) && shower.CanUse(pawn))
				{
					return true;
				}
			}
			foreach (Building_Bath bath in map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Bath>())
			{
				if (!bath.IsForbidden(pawn) && bath.CanUse(pawn))
				{
					return true;
				}
			}
			return false;
		}

		private static bool HasFreeToilet(Map map, Pawn pawn)
		{
			foreach (Building_Toilet toilet in map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Toilet>())
			{
				if (!toilet.IsForbidden(pawn) && toilet.CanUse(pawn))
				{
					return true;
				}
			}
			return false;
		}
	}
}
