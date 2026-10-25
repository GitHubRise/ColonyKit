#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理观察小人与目标之间存在心灵爱恋联系
	/// 的情况。</para>
	/// <para>在 RimWorld 的 `RelationsUtility` 中心灵爱恋极其强大。</para>
	/// </summary>
	public sealed class R_PsychicLove : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 必须与目标有心灵爱恋。
			if (!request.Pawn.HasPsychicLoveWith(request.Target)) return;

			request.SetPreference(new R_PsychicLove());
		}

		private R_PsychicLove() : base(
			AttractionMode.Romantic,
			nameof(R_PsychicLove),
			FactorOperation.Multiply)
		{
			Priority = 50;
		}

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			request.Purpose switch
			{
				// 爱的力量把你的怒火引入我的骨盆！
				AttractionPurpose.ForRape when xxx.is_masochist(request.Target) => 1.25f,
				// 爱的力量迫使你停下！
				AttractionPurpose.ForRape => 0f,
				// 爱的力量迫使你只想着我！
				_ => 10f
			};
	}
}
