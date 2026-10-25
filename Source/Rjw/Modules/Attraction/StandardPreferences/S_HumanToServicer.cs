#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 非服务的人类小人对提供性服务的小人施加加成。
	/// </summary>
	public sealed class S_HumanToServicer : FlatAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅适用于性行为。
			if (request.Purpose.IsNotForSex()) return;
			// 不能涉及动物。
			if (request.Category.InvolvesAnimals()) return;
			// 且观察者不能是提供性服务的。
			if (request.Pawn.IsDesignatedService()) return;
			// 且目标必须是提供性服务的。
			if (!request.Target.IsDesignatedService()) return;

			request.SetPreference(new S_HumanToServicer());
		}

		private S_HumanToServicer() : base(
			AttractionMode.Social,
			nameof(S_HumanToServicer),
			FactorOperation.Add,
			0.2f)
		{ }
	}
}
