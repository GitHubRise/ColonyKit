#nullable disable

using Verse;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using HarmonyLib;
using RimWorld.Planet;

namespace rjw
{
	/// <summary>
	/// 小人指定器列表的集合
	/// </summary>
	public static class DesignatorsData
	{
		public static readonly List<Pawn> rjwHero = new List<Pawn>();
		public static readonly List<Pawn> rjwComfort = new List<Pawn>();
		public static readonly List<Pawn> rjwService = new List<Pawn>();
		public static readonly List<Pawn> rjwMilking = new List<Pawn>();
		public static readonly List<Pawn> rjwBreeding = new List<Pawn>();
		public static readonly List<Pawn> rjwBreedingAnimal = new List<Pawn>();

		/// <summary>
		/// 清空所有 DesignatorsData 列表的快捷方法
		/// </summary>
		public static void ClearAll()
		{
			rjwHero.Clear();
			rjwComfort.Clear();
			rjwService.Clear();
			rjwMilking.Clear();
			rjwBreeding.Clear();
			rjwBreedingAnimal.Clear();
		}

		[DebugAction("RJW", "Dump DesignatorsData", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		internal static void DumpDesignators()
		{
			ModLog.Message("DesignatorsData dump");
			DumpDesignator(nameof(rjwHero), rjwHero);
			DumpDesignator(nameof(rjwComfort), rjwComfort);
			DumpDesignator(nameof(rjwService), rjwService);
			DumpDesignator(nameof(rjwMilking), rjwMilking);
			DumpDesignator(nameof(rjwBreeding), rjwBreeding);
			DumpDesignator(nameof(rjwBreedingAnimal), rjwBreedingAnimal);
			ModLog.Message("End of dump");
		}

		private static void DumpDesignator(string name, List<Pawn> list)
		{
			ModLog.Message($"{name} ({list.Count()} records):");
			foreach(Pawn pawn in list)
			{
				ModLog.Message($"- {pawn}");
			}
		}
	}

	[HarmonyPatch(typeof(World), "ConstructComponents")]
	static class PATCH_World_FillComponents_ClearDesignators
	{
		public static void Postfix()
		{
			DesignatorsData.ClearAll();
		}
	}
}
