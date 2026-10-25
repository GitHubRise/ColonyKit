#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理对死亡小人施加的惩罚。</para>
	/// <para>恋尸癖这一怪癖会调整此偏好，使它对该小人
	/// 更有利。</para>
	/// </summary>
	public sealed class P_DeadOrAlive : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅适用于性行为。小人可以深情地回忆死去的小人，
			// 但涉及到操他们的尸体时，那才关心
			// 他们是死是活。
			if (request.Purpose.IsNotForSex()) return;

			request.SetPreference(new P_DeadOrAlive());
		}

		private P_DeadOrAlive() : base(
			AttractionMode.Physical,
			nameof(P_DeadOrAlive),
			FactorOperation.Multiply,
			IsAlive,
			whenFalse: FixedValue(0.5f))
		{ }

		private static bool IsAlive(ref AttractionRequest request) => !request.Target.Dead;
	}
}
