using Verse;

namespace rjw
{
	public static class PawnDesignations_Utility
	{
		public static bool UpdatePermissions(this Pawn pawn)
		{
			if (pawn.GetRJWPawnData() == null)
			{
				ModLog.Error($"UpdatePermissions was called for {pawn}, pawn without the RJW PawnData");
				return false;
			}

			pawn.UpdateCanChangeDesignationPrisoner();
			pawn.UpdateCanChangeDesignationColonist();
			pawn.UpdateCanDesignateService();
			pawn.UpdateCanDesignateComfort();
			pawn.UpdateCanDesignateBreedingAnimal();
			pawn.UpdateCanDesignateBreeding();
			pawn.UpdateCanDesignateHero();
			return true;
		}

		/// <summary>
		/// 检查该小人是否为其他玩家的主角。
		/// 如果是——在多人游戏中限制该小人的控件访问。
		/// </summary>
		public static bool UpdateCanChangeDesignationColonist(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				ModLog.Error($"UpdateCanChangeDesignationColonist was called for {pawn}, pawn without the RJW PawnData");
				return false;
			}

			if (pawn.IsDesignatedHero() && !pawn.IsHeroOwner())
			{
				//ModLog.Warning("CanChangeDesignationColonist:: Pawn:" + xxx.get_pawnname(pawn));
				//ModLog.Warning("CanChangeDesignationColonist:: IsDesignatedHero:" + pawn.IsDesignatedHero());
				//ModLog.Warning("CanChangeDesignationColonist:: IsHeroOwner:" + pawn.IsHeroOwner());
				return pawnData.CanChangeDesignationColonist = false;
			}

			return pawnData.CanChangeDesignationColonist = true;
		}
		public static bool CanChangeDesignationColonist(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanChangeDesignationColonist == true;
		}

		/// <summary>
		/// 检查玩家主角是否为奴隶/囚犯。
		/// 如果是——在多人游戏中限制所有控件的访问。
		/// </summary>
		public static bool UpdateCanChangeDesignationPrisoner(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				ModLog.Error($"UpdateCanChangeDesignationPrisoner was called for {pawn}, pawn without the RJW PawnData");
				return false;
			}

			//Stopwatch sw = new Stopwatch();
			//sw.Start();

			//ModLog.Warning("rjwHero:: Count " + DesignatorsData.rjwHero.Count);
			//ModLog.Warning("rjwComfort:: Count " + DesignatorsData.rjwComfort.Count);
			//ModLog.Warning("rjwService:: Count " + DesignatorsData.rjwService.Count);
			//ModLog.Warning("rjwMilking:: Count " + DesignatorsData.rjwMilking.Count);
			//ModLog.Warning("rjwBreeding:: Count " + DesignatorsData.rjwBreeding.Count);
			//ModLog.Warning("rjwBreedingAnimal:: Count " + DesignatorsData.rjwBreedingAnimal.Count);

			foreach (Pawn item in DesignatorsData.rjwHero)
			{
				//ModLog.Warning("CanChangeDesignationPrisoner:: Pawn:" + xxx.get_pawnname(item));
				//ModLog.Warning("CanChangeDesignationPrisoner:: IsHeroOwner:" + item.IsHeroOwner());
				if (item.IsHeroOwner())
				{
					if (item.IsPrisonerOfColony || item.IsPrisoner || xxx.is_slave(item))
					{
						//ModLog.Warning("CanChangeDesignationPrisoner:: Pawn:" + xxx.get_pawnname(item));
						//ModLog.Warning("CanChangeDesignationPrisoner:: Hero of " + MP.PlayerName);
						//ModLog.Warning("CanChangeDesignationPrisoner:: is prisoner(colony):" + item.IsPrisonerOfColony);
						//ModLog.Warning("CanChangeDesignationPrisoner:: is prisoner:" + item.IsPrisoner);
						//ModLog.Warning("CanChangeDesignationPrisoner:: is slave:" + xxx.is_slave(item));
						return pawnData.CanChangeDesignationPrisoner = false;
					}
				}
			}
			//sw.Stop();
			//ModLog.Warning("Elapsed={0}" + sw.Elapsed);

			return pawnData.CanChangeDesignationPrisoner = true;
		}
		public static bool CanChangeDesignationPrisoner(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanChangeDesignationPrisoner == true;
		}
	}
}
