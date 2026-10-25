#nullable disable

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// <para>处理两个小人之间的派系关系。</para>
	/// <para>这里也负责为动物添加在本派系内
	/// 配种的偏好。</para>
	/// </summary>
	public sealed class S_Faction : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 观察者必须有派系。
			if (request.Pawn.Faction is null) return;

			request.SetPreference(new S_Faction());
		}

		private S_Faction() : base(
			AttractionMode.Social,
			nameof(S_Faction),
			FactorOperation.Add,
			HasDifferingFaction,
			whenTrue: WithDifferingFaction,
			whenFalse: WithSameFaction)
		{ }

		private static bool HasDifferingFaction(ref AttractionRequest request)
		{
			// 不应发生，但如果某处错误地添加了此偏好，
			// 则将其视为与一切不同来处理。
			if (request.Pawn.Faction is not { } pFaction) return true;
			if (request.Target.Faction is not { } tFaction) return true;
			// 两个非类人派系的特殊情况。把它们视为相同。
			if (!pFaction.def.humanlikeFaction && !tFaction.def.humanlikeFaction) return false;
			return pFaction != tFaction;
		}

		private static float WithDifferingFaction(ref AttractionRequest request, float factor)
		{
			var target = request.Target;
			
			// 除非在强暴，小人一般不在意访客。
			// TODO：除非他们的意识形态另有主张，大概吧。
			if (!request.Purpose.IsForRape()) return 0f;

			// 动物在强暴时不考虑派系。
			if (request.Category.AnimalObserver()) return 0f;
			// 人类强暴者不会顾及无派系的动物。
			if (request.Category.AnimalTarget() && target.Faction is null) return 0f;

			return 0.2f;
		}
		
		// 使强暴者偏向强暴其他派系的小人。
		private static float WithSameFaction(ref AttractionRequest request, float factor)
		{
			// 轻微偏向，让小人更容易被本派系成员吸引。
			if (!request.Purpose.IsForRape()) return 0.1f;

			// 动物在强暴时不考虑派系。
			if (request.Category.AnimalObserver()) return 0f;

			return -0.2f;
		}
	}
}
