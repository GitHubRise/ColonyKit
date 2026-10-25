#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理小人之间的敌对关系。
	/// </summary>
	public sealed class S_Hostility : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request) =>
			request.SetPreference(new S_Hostility());

		private S_Hostility() : base(
			AttractionMode.Social,
			nameof(S_Hostility),
			FactorOperation.Add,
			AttractionUtility.HasHostility,
			whenTrue: WithHostility,
			whenFalse: WithoutHostility)
		{ }

		private static float WithHostility(ref AttractionRequest request, float factor) =>
			request.Purpose.IsForRape() ? 0.2f : -0.4f;
		
		private static float WithoutHostility(ref AttractionRequest request, float factor)
		{
			// 动物不在意其他非敌对的动物。
			if (request.Category.AnimalObserver()) return 0f;
			return request.Purpose.IsForRape() ? -0.2f : 0.1f;
		}
	}
}
