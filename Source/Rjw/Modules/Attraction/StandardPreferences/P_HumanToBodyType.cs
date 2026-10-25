#nullable disable

using Sex = rjw.GenderHelper.Sex;

namespace rjw.Modules.Attraction.StandardPreferences
{
	public record struct BodyTypeData(
		Orientation PawnOri,
		Sex PawnSex,
		StandardizedBody TargetBody
	);

	/// <summary>
	/// 处理目标的体型。
	/// </summary>
	public sealed class P_HumanToBodyType : SelectAttractionPreference<BodyTypeData>
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 不适用于动物观察者。
			if (request.Category.AnimalObserver()) return;

			request.SetPreference(new P_HumanToBodyType());
		}

		private P_HumanToBodyType() : base(
			AttractionMode.Physical,
			nameof(P_HumanToBodyType),
			FactorOperation.Multiply,
			GetBodyData,
			SelectFromBody)
		{ }

		private static BodyTypeData GetBodyData(ref AttractionRequest request) => new(
			request.Pawn.GetCompRJW().orientation.Simplify(),
			GenderHelper.GetSex(request.Pawn),
			AttractionUtility.StandardizeBody(request.Target)
		);

		private static float SelectFromBody(BodyTypeData data, ref AttractionRequest request)
		{
			float result = 1f;

			// 对未知体型不做任何处理。
			if (data.TargetBody == StandardizedBody.Unknown)
			{
				result = 1f;
			}
			// 对动物大幅降低（恋兽癖会覆盖此值）。
			else if (data.TargetBody == StandardizedBody.Animal || data.TargetBody == StandardizedBody.Insectoid)
			{
				if (!xxx.is_zoophile(request.Pawn))
				{
					result = 0.45f;
				}
				else
				{
					result = 1f;
				}
				
			}
			// 对爆腹机器大幅降低。
			else if (data.TargetBody == StandardizedBody.Mechanoid)
			{
				result = 0.45f;
			}
			// 对幼态体型不提供吸引力加成。
			else if (data.TargetBody == StandardizedBody.Baby || data.TargetBody == StandardizedBody.Child)
			{
				result = 1f;
			}
			// 不在意肥胖小人的性取向。
			else if (data.PawnOri == Orientation.Asexual)
			{
				result = 1f;
			}
			else if (data.PawnOri == Orientation.Pansexual)
			{
				result = 1.25f;
			}
			// 肥胖小人没有加成（畸胎怪癖会覆盖此值）。
			else if (data.TargetBody == StandardizedBody.Fat)
			{
				result = 1f;
			}
			// 双性恋两者都喜欢。
			else if (data.PawnOri == Orientation.Bisexual &&
					 (data.TargetBody == StandardizedBody.Male || data.TargetBody == StandardizedBody.Female))
			{
				result = 1.25f;
			}
			// 把扶她视同双性恋。
			else if (data.PawnSex == Sex.Futa &&
					 (data.TargetBody == StandardizedBody.Male || data.TargetBody == StandardizedBody.Female))
			{
				result = 1.25f;
			}
			// 具有特定性向的男性。
			else if (data.PawnOri == Orientation.Heterosexual &&
					 (data.PawnSex == Sex.Male || data.PawnSex == Sex.Trap) &&
					 data.TargetBody == StandardizedBody.Female)
			{
				result = 1.25f;
			}
			else if (data.PawnOri == Orientation.Homosexual &&
					 (data.PawnSex == Sex.Male || data.PawnSex == Sex.Trap) &&
					 data.TargetBody == StandardizedBody.Male)
			{
				result = 1.25f;
			}
			// 具有特定性向的女性。
			else if (data.PawnOri == Orientation.Heterosexual &&
					 data.PawnSex == Sex.Female &&
					 data.TargetBody == StandardizedBody.Male)
			{
				result = 1.25f;
			}
			else if (data.PawnOri == Orientation.Homosexual &&
					 data.PawnSex == Sex.Female &&
					 data.TargetBody == StandardizedBody.Female)
			{
				result = 1.25f;
			}

			// 默认值已被初始赋值覆盖（1f 或 1.1f，取决于你的意图）
			return result;
		}
	}
}
