#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理因畸形而施加的惩罚。</para>
	/// <para>畸胎怪癖会把它变成加成。</para>
	/// </summary>
	public sealed class P_Disfigurement : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request) =>
			request.SetPreference(new P_Disfigurement());

		private P_Disfigurement() : base(
			AttractionMode.Physical,
			nameof(P_Disfigurement),
			FactorOperation.Multiply,
			IsDisfigured,
			whenTrue: FixedValue(0.8f))
		{ }

		private static bool IsDisfigured(ref AttractionRequest request) =>
			request.Target.IsDisfiguredTo(request.Pawn);
	}
}
