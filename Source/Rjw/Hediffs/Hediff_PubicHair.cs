#nullable disable

using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw
{
	/// <summary>
	/// 阴毛密度。密度用 <see cref="Hediff.Severity"/>（0~1）表示，
	/// 各个档位的名称由 <c>RJW_PubicHair</c> 的 stages 提供。
	/// </summary>
	/// <remarks>
	/// 它只是外观属性，不参与任何计算；挂在生殖器部位上，
	/// 因此生殖器被移除时它也会一起消失。
	/// </remarks>
	public class Hediff_PubicHair : HediffWithComps
	{
		/// <summary>不因任何原因自动移除。</summary>
		public override bool ShouldRemove => false;

		/// <summary>不在健康面板里占位，只在性爱卡片中展示。</summary>
		public override bool Visible => false;

		/// <summary>标签直接用当前档位的名称（例如「稀疏」）。</summary>
		public override string LabelBase => CurStage?.label ?? def.label;
	}

	/// <summary>
	/// 阴毛密度的读写、随机生成与补建。
	/// </summary>
	public static class PubicHairHelper
	{
		/// <summary>阴毛 hediff 的 defName。</summary>
		public const string DefName = "RJW_PubicHair";

		/// <summary>密度下限。</summary>
		public const float MinDensity = 0f;

		/// <summary>密度上限。</summary>
		public const float MaxDensity = 1f;

		/// <summary>每档密度的宽度，用于循环切换。</summary>
		public const float StepDensity = 0.2f;

		private static HediffDef cachedDef;

		/// <summary>阴毛 hediff 的定义；模组数据异常时可能为 null。</summary>
		public static HediffDef Def =>
			cachedDef ??= DefDatabase<HediffDef>.GetNamedSilentFail(DefName);

		/// <summary>取小人身上的阴毛 hediff，没有则返回 null。</summary>
		/// <param name="pawn">要检查的小人。</param>
		public static Hediff_PubicHair Get(Pawn pawn)
		{
			HediffDef def = Def;
			if (def == null || pawn?.health?.hediffSet == null)
			{
				return null;
			}

			return pawn.health.hediffSet.GetFirstHediffOfDef(def) as Hediff_PubicHair;
		}

		/// <summary>读取阴毛密度。</summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="density">密度，取值 0~1。</param>
		/// <returns>小人身上是否有阴毛 hediff。</returns>
		public static bool TryGetDensity(Pawn pawn, out float density)
		{
			Hediff_PubicHair hediff = Get(pawn);
			density = hediff?.Severity ?? MinDensity;
			return hediff != null;
		}

		/// <summary>写入阴毛密度，超出范围时截断。</summary>
		/// <param name="pawn">要修改的小人。</param>
		/// <param name="density">新的密度，取值 0~1。</param>
		public static void SetDensity(Pawn pawn, float density)
		{
			Hediff_PubicHair hediff = Get(pawn);
			if (hediff == null)
			{
				return;
			}

			hediff.Severity = Mathf.Clamp(density, MinDensity, MaxDensity);
		}

		/// <summary>循环切换一档：无毛 → 稀疏 → … → 茂密 → 无毛。</summary>
		/// <param name="pawn">要修改的小人。</param>
		public static void CycleDensity(Pawn pawn)
		{
			Hediff_PubicHair hediff = Get(pawn);
			if (hediff == null)
			{
				return;
			}

			float next = hediff.Severity + StepDensity;
			hediff.Severity = next > MaxDensity + 0.001f ? MinDensity : Mathf.Clamp(next, MinDensity, MaxDensity);
		}

		/// <summary>
		/// 给符合条件的小人补上阴毛：类人生物、身上有生殖器、且还没有阴毛。
		/// 已经有时直接返回，因此可以安全地反复调用。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		public static void Ensure(Pawn pawn)
		{
			HediffDef def = Def;
			if (def == null || pawn == null || pawn.health == null)
			{
				return;
			}

			// 只给人形小人；动物与机械体不生成。
			if (!pawn.RaceProps.Humanlike)
			{
				return;
			}

			if (Get(pawn) != null)
			{
				return;
			}

			BodyPartRecord genitals = Genital_Helper.get_genitalsBPR(pawn);
			if (genitals == null || pawn.health.hediffSet.PartIsMissing(genitals))
			{
				return;
			}

			Hediff_PubicHair hediff = (Hediff_PubicHair)HediffMaker.MakeHediff(def, pawn, genitals);
			hediff.Severity = RandomDensity();
			pawn.health.AddHediff(hediff, genitals);
		}

		/// <summary>
		/// 随机密度：以 0.5 为中心，大多数小人落在中间几档，偶尔出现完全无毛或很浓密。
		/// </summary>
		[SyncMethod]
		public static float RandomDensity()
		{
			return Mathf.Clamp(Rand.Gaussian(0.5f, 0.22f), 0.02f, MaxDensity);
		}
	}
}
