#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 提供性服务的人类小人对非服务小人施加加成。
	/// </summary>
	public sealed class S_ServicerBias : FlatAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅适用于性行为。
			if (request.Purpose.IsNotForSex()) return;
			// 不能涉及动物（动物没有钱）。
			if (request.Category.InvolvesAnimals()) return;
			// 且观察者必须是提供性服务的。
			if (!request.Pawn.IsDesignatedService()) return;
			// 且目标不能是提供性服务的。
			if (request.Target.IsDesignatedService()) return;
			// 必须有不同派系。
			if (request.Pawn.Faction == request.Target.Faction) return;

			request.SetPreference(new S_ServicerBias());
		}

		private S_ServicerBias() : base(
			AttractionMode.Social,
			nameof(S_ServicerBias),
			FactorOperation.Add,
			0.2f)
		{ }
	}
}
