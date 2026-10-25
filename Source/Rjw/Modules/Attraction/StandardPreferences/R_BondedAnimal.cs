#nullable disable

using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理人与动物之间的羁绊。</para>
	/// <para>恋兽癖会把它调整为更大的加成，因为他们将其
	/// 视为真正的恋爱关系。</para>
	/// </summary>
	public sealed class R_BondedAnimal : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 仅用于兽交。
			if (!request.Category.IsBestiality()) return;
			// 精神变态不在意动物关系的细微之处。
			if (xxx.is_psychopath(request.Pawn)) return;

			request.SetPreference(new R_BondedAnimal());
		}

		private R_BondedAnimal() : base(
			AttractionMode.Romantic,
			nameof(R_BondedAnimal),
			FactorOperation.Multiply,
			IsBondedAnimal,
			whenTrue: FixedValue(1.1f))
		{ }

		private static bool IsBondedAnimal(ref AttractionRequest request) =>
			request.Relations.Contains(PawnRelationDefOf.Bond);
	}
}
