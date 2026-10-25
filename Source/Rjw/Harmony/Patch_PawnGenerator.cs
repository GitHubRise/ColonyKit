#nullable enable

using HarmonyLib;
using Verse;
using RimWorld;

namespace rjw
{
	/// <summary>
	/// 修补 PawnGenerator，使其：
	/// 为小人添加生殖器
	/// 在需要时生成花痴
	/// 修复新生儿的胡须和纹身
	/// </summary>
	[HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
	static class Patch_PawnGenerator_GenerateNewPawnInternal
	{
		[HarmonyPrefix]
		static void Generate_Nymph(ref PawnGenerationRequest request)
		{
			if (Nymph_Generator.IsNymph(request))
			{
				request = new PawnGenerationRequest(
					kind: request.KindDef = Nymph_Generator.GetFixedNymphPawnKindDef(),
					canGeneratePawnRelations: request.CanGeneratePawnRelations = false,
					fixedGender: request.FixedGender = Nymph_Generator.RandomNymphGender()
					);
			}
		}

		public static void Postfix(PawnGenerationRequest request, Pawn __result)
		{
			__result.GetCompRJW()?.Sexualize();
			FixNymph(request, __result);
			FixNewbornStyles(request, __result);
		}

		private static void FixNymph(PawnGenerationRequest request, Pawn pawn)
		{
			if (Nymph_Generator.IsNymph(request))
			{
				Nymph_Generator.set_story(pawn);
				Nymph_Generator.set_skills(pawn);
			}
		}

		private static void FixNewbornStyles(PawnGenerationRequest request, Pawn pawn)
		{
			if (request.AllowedDevelopmentalStages == DevelopmentalStage.Newborn)
			{
				if(pawn.style != null)
				{
					pawn.style.beardDef = BeardDefOf.NoBeard;
					pawn.style.SetupTattoos_NoIdeology();
				}
			}
		}
	}
}
