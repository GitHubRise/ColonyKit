#nullable disable

﻿using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace rjw
{
	/// <summary>
	/// 修改 Dialog_NamePawn，使其能在子女的重命名窗口中正确显示同性父母
	/// </summary>
	[HarmonyPatch(typeof(Dialog_NamePawn))]
	[HarmonyPatch(MethodType.Constructor)]
	[HarmonyPatch(new System.Type[] {
	typeof(Pawn),
	typeof(NameFilter),
	typeof(NameFilter),
	typeof(Dictionary<NameFilter, List<string>>),
	typeof(string),
	typeof(string),
	typeof(string),
	typeof(string)})]
	static class patch_Dialog_NamePawn
	{
		static AccessTools.FieldRef<Dialog_NamePawn, TaggedString> descriptionTextRef = AccessTools.FieldRefAccess<Dialog_NamePawn, TaggedString>("descriptionText");
		static MethodInfo DescribePawn = AccessTools.Method(typeof(Dialog_NamePawn), nameof(DescribePawn));

		[HarmonyPostfix]
		static void Constructor(Dialog_NamePawn __instance, Pawn pawn)
		{
			string motherString = "Mother";
			string fatherString = "Father";

			if (pawn != null && pawn.RaceProps.Humanlike)
			{
				Pawn mother = pawn.GetMother();
				Pawn father = pawn.GetFather();
				if (father == null && mother == null) return; // 如果两者都失败，这段代码就毫无意义，我不知道这种情况会不会发生

				Pawn parent = RelationChecker.getParent(pawn);
				Pawn secondParent = RelationChecker.getSecondParent(pawn);
				if (parent != null && secondParent != null && (mother == null || father == null)) // 同性父母
				{
					if (father == null)
					{
						fatherString = "Mother"; // 可以随意换成更合适的词
						father = mother == parent ? secondParent : parent;
					}
					if (mother == null)
					{
						motherString = "Father";
						mother = father == parent ? secondParent : parent;
					}

					descriptionTextRef(__instance) =
						"{0}: {1}\n{2}: {3}".Formatted(
							motherString.Translate().CapitalizeFirst(),
							(TaggedString)DescribePawn.Invoke(__instance, new object[]{mother}),
							fatherString.Translate().CapitalizeFirst(),
							(TaggedString)DescribePawn.Invoke(__instance, new object[]{father}));
				}
			}
		}
	}
}
