using System.Linq;
using Verse;
using rjw.Modules.Shared.Logs;
using static rjw.Genital_Helper;

using Sex = rjw.GenderHelper.Sex;
using Seeded = rjw.Modules.Rand.Seeded;

namespace rjw.Modules.Testing
{
	static class TestHelper
	{
		static readonly ILog Log = LogManager.GetLogger("TestHelper");

		/// <summary>
		/// 为此请求添加一些便于测试的默认选项。
		/// </summary>
		/// <param name="request">要修改的请求。</param>
		/// <returns>修改后的请求。</returns>
		public static PawnGenerationRequest RequestDefaults(this PawnGenerationRequest request)
		{
			request.CanGeneratePawnRelations = false;
			request.ForceNoIdeo = true;
			request.ForceGenerateNewPawn = true;
			return request;
		}

		/// <summary>
		/// <para>使用给定的请求和固定种子生成一个自然小人。</para>
		/// <para>如果生成小人时出现问题，将返回 null。</para>
		/// </summary>
		/// <param name="request">小人请求。</param>
		/// <returns>生成的小人，或 null。</returns>
		public static Pawn? GenerateSeededPawn(PawnGenerationRequest request)
		{
			using (Seeded.With(42))
			{
				var tries = 5;
				while (tries >= 0)
				{
					var pawn = PawnGenerator.GeneratePawn(request);

					// 不断生成，直到得到“自然”的小人。我们想要
					// 控制扶她和伪娘出现的时机。
					switch (GenderHelper.GetSex(pawn))
					{
						case Sex.Male when pawn.gender == Gender.Male:
						case Sex.Female when pawn.gender == Gender.Female:
						case Sex.None when pawn.gender == Gender.None:
							return pawn;
					}
					tries -= 1;
				}
				Log.Error($"Could not generate test pawn for: {request.KindDef.defName}");
				return null;
			}
		}

		/// <summary>
		/// 尝试用配方替换给定小人的某个性部件。
		/// </summary>
		/// <param name="pawn">要修改的小人。</param>
		/// <param name="recipe">要应用的配方。</param>
		/// <returns>部件是否成功应用。</returns>
		public static bool ApplyPartToPawn(Pawn pawn, RecipeDef recipe)
		{
			var worker = recipe.Worker;
			var defToAdd = recipe.addsHediff as HediffDef_SexPart;
			if (defToAdd == null) return false;

			// 它们必须有对应的身体部位才能安装该性部件。
			var bpr = worker.GetPartsToApplyOn(pawn, recipe).FirstOrDefault();
			if (bpr is null) return false;

			var curParts = get_AllPartsHediffList(pawn)
				.Where(hed => hed is ISexPartHediff part && part.Def.genitalFamily == defToAdd.genitalFamily)
				.ToArray();

			// 我们用人工部件替换天然部件。
			if (curParts.Length == 0) return false;
			foreach (var part in curParts) pawn.health.RemoveHediff(part);

			var hediff = SexPartAdder.MakePart(defToAdd, pawn, bpr);
			pawn.health.AddHediff(hediff, bpr);
			return true;
		}

		/// <summary>
		/// 把某个小人的生殖器替换为人工生殖器。
		/// </summary>
		/// <param name="pawn">要修改的小人。</param>
		/// <returns>部件是否成功替换。</returns>
		public static bool GiveArtificialGenitals(Pawn pawn)
		{
			var changed = false;
			if (get_genitalsBPR(pawn) is not { } bpr) return changed;

			if (has_penis_fertile(pawn))
			{
				foreach (var part in pawn.GetGenitalsList())
					if (is_fertile_penis(part))
						pawn.health.RemoveHediff(part);
				var hediff = SexPartAdder.MakePart(hydraulic_penis, pawn, bpr);
				pawn.health.AddHediff(hediff, bpr);
				changed = true;
			}

			if (has_vagina(pawn))
			{
				foreach (var part in pawn.GetGenitalsList())
					if (is_vagina(part))
						pawn.health.RemoveHediff(part);
				var hediff = SexPartAdder.MakePart(hydraulic_vagina, pawn, bpr);
				pawn.health.AddHediff(hediff, bpr);
				changed = true;
			}

			return changed;
		}

		/// <summary>
		/// <para>为小人添加部位，使其成为伪娘。</para>
		/// <para>小人必须是天然男性才能被修改。如果小人
		/// 已经是伪娘，则返回 `true`。否则，如果修改小人失败，
		/// 就返回 `false`。</para>
		/// <para>备注（2023 年 2 月起）：此功能目前有 bug，因为
		/// `SexPartAdder.add_breasts` 不遵循使用女性
		/// 胸部的请求。</para>
		/// </summary>
		/// <param name="pawn">要修改的小人。</param>
		/// <returns>小人是否被修改。</returns>
		public static bool MakeIntoTrap(Pawn pawn)
		{
			if (GenderHelper.GetSex(pawn) is var sex and not Sex.Male)
				return sex is Sex.Trap;
			
			var parts = pawn.GetBreastList();
			foreach (var part in parts)
				pawn.health.RemoveHediff(part);
			SexPartAdder.add_breasts(pawn, gender: Gender.Female);
			return GenderHelper.GetSex(pawn) is Sex.Trap;
		}

		/// <summary>
		/// <para>为小人添加部位，使其成为扶她。</para>
		/// <para>小人必须是天然男性或女性；如果未能把小人
		/// 改成扶她，则返回 `false`。</para>
		/// </summary>
		/// <param name="pawn">要修改的小人。</param>
		/// <param name="infertile">该部件是否应为不育。</param>
		/// <returns>小人是否被修改。</returns>
		public static bool MakeIntoFuta(Pawn pawn, bool infertile = false)
		{
			Hediff hediff;
			switch ((GenderHelper.GetSex(pawn), infertile))
			{
				case (Sex.Male or Sex.Trap, false):
					SexPartAdder.add_genitals(pawn, gender: Gender.Female);
					return GenderHelper.GetSex(pawn) is Sex.Futa;
				case (Sex.Male or Sex.Trap, true) when get_genitalsBPR(pawn) is { } bpr:
					hediff = SexPartAdder.MakePart(hydraulic_vagina, pawn, bpr);
					pawn.health.AddHediff(hediff, bpr);
					return GenderHelper.GetSex(pawn) is Sex.Futa;
				case (Sex.Female, false):
					SexPartAdder.add_genitals(pawn, gender: Gender.Male);
					return GenderHelper.GetSex(pawn) is Sex.Futa;
				case (Sex.Female, true) when get_genitalsBPR(pawn) is { } bpr:
					hediff = SexPartAdder.MakePart(hydraulic_penis, pawn, bpr);
					pawn.health.AddHediff(hediff, bpr);
					return GenderHelper.GetSex(pawn) is Sex.Futa;
				default:
					return false;
			}
		}
	}
}