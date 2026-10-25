using Verse;
using RimWorld;
using Multiplayer.API;

namespace rjw
{
	public static class PawnDesignations_Breeder
	{
		public static bool UpdateCanDesignateBreedingAnimal(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			//无权更改非囚犯主角/其他玩家主角的标记
			if (!pawn.CanChangeDesignationPrisoner() && !pawn.CanChangeDesignationColonist())
				return pawnData.CanDesignateBreedingAnimal = false;

			//无权更改囚犯主角/自己的标记
			if (!pawn.CanChangeDesignationPrisoner())
				return pawnData.CanDesignateBreedingAnimal = false;

			//ModLog.Message("CanDesignateAnimal for " + xxx.get_pawnname(pawn) + " " + SaveStorage.bestiality_enabled);
			//ModLog.Message("checking animal props " + (pawn.Faction?.IsPlayer.ToString()?? "tynanfag") + xxx.is_animal(pawn) + xxx.can_rape(pawn));
			if ((RJWSettings.bestiality_enabled || RJWSettings.animal_on_animal_enabled)
				&& xxx.is_animal(pawn)
				&& xxx.can_fuck(pawn)
				&& pawn.Faction == Faction.OfPlayer)
				return pawnData.CanDesignateBreedingAnimal = true;

			return pawnData.CanDesignateBreedingAnimal = false;
		}

		public static bool CanDesignateBreedingAnimal(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanDesignateBreedingAnimal == true;
		}

		public static void ToggleBreedingAnimal(this Pawn pawn)
		{
			pawn.UpdateCanDesignateBreedingAnimal();
			if (pawn.CanDesignateBreedingAnimal())
			{
				if (!pawn.IsDesignatedBreedingAnimal())
					DesignateBreedingAnimal(pawn);
				else
					UnDesignateBreedingAnimal(pawn);
			}
		}

		public static bool IsDesignatedBreedingAnimal(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (pawnData.BreedingAnimal)
			{
				if (!pawn.Faction?.IsPlayer ?? false)
					UnDesignateBreedingAnimal(pawn);

				if (pawn.Dead)
					pawn.UnDesignateBreedingAnimal();
			}

			return pawnData.BreedingAnimal;
		}

		[SyncMethod]
		public static void DesignateBreedingAnimal(this Pawn pawn)
		{
			DesignatorsData.rjwBreedingAnimal.AddDistinct(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.BreedingAnimal = true;
			}
		}

		[SyncMethod]
		public static void UnDesignateBreedingAnimal(this Pawn pawn)
		{
			DesignatorsData.rjwBreedingAnimal.Remove(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.BreedingAnimal = false;
			}
		}
	}
}
