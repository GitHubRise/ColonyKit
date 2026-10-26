using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>扫地/清洁/卫生间污渍共用的找污渍逻辑。</summary>
	internal static class NannyBotCleanUtility
	{
		public static Job TryGiveJob(Pawn pawn, bool insideHome, bool bathroomOnly = false)
		{
			Filth filth = FindFilth(pawn, insideHome, bathroomOnly);
			if (filth == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCleanFilth, filth);
			job.ignoreForbidden = true;
			return job;
		}

		public static Filth FindFilth(Pawn pawn, bool insideHome, bool bathroomOnly = false)
		{
			Map map = pawn.Map;
			if (map == null)
			{
				return null;
			}

			Thing found = GenClosest.ClosestThing_Global_Reachable(
				pawn.Position,
				map,
				map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Filth)),
				PathEndMode.Touch,
				TraverseParms.For(pawn, Danger.Deadly),
				9999f,
				(Thing thing) => ValidFilth(pawn, thing, insideHome, bathroomOnly));

			return found as Filth;
		}

		private static bool IsBathroom(Filth filth)
		{
			Room room = filth.GetRoom();
			string role = room?.Role?.defName;
			return role == "CK_PrivateBathroom" || role == "CK_PublicBathroom";
		}

		private static bool ValidFilth(Pawn pawn, Thing thing, bool insideHome, bool bathroomOnly)
		{
			if (!(thing is Filth filth) || !filth.Spawned || filth.Destroyed)
			{
				return false;
			}
			if (filth.Position.Fogged(pawn.Map))
			{
				return false;
			}
			if (bathroomOnly && !IsBathroom(filth))
			{
				return false;
			}

			bool inHome = pawn.Map.areaManager.Home[filth.Position];
			if (inHome != insideHome)
			{
				return false;
			}
			if (!NannyBotUtility.IsWithinWorkArea(pawn, filth.Position))
			{
				return false;
			}
			if (!pawn.CanReserve(filth))
			{
				return false;
			}
			if (filth.IsForbidden(pawn))
			{
				return false;
			}
			return pawn.CanReach(filth, PathEndMode.Touch, Danger.Deadly);
		}
	}
}
