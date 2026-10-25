#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理两个不同物种的动物，检查它们的
	/// 性器官（可能还有其他特征）以判断它们是否可能
	/// 属于相近物种，比如狗和狼。如果不同，则会
	/// 施加惩罚。</para>
	/// <para>人类始终被视为动物的兼容配偶，
	/// 因为这样好玩！所以这里不做检查。</para>
	/// </summary>
	public sealed class P_AnimalToCompatibleSpecies : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 观察者必须是动物。
			if (!request.Category.AnimalObserver()) return;

			request.SetPreference(new P_AnimalToCompatibleSpecies());
		}

		private P_AnimalToCompatibleSpecies() : base(
			AttractionMode.Physical,
			nameof(P_AnimalToCompatibleSpecies),
			FactorOperation.Multiply,
			IsCompatibleSpecies,
			whenFalse: FixedValue(0.4f))
		{ }

		private static bool IsCompatibleSpecies(ref AttractionRequest request) =>
			RaceGroupDef_Helper.IsCompatibleForMating(request.Pawn, request.Target);
	}
}
