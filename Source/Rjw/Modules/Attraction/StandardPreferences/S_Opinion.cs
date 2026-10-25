#nullable disable

using Verse;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理观察者对目标的看法。
	/// </summary>
	public sealed class S_Opinion : CurveAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 只在可拥有关系时才处理。
			if (request.Pawn.relations is null) return;
			// 对于非人类目标，`OpinionOf` 被硬编码为直接返回零。
			// 不过动物显然仍能对人类抱有看法，
			// 不管出于什么原因。
			if (!request.Target.IsHumanLike()) return;
			// 精神变态不考虑自己对他人的看法。
			if (xxx.is_psychopath(request.Pawn)) return;

			request.SetPreference(new S_Opinion());
		}

		/// <summary>
		/// <para>曲线的点，将看法映射为要施加的系数。</para>
		/// <para>强暴时使用取反的看法值。</para>
		/// </summary>
		private static readonly CurvePoint[] opinionPoints = new CurvePoint[]
		{
			new(-100f, 0.5f),
			new(-50f, 0.8f),
			new(0f, 1f),
			new(50f, 1.25f),
			new(100f, 1.25f)
		};

		private S_Opinion() : base(
			AttractionMode.Social,
			nameof(S_Opinion),
			GetOpinon,
			opinionPoints)
		{ }

		private static float GetOpinon(ref AttractionRequest request)
		{
			var pawn = request.Pawn;
			var target = request.Target;
			if (pawn.relations?.OpinionOf(target) is not int opinion) return 0f;

			// 看法范围是 -100 到 100；强暴时翻转符号。
			if (request.Purpose.IsForRape()) opinion *= -1;
			return opinion;
		}
	}
}
