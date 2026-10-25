#nullable disable

using HarmonyLib;
using RimWorld;
using System;
using Verse;


namespace rjw
{
	///<summary>
	///在 GuestRelease 时清除标记
	///在小人离开地图之前，小人仍然是访客/奴隶/囚犯，因此实际上可以改回来
	///</summary>
	[HarmonyPatch(typeof(GenGuest), "GuestRelease")]
	[StaticConstructorOnStartup]
	static class PATCH_GenGuest_GuestRelease
	{
		[HarmonyPrefix]
		private static void GuestRelease_Update_Designators(Pawn p)
		{
			if (p != null)
			{
				//ModLog.Message("GenGuest GuestRelease");
				//ModLog.Message("pawn: " + xxx.get_pawnname(p));
				p.UnDesignateComfort();
				p.UnDesignateService();
				p.UnDesignateBreeding();
				p.UnDesignateBreedingAnimal();
				p.UnDesignateMilking();
				p.UnDesignateHero();
				//ModLog.Message(p.IsDesignatedComfort().ToString());
			}
		}
	}

	///<summary>
	///在 Pawn_ExitMap 时清除标记
	///此时小人真正离开地图并重置其派系
	///</summary>
	[HarmonyPatch(typeof(Pawn), "ExitMap")]
	[StaticConstructorOnStartup]
	static class PATCH_Pawn_ExitMap
	{
		[HarmonyPrefix]
		private static void Pawn_ExitMap_Update_Designators(Pawn __instance, bool allowedToJoinOrCreateCaravan, Rot4 exitDir)
		{
			Pawn p = __instance;
			if (p != null)
				if (!p.IsColonist)
				{
					//ModLog.Message("Pawn ExitMap");
					//ModLog.Message("pawn: " + xxx.get_pawnname(p));
					p.UnDesignateComfort();
					p.UnDesignateService();
					p.UnDesignateBreeding();
					p.UnDesignateBreedingAnimal();
					p.UnDesignateMilking();
					p.UnDesignateHero();
					//ModLog.Message(p.IsDesignatedComfort().ToString());
				}
		}
	}
}
