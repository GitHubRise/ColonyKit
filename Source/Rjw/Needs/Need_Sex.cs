#nullable disable

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	public class Need_Sex : Need_Seeker
	{
		public bool isInvisible => pawn.Map == null;

		private static float decay_per_day = 0.3f;

		// TODO：把这些阈值改成常量，或至少改成静态字段
		// 只读属性——如果它们是为打补丁而设计的。

		public float thresh_frustrated() => 0.05f;

		public float thresh_horny() => 0.25f;

		public float thresh_neutral() => 0.50f;

		public float thresh_satisfied() => 0.75f;

		public float thresh_ahegao() => 0.95f;

		public Need_Sex(Pawn pawn) : base(pawn)
		{
			//if (xxx.is_mechanoid(pawn)) return; //Added by nizhuan-jjr:Misc.Robots are not allowed to have sex, so they don't need sex actually.
			threshPercents = new List<float>
			{
				thresh_frustrated(),
				thresh_horny(),
				thresh_neutral(),
				thresh_satisfied(),
				thresh_ahegao()
			};
		}

		// 这些成员被重写，只是为了方便其他模组打补丁并改变其行为。
		// 若没有它，其他模组就得直接给 `Need` 打补丁，从而在执行新行为之前为
		// 游戏中的每一个需求都加上类型检查。
		public override float CurInstantLevel
		{
			get { return base.CurInstantLevel; }
		}

		public override float CurLevel
		{
			get { return base.CurLevel; }
			set { base.CurLevel = value; }
		}

		//public override bool ShowOnNeedList
		//{
		//	get
		//	{
		//		if (Genital_Helper.has_genitals(pawn))
		//			return true;

		//		ModLog.Message("curLevelInt " + curLevelInt);
		//		return false;
		//	}
		//}

		//public override string GetTipString()
		//{
		//	return string.Concat(new string[]
		//	{
		//		this.LabelCap,
		//		": ",
		//		this.CurLevelPercentage.ToStringPercent(),
		//		"\n",
		//		this.def.description,
		//		"\n",
		//	});
		//}

		public static float brokenbodyfactor(Pawn pawn)
		{
			//这里加入了残损身体系统
			float broken_body_factor = 1f;
			if (pawn.health.hediffSet.HasHediff(xxx.feelingBroken))
			{
				switch (pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).CurStageIndex)
				{
					case 0:
						return 0.75f;
					case 1:
						return 1.4f;
					case 2:
						return 2f;
				}
			}
			return broken_body_factor;
		}

		public static float druggedfactor(Pawn pawn)
		{
			if (pawn.health.hediffSet.HasHediff(RJWHediffDefOf.HumpShroomEffect))
			{
				//ModLog.Message("Need_Sex::druggedfactor 3 pawn is " + xxx.get_pawnname(pawn));
				return 3f;
			}

			// 没有 humpshroom 的效果，却上瘾了？
			if (pawn.health.hediffSet.HasHediff(RJWHediffDefOf.HumpShroomAddiction))
			{
				//ModLog.Message("Need_Sex::druggedfactor 0.5 pawn is " + xxx.get_pawnname(pawn));
				return 0.5f;
			}

			//ModLog.Message("Need_Sex::druggedfactor 1 pawn is " + xxx.get_pawnname(pawn));
			return 1f;
		}

		static float diseasefactor(Pawn pawn)
		{
			return 1f;
		}

		static float futafactor(Pawn pawn)
		{
			// 检查双倍生育力的扶她。
			// 大概是因为她们大脑中流淌着两倍的激素。
			return Genital_Helper.is_futa(pawn) ? 2.0f : 1.0f;
		}

		static float agefactor(Pawn pawn)
		{
			// 年龄检查已移到 `NeedInterval` 开头，此系数
			// 在原版 RJW 中已不再有用。保留它是为了以防某些
			// 模组会修改这个系数。
			return 1f;
		}

		/// <summary>
		/// 获取影响指定小人需求衰减的累加系数。
		/// </summary>
		public static float GetFallFactorFor(Pawn pawn)
		{
			return brokenbodyfactor(pawn) *
				druggedfactor(pawn) *
				diseasefactor(pawn) *
				agefactor(pawn) *
				futafactor(pawn);
		}

		static float fall_per_tick(Pawn pawn)
		{
			var fall_per_tick = decay_per_day / GenDate.TicksPerDay * GetFallFactorFor(pawn);
			//--ModLog.Message("Need_Sex::NeedInterval is called - pawn is " + xxx.get_pawnname(pawn) + " is has both genders " + (Genital_Helper.has_penis(pawn) && Genital_Helper.has_vagina(pawn)));
			//ModLog.Message(" " + xxx.get_pawnname(pawn) + "'s sex need stats:: fall_per_tick: " + fall_per_tick + ", sex_need_factor_from_lifestage: " + sex_need_factor_from_lifestage(pawn) );
			return fall_per_tick;
		}

		public override void NeedInterval()
		{
			if (isInvisible) return; // 商队中的小人跳过

			// 无性恋或年龄太小。
			if (xxx.is_asexual(pawn) || !xxx.can_do_loving(pawn))
			{
				CurLevel = 0.5f;
				return;
			}

			//--ModLog.Message("Need_Sex::NeedInterval is called0 - pawn is "+xxx.get_pawnname(pawn));

			if (!def.freezeWhileSleeping || pawn.Awake())
			{
				// `NeedInterval` 由 `Pawn_NeedsTracker` 每 150 tick 调用一次，约合 0.06 游戏小时。
				var fallPerCall = 150 * fall_per_tick(pawn);
				var sexDriveModifier = xxx.get_sex_drive(pawn);
				var decayRateModifier = RJWSettings.sexneed_decay_rate;
				var decayThisCall = fallPerCall * sexDriveModifier * decayRateModifier;
				CurLevel -= decayThisCall;

				// ModLog.Message($" {xxx.get_pawnname(pawn)}'s sex need stats:: Decay/call: {decayThisCall}, Dec. rate: {decayRateModifier}, Cur.lvl: {CurLevel}, Sex drive: {sexDriveModifier}");
			}

			//--ModLog.Message("Need_Sex::NeedInterval is called1");
		}
	}
}
