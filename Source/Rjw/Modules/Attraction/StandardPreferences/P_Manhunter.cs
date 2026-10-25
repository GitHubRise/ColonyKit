#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 猎杀人类的动物就是强暴机器。有何不可？
	/// </summary>
	public sealed class P_Manhunter : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅用于性行为。
			if (request.Purpose.IsNotForSex()) return;
			// 观察者必须是猎杀人类的动物。
			if (!request.Category.AnimalObserver()) return;
			if (!request.Pawn.IsManhunter()) return;

			request.SetPreference(new P_Manhunter());
		}

		private P_Manhunter() : base(
			AttractionMode.Physical,
			nameof(P_Manhunter),
			FactorOperation.Multiply,
			IsManToHunt,
			whenTrue: FixedValue(3f),
			whenFalse: FixedValue(0f))
		{
			Priority = 50;
		}

		private static bool IsManToHunt(ref AttractionRequest request) =>
			request.Target.IsHumanLike();
	}
}
