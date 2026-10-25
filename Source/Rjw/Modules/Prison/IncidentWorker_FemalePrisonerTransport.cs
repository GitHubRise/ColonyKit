#nullable disable

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	/// <summary>
	/// 女囚运输：在殖民地边缘送来 2–5 名女性囚犯，直接成为玩家的囚犯。
	/// </summary>
	public class IncidentWorker_FemalePrisonerTransport : IncidentWorker
	{
		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			if (parms.target is not Map map) return false;

			int count = Rand.RangeInclusive(2, 5);
			var prisoners = new List<Pawn>();

			Faction faction = parms.faction;
			if (faction == null || faction == Faction.OfPlayer)
			{
				faction = Find.FactionManager.AllFactionsVisible
					.Where(f => f != Faction.OfPlayer && f.def.humanlikeFaction)
					.RandomElementWithFallback(null);
			}

			for (int i = 0; i < count; i++)
			{
				PawnKindDef kind = PawnKindDefOf.Villager;
				Pawn pawn;
				try
				{
					pawn = PawnGenerator.GeneratePawn(kind, faction);
				}
				catch
				{
					continue;
				}

				if (pawn == null) continue;

				pawn.gender = Gender.Female;

				IntVec3 cell = CellFinder.RandomEdgeCell(map);
				GenSpawn.Spawn(pawn, cell, map);
				pawn.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);

				prisoners.Add(pawn);
			}

			if (prisoners.Count == 0) return false;

			Find.LetterStack.ReceiveLetter(
				"RJW_FemalePrisonTransport_Label".Translate(),
				"RJW_FemalePrisonTransport_Text".Translate(prisoners.Count),
				LetterDefOf.NeutralEvent,
				prisoners);

			return true;
		}
	}
}
