using Verse;
using RimWorld;
using Multiplayer.API;

namespace rjw
{
	public static class PawnDesignations_Hero
	{
		public static bool UpdateCanDesignateHero(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (RJWSettings.RPG_hero_control
				&& xxx.is_human(pawn)
				&& pawn.IsColonist
				&& !xxx.is_slave(pawn)
				&& !pawn.IsPrisoner)
			{
				if (!pawn.IsDesignatedHero())
				{
					foreach (Pawn item in DesignatorsData.rjwHero)
					{
						if (item.IsHeroOwner())
						{
							if (RJWSettings.RPG_hero_control_Ironman && !pawnData.Ironman)
								SetHeroIronman(item);
							if (item.Dead && !pawnData.Ironman)
							{
								UnDesignateHero(item);
								//ModLog.Warning("CanDesignateHero:: "  + MP.PlayerName + " hero is dead remove hero tag from " + item.Name);
							}
							else
							{
								//ModLog.Warning("CanDesignateHero:: "  + MP.PlayerName + " already has hero - " + item.Name);
								return pawnData.CanDesignateHero = false;
							}

						}
						else
							continue;
					}
					return pawnData.CanDesignateHero = true;
				}
			}
			return pawnData.CanDesignateHero = false;
		}

		public static bool CanDesignateHero(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.CanDesignateHero == true;
		}

		public static void ToggleHero(this Pawn pawn)
		{
			pawn.UpdateCanDesignateHero();
			if (pawn.CanDesignateHero() && Find.Selector.NumSelected <= 1)
			{
				if (!pawn.IsDesignatedHero())
					DesignateHero(pawn);
			}
		}

		public static bool IsDesignatedHero(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.Hero == true;
		}

		public static void DesignateHero(this Pawn pawn)
		{
			SyncHero(pawn, MP.PlayerName);
		}

		[SyncMethod]
		public static void UnDesignateHero(this Pawn pawn)
		{
			DesignatorsData.rjwHero.Remove(pawn);
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Hero = false;
			}
		}

		public static bool IsHeroOwner(this Pawn pawn)
		{
			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				return false;
			}

			if (!MP.enabled)
				return pawnData.HeroOwner == "Player" || pawnData.HeroOwner == null || pawnData.HeroOwner == "";
			else
				return pawnData.HeroOwner == MP.PlayerName;
		}

		[SyncMethod]
		static void SyncHero(Pawn pawn, string theName)
		{
			if (!MP.enabled)
				theName = "Player";

			PawnData? pawnData = pawn.GetRJWPawnData();

			if (pawnData == null)
			{
				ModLog.Error($"Tried to make {pawn} a hero, pawn have no PawnData");
				return;
			}

			pawnData.Hero = true;
			pawnData.HeroOwner = theName;
			pawnData.Ironman = RJWSettings.RPG_hero_control_Ironman;
			DesignatorsData.rjwHero.AddDistinct(pawn);
			string text = "RJW_Message_BecameHero".Translate(pawn, theName).CapitalizeFirst();
			Messages.Message(text, pawn, MessageTypeDefOf.NeutralEvent);
			//ModLog.Message(MP.PlayerName + "  set " + pawn.Name + " to hero:" + pawn.GetPawnData().Hero);

			pawn.UpdatePermissions();
		}

		[SyncMethod]
		public static void SetHeroIronman(this Pawn pawn)
		{
			if (pawn.GetRJWPawnData() is PawnData pawnData)
			{
				pawnData.Ironman = true;
			}
		}
	}
}
