#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理观察者的性取向。
	/// </summary>
	public sealed class R_Orientation : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request) =>
			request.SetPreference(new R_Orientation());

		private R_Orientation() : base(
			AttractionMode.Romantic,
			nameof(R_Orientation),
			FactorOperation.Multiply)
		{
			Priority = 100;
		}

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			OrientationUtility.PreferenceFactor(request.Pawn, request.Target);
	}
}
