using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>2×2 双人浴缸：可同时容纳两名小人，也是鸳鸯浴的场所。</summary>
	public class Building_DoubleBath : Building_Bath
	{
		public const int MaxBathers = 2;

		public const float WaterCapacity = 320f;

		private static JobDef togetherJobDef;

		public static JobDef TogetherJobDef
		{
			get
			{
				if (togetherJobDef == null)
				{
					togetherJobDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeBathTogether");
				}
				return togetherJobDef;
			}
		}

		public override float waterUsed => WaterCapacity * HygieneSettings.Current.waterUseMultiplier;

		/// <summary>该小人是否正占着这个浴缸（含还在路上）。用当前 Job 判定，读档后依然准确。</summary>
		public bool IsBathing(Pawn pawn)
		{
			return pawn != null
				&& pawn.Spawned
				&& pawn.Map == Map
				&& pawn.CurJob != null
				&& TogetherJobDef != null
				&& pawn.CurJob.def == TogetherJobDef
				&& pawn.CurJob.targetA.Thing == this;
		}

		public int BatherCount
		{
			get
			{
				if (Map == null || TogetherJobDef == null)
				{
					return 0;
				}
				int count = 0;
				foreach (Pawn pawn in Map.mapPawns.AllPawnsSpawned)
				{
					if (IsBathing(pawn))
					{
						count++;
					}
				}
				return count;
			}
		}

		public bool RoomFor(Pawn pawn)
		{
			if (pawn != null && IsBathing(pawn))
			{
				return true;
			}
			return BatherCount < MaxBathers;
		}

		public bool AnyOtherBather(Pawn pawn)
		{
			if (Map == null)
			{
				return false;
			}
			foreach (Pawn other in Map.mapPawns.AllPawnsSpawned)
			{
				if (other != pawn && IsBathing(other))
				{
					return true;
				}
			}
			return false;
		}

		public override bool CanUse(Pawn pawn)
		{
			return base.CanUse(pawn) && RoomFor(pawn);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			string bathers = "CK.Hygiene.BathOccupants".Translate(BatherCount, MaxBathers);
			if (text.NullOrEmpty())
			{
				return bathers;
			}
			return text + "\n" + bathers;
		}

		/// <summary>邀请条件：异性、成年、能走到、当前有空位；恋人优先。</summary>
		public static bool CanInvite(Pawn initiator, Pawn partner, Building_DoubleBath bath)
		{
			if (initiator == null || partner == null || bath == null || bath.Map == null)
			{
				return false;
			}
			if (partner == initiator
				|| partner.Dead
				|| partner.Downed
				|| !partner.Spawned
				|| partner.Map != bath.Map
				|| partner.Drafted
				|| partner.InMentalState
				|| partner.InBed())
			{
				return false;
			}
			if (!partner.RaceProps.Humanlike || partner.DevelopmentalStage != DevelopmentalStage.Adult)
			{
				return false;
			}
			if (!partner.IsColonist && !partner.IsSlave)
			{
				return false;
			}
			// 只邀请异性；性别未定（机械体改造等）的不参与。
			if (partner.gender == Gender.None || partner.gender == initiator.gender)
			{
				return false;
			}
			Need_Hygiene need = partner.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null || need.CurLevel >= 0.99f)
			{
				return false;
			}
			if (TogetherJobDef != null && partner.CurJobDef == TogetherJobDef)
			{
				return false;
			}
			return bath.PawnAllowed(partner)
				&& bath.RoomFor(partner)
				&& partner.CanReach(bath, PathEndMode.Touch, Danger.Deadly);
		}

		public static Pawn FindPartner(Pawn initiator, Building_DoubleBath bath)
		{
			if (initiator?.Map == null || bath == null)
			{
				return null;
			}
			Pawn best = null;
			float bestScore = float.MinValue;
			foreach (Pawn candidate in initiator.Map.mapPawns.AllPawnsSpawned)
			{
				if (!CanInvite(initiator, candidate, bath))
				{
					continue;
				}
				float score = Rand.Value;
				if (LovePartnerRelationUtility.LovePartnerRelationExists(initiator, candidate))
				{
					score += 3f;
				}
				if (score > bestScore)
				{
					bestScore = score;
					best = candidate;
				}
			}
			return best;
		}
	}
}
