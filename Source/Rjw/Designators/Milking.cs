using Verse;
using Multiplayer.API;

namespace rjw
{
	public static class PawnDesignations_Milking
	{
		public static bool UpdateCanDesignateMilking(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			return pawnData.CanDesignateMilking = false;
		}

		public static bool CanDesignateMilking(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanDesignateMilking == true;
		}

		public static void ToggleMilking(this Pawn pawn)
		{
			if (pawn.CanDesignateMilking())
			{
				if (!pawn.IsDesignatedMilking())
					DesignateMilking(pawn);
				else
					UnDesignateMilking(pawn);
			}
		}

		public static bool IsDesignatedMilking(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (pawnData.Milking)
			{
				if (!pawn.IsDesignatedHero())
					if (!(pawn.IsColonist || pawn.IsPrisonerOfColony || xxx.is_slave(pawn)))
						UnDesignateMilking(pawn);

				if (pawn.Dead)
					pawn.UnDesignateMilking();
			}

			return pawnData.Milking;
		}

		[SyncMethod]
		public static void DesignateMilking(this Pawn pawn)
		{
			DesignatorsData.rjwMilking.AddDistinct(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Milking = true;
			}
		}

		[SyncMethod]
		public static void UnDesignateMilking(this Pawn pawn)
		{
			DesignatorsData.rjwMilking.Remove(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Milking = false;
			}
		}
	}
}
