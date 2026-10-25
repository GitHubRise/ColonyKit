#nullable disable

﻿using Multiplayer.API;
using RimWorld;
using rjw.Modules.Nymphs.Implementation;
using rjw.Modules.Shared.Logs;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace rjw.Modules.Nymphs.Incidents
{
	public abstract class IncidentWorker_BaseNymphRaid : IncidentWorker_NeutralGroup
	{
		private static ILog _log = LogManager.GetLogger<IncidentWorker_BaseNymphRaid, NymphLogProvider>();

		protected static readonly INymphService _nymphGeneratorService;

		static IncidentWorker_BaseNymphRaid()
		{
			_nymphGeneratorService = NymphService.Instance;
		}

		protected virtual float ThreatPointMultiplier => 1f;

		[SyncMethod]
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			//不支持多人游戏……大概
			if (MP.IsInMultiplayer)
			{
				_log.Debug("Could not fire because multiplayer api is on");
				return false;
			}

			//没有地图
			if (parms.target is Map == false)
			{
				_log.Debug("Could not fire because the incident target is not a map");
				return false;
			}

			return base.CanFireNowSub(parms);
		}

		protected virtual int GetNymphCount(IncidentParms parms, Map map)
		{
			//计算花痴的数量
			int count;
			count = Mathf.RoundToInt(parms.points / parms.pawnKind.combatPower * ThreatPointMultiplier);
			//限制下限
			count = Mathf.Max(count, 1);

			return count;
		}

		[SyncMethod]
		protected override void ResolveParmsPoints(IncidentParms parms)
		{
			if (parms.points <= 0)
			{
				parms.points = StorytellerUtility.DefaultThreatPointsNow(parms.target) * ThreatPointMultiplier;
			}
			_log.Debug($"Incident generated with {parms.points} points");
		}

		[SyncMethod]
		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			_log.Debug($"Generating incident");

			//从边缘走入
			parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
			if (parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms) == false)
			{
				_log.Debug($"Incident failed to fire, no spawn points available");
				return false;
			}

			//查找 PawnKind
			parms.pawnKind = Nymph_Generator.GetFixedNymphPawnKindDef();

			if(parms.pawnKind == null)
			{
				_log.Debug($"Incident failed to fire, no Pawn Kind for nymphs");
				return false;
			}

			//计算花痴的数量
			int count = GetNymphCount(parms, (Map)parms.target);

			_log.Debug($"Will generate {count} nymphs");

			List<Pawn> nymphs = GenerateNymphs(parms.target as Map, count);

			parms.raidArrivalMode.Worker.Arrive(nymphs, parms);

			SetManhunters(nymphs);

			Find.LetterStack.ReceiveLetter(
				"RJW_nymph_incident_raid_title".Translate(), 
				"RJW_nymph_incident_raid_description".Translate(),
				LetterDefOf.ThreatBig,
				nymphs);

			return true;
		}


		protected List<Pawn> GenerateNymphs(Map map, int count)
		{
			List<Pawn> result = new List<Pawn>();

			PawnKindDef nymphKind = _nymphGeneratorService.RandomNymphKind();

			for (int i = 1; i <= count; ++i)
			{
				Pawn nymph = _nymphGeneratorService.GenerateNymph(map, nymphKind);

				//设为野生
				nymph.ChangeKind(PawnKindDefOf.WildMan);

				result.Add(nymph);
			}

			return result;
		}

		protected void SetManhunters(List<Pawn> nymphs)
		{
			foreach (var nymph in nymphs)
			{
				_nymphGeneratorService.SetManhunter(nymph);
			}
		}
	}
}
