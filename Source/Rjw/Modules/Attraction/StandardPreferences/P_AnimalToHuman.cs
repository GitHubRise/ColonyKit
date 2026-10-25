#nullable disable

using Verse;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理动物观察类人小人的情况。它们越有宠物感，
	/// 就越会对人类感兴趣。
	/// </summary>
	public sealed class P_AnimalToHuman : AttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅用于动物对人类的请求。
			if (request.Category is not AttractionCategory.AnimalToHuman) return;
			// 目标必须明确是类人（而不是机器人／机械体）。
			if (!request.Target.IsHumanLike()) return;

			request.SetPreference(new P_AnimalToHuman());
		}

		private P_AnimalToHuman() : base(
			AttractionMode.Physical,
			nameof(P_AnimalToHuman),
			FactorOperation.Add)
		{ }

		protected override float GetOperand(ref AttractionRequest request, float factor) =>
			GenMath.LerpDouble(0f, 1f, -0.25f, 0.25f, request.Pawn.RaceProps.petness);
	}
}
