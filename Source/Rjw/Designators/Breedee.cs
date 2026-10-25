using Verse;
using RimWorld;
using Multiplayer.API;

namespace rjw
{
	public static class PawnDesignations_Breedee
	{
		public static bool UpdateCanDesignateBreeding(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			//无权更改非囚犯主角/其他玩家主角的标记
			if (!pawn.CanChangeDesignationPrisoner() && !pawn.CanChangeDesignationColonist())
				return pawnData.CanDesignateBreeding = false;

			//无权更改囚犯主角/自己的标记
			if (!pawn.CanChangeDesignationPrisoner())
				return pawnData.CanDesignateBreeding = false;

			//无法进行插入式性行为
			if (!xxx.can_be_fucked(pawn))
				return pawnData.CanDesignateBreeding = false;


			if (RJWSettings.bestiality_enabled && xxx.is_human(pawn))
			{
				if (!pawn.IsDesignatedHero())
				{
					if ((xxx.is_zoophile(pawn) || (RJWSettings.override_RJW_designation_checks && !MP.IsInMultiplayer)) && pawn.IsColonist)
						return pawnData.CanDesignateBreeding = true;
				}
				else if (pawn.IsHeroOwner())
					return pawnData.CanDesignateBreeding = true;

				if (pawn.IsPrisonerOfColony || xxx.is_slave(pawn))
					return pawnData.CanDesignateBreeding = true;
			}

			if (RJWSettings.animal_on_animal_enabled && xxx.is_animal(pawn)
				&& pawn.Faction == Faction.OfPlayer)
				return pawnData.CanDesignateBreeding = true;

			return pawnData.CanDesignateBreeding = false;
		}

		public static bool CanDesignateBreeding(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanDesignateBreeding == true;
		}

		public static void ToggleBreeding(this Pawn pawn)
		{
			pawn.UpdateCanDesignateBreeding();
			if (pawn.CanDesignateBreeding())
			{
				if (!pawn.IsDesignatedBreeding())
					DesignateBreeding(pawn);
				else
					UnDesignateBreeding(pawn);
			}
		}

		public static bool IsDesignatedBreeding(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (pawnData.Breeding)
			{
				if (!xxx.is_animal(pawn))
				{
					if (!RJWSettings.bestiality_enabled)
						UnDesignateBreeding(pawn);

					else if (!pawn.IsDesignatedHero())
						if (!(xxx.is_zoophile(pawn) || pawn.IsPrisonerOfColony || xxx.is_slave(pawn)))
							if (!(RJWSettings.WildMode || (RJWSettings.override_RJW_designation_checks && !MP.IsInMultiplayer)))
								UnDesignateBreeding(pawn);
				}
				else
				{
					if (!RJWSettings.animal_on_animal_enabled)
						UnDesignateBreeding(pawn);

					else if (!pawn.Faction?.IsPlayer ?? false)
						UnDesignateBreeding(pawn);
				}
				if (pawn.Dead)
					pawn.UnDesignateBreeding();
			}

			return pawnData.Breeding;
		}

		[SyncMethod]
		public static void DesignateBreeding(this Pawn pawn)
		{
			DesignatorsData.rjwBreeding.AddDistinct(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Breeding = true;
			}
		}

		[SyncMethod]
		public static void UnDesignateBreeding(this Pawn pawn)
		{
			DesignatorsData.rjwBreeding.Remove(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Breeding = false;
			}
		}
	}
}
