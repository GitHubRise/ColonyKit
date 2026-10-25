using Verse;
using Multiplayer.API;

namespace rjw
{
	public static class PawnDesignations_Service
	{
		public static bool UpdateCanDesignateService(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			//无权更改非囚犯主角/其他玩家主角的标记
			if (!pawn.CanChangeDesignationPrisoner() && !pawn.CanChangeDesignationColonist())
				return pawnData.CanDesignateService = false;

			//无权更改囚犯主角/自己的标记
			if (!pawn.CanChangeDesignationPrisoner())
				return pawnData.CanDesignateService = false;

			//无法进行性行为
			if (!(xxx.can_fuck(pawn) || xxx.can_be_fucked(pawn)))
				return pawnData.CanDesignateService = false;

			if (!pawn.IsDesignatedHero())
			{
				if (pawn.IsColonist)
					return pawnData.CanDesignateService = true;
			}
			else if (pawn.IsHeroOwner())
				return pawnData.CanDesignateService = true;

			if (pawn.IsPrisonerOfColony || xxx.is_slave(pawn))
				return pawnData.CanDesignateService = true;

			return pawnData.CanDesignateService = false;
		}

		public static bool CanDesignateService(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanDesignateService == true;
		}

		public static void ToggleService(this Pawn pawn)
		{
			pawn.UpdateCanDesignateService();
			if (pawn.CanDesignateService())
			{
				if (!pawn.IsDesignatedService())
					DesignateService(pawn);
				else
					UnDesignateService(pawn);
			}
		}

		public static bool IsDesignatedService(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (pawnData.Service)
			{
				if (!pawn.IsDesignatedHero())
					if (!(pawn.IsColonist || pawn.IsPrisonerOfColony || xxx.is_slave(pawn)))
						UnDesignateService(pawn);

				if (pawn.Dead)
					pawn.UnDesignateService();
			}

			return pawnData.Service;
		}

		[SyncMethod]
		public static void DesignateService(this Pawn pawn)
		{
			DesignatorsData.rjwService.AddDistinct(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Service = true;
			}
		}

		[SyncMethod]
		public static void UnDesignateService(this Pawn pawn)
		{
			DesignatorsData.rjwService.Remove(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Service = false;
			}
		}
	}
}
