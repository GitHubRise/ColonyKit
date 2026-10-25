#nullable disable

using Verse;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理两个小人之间的意识形态。
	/// </summary>
	public sealed class S_Ideology : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 需要意识形态。
			if (!ModsConfig.IdeologyActive) return;
			// 观察者必须拥有意识形态。
			if (request.Pawn.Ideo is null) return;
			// 动物不可能有意识形态，所以不必费心。
			if (request.Category.AnimalTarget()) return;

			request.SetPreference(new S_Ideology());
		}

		private S_Ideology() : base(
			AttractionMode.Social,
			nameof(S_Ideology),
			FactorOperation.Add,
			HasDifferingIdeo,
			whenTrue: WithDifferingIdeo,
			whenFalse: WithSameIdeo)
		{ }

		private static bool HasDifferingIdeo(ref AttractionRequest request)
		{
			// 不应发生，但如果某处错误地添加了此偏好，
			// 则默认将其视为不同来处理。
			if (request.Pawn.Ideo is not { } pIdeo) return true;
			if (request.Target.Ideo is not { } tIdeo) return true;
			return pIdeo != tIdeo;
		}

		private static float WithDifferingIdeo(ref AttractionRequest request, float factor)
		{
			// 强暴者喜欢用猛干让异教徒开窍。
			if (request.Purpose.IsForRape()) return 0.2f;
			
			// 除非在强暴，小人一般不在意意识形态不同。
			// TODO：除非他们的意识形态另有主张，大概吧。
			return 0f;
		}
		
		// 使强暴者偏向强暴其他派系的小人。
		private static float WithSameIdeo(ref AttractionRequest request, float factor)
		{
			// 强暴者对想法相近的小人稍友好一些。
			if (request.Purpose.IsForRape()) return -0.1f;

			// 小人会被想法相近的小人吸引。
			return 0.1f;
		}
	}
}
