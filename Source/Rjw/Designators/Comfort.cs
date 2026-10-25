using Verse;
using Multiplayer.API;

namespace rjw
{
	public static class PawnDesignations_Comfort
	{
		public static bool UpdateCanDesignateComfort(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			//强暴已禁用
			if (!RJWSettings.rape_enabled)
				return pawnData.CanDesignateComfort = false;

			//无权更改非囚犯主角/其他玩家主角的标记
			if (!pawn.CanChangeDesignationPrisoner() && !pawn.CanChangeDesignationColonist())
				return pawnData.CanDesignateComfort = false;

			//无权更改囚犯主角/自己的标记
			if (!pawn.CanChangeDesignationPrisoner())
				return pawnData.CanDesignateComfort = false;

			//无法进行性行为
			if (!(xxx.can_fuck(pawn) || xxx.can_be_fucked(pawn)))
				return pawnData.CanDesignateComfort = false;

			if (!pawn.IsDesignatedHero())
			{
				if ((xxx.is_masochist(pawn) || (RJWSettings.override_RJW_designation_checks && !MP.IsInMultiplayer)) && pawn.IsColonist)
					return pawnData.CanDesignateComfort = true;
			}
			else if (pawn.IsHeroOwner())
				return pawnData.CanDesignateComfort = true;

			if (pawn.IsPrisonerOfColony || xxx.is_slave(pawn))
				return pawnData.CanDesignateComfort = true;

			return pawnData.CanDesignateComfort = false;
		}

		public static bool CanDesignateComfort(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanDesignateComfort == true;
		}

		public static void ToggleComfort(this Pawn pawn)
		{
			pawn.UpdateCanDesignateComfort();
			if (pawn.CanDesignateComfort())
			{
				if (!pawn.IsDesignatedComfort())
					DesignateComfort(pawn);
				else
					UnDesignateComfort(pawn);
			}
		}

		public static bool IsDesignatedComfort(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (pawnData.Comfort)
			{
				if (!pawn.IsDesignatedHero())
				{
					if (!pawn.IsPrisonerOfColony)
						if (!(xxx.is_masochist(pawn) || xxx.is_slave(pawn)))
						{
							if (!pawn.IsColonist)
								UnDesignateComfort(pawn);
							else if (!(RJWSettings.WildMode || (RJWSettings.override_RJW_designation_checks && !MP.IsInMultiplayer)))
								UnDesignateComfort(pawn);
						}
				}

				if (pawn.Dead)
					pawn.UnDesignateComfort();
			}

			return pawnData.Comfort;
		}

		[SyncMethod]
		public static void DesignateComfort(this Pawn pawn)
		{
			DesignatorsData.rjwComfort.AddDistinct(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Comfort = true;
			}
		}

		[SyncMethod]
		public static void UnDesignateComfort(this Pawn pawn)
		{
			DesignatorsData.rjwComfort.Remove(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Comfort = false;
			}
		}
	}
}
