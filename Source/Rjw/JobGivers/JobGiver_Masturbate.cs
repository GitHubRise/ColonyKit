#nullable disable

using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;

namespace rjw
{
	public class JobGiver_Masturbate : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			//--ModLog.Message(" JobGiver_Masturbate::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) called");

			if (pawn.Drafted) return null;

			if (!xxx.can_masturbate(pawn)) return null;

			// 妓女只在欲求不满时自慰，除非被囚禁。
			if ((SexUtility.ReadyForLovin(pawn) && (!xxx.is_whore(pawn) || pawn.IsPrisoner || xxx.is_slave(pawn))) || xxx.is_frustrated(pawn))
			{
				if (RJWPreferenceSettings.FapInBed && pawn.jobs.curDriver is JobDriver_LayDown layDown)
				{
					Building_Bed bed = layDown.Bed;

					if (bed != null)
					{
						if (WillingToMasturbateIn(pawn, bed))
						{
							SexUtility.IncreaseTicksToNextHookup(pawn);
							return JobMaker.MakeJob(xxx.Masturbate, pawn, bed, bed.Position);
						}
					}
				}
				else if (WillingToMasturbateEverywhere(pawn))
				{
					var spot = CasualSex_Helper.FindSexLocation(pawn);
					if (spot == null)
					{
						return null;
					}
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.Masturbate, pawn, null, spot);
				}
			}
			return null;
		}

		private bool WillingToMasturbateIn(Pawn pawn, Building_Bed bed)
		{
			if (xxx.is_frustrated(pawn))
			{
				return true;
			}

			RoomRoleDef roomRole = bed.GetRoom().Role;

			return roomRole == RoomRoleDefOf.Bedroom || roomRole == RoomRoleDefOf.PrisonCell;
		}

		private bool WillingToMasturbateEverywhere(Pawn pawn)
		{
			if (!RJWPreferenceSettings.FapEverywhere)
			{
				return false;
			}

			return xxx.is_frustrated(pawn);
		}
	}
}
