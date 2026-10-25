#nullable disable

﻿using Multiplayer.API;
using RimWorld;
using rjw.Modules.Shared.Logs;
using UnityEngine;
using Verse;

namespace rjw.Modules.Nymphs.Incidents
{
	public class IncidentWorker_NymphRaidEasy : IncidentWorker_BaseNymphRaid
	{
		private static ILog _log = LogManager.GetLogger<IncidentWorker_NymphRaidEasy, NymphLogProvider>();

		[SyncMethod]
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			//该事件已被禁用，不要触发！
			if (RJWSettings.NymphRaidEasy == false)
			{
				_log.Debug("The incident can't fire as it is disabled in RJW settings");
				return false;
			}

			return base.CanFireNowSub(parms);
		}
		protected override int GetNymphCount(IncidentParms parms, Map map)
		{
			int count;

			//计算花痴的数量
			if (RJWSettings.NymphRaidRP)
			{
				count = base.GetNymphCount(parms, map);
			}
			else
			{
				count = (Find.World.worldPawns.AllPawnsAlive.Count + map.mapPawns.FreeColonistsAndPrisonersSpawnedCount);
				//限制上限
				count = Mathf.Min(count, 100);
				//限制下限
				count = Mathf.Max(count, 1);
			}

			return count;
		}
	}
}
