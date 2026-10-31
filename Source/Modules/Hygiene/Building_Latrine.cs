using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_Latrine 的移植：纯旱厕，不接水管也不接污水管，
	/// 每次使用把 14 堆进坑里，坑满 500 后必须由殖民者清空（清出粪渣）才能继续用。
	/// 和马桶/浴缸那种「自带水管 + 污水管」的洁具不同，旱厕完全靠人处理。
	///
	/// 继承 Building_Toilet 是为了复用选点、使用流程、告警与浴室房间判定的既有逻辑。
	/// </summary>
	public class Building_Latrine : Building_Toilet
	{
		/// <summary>粪坑容量，与 DBH 的 sewageLimit 一致。</summary>
		public const float SewageLimit = 500f;

		/// <summary>每次使用堆进坑里的量，与 DBH 的 flushSize 一致。</summary>
		public const float SewagePerUse = 14f;

		public float sewage;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		/// <summary>旱厕不消耗水。</summary>
		public override float waterUsed => 0f;

		/// <summary>没有水管也算「有水」：旱厕不参与水网检查。</summary>
		public override bool HasWaterSupply => true;

		public override bool PowerRequiredToUse => false;

		/// <summary>旱厕没有掀盖贴图，别沿用 Building_Toilet 的 CK_ToiletUp。</summary>
		public override Graphic Graphic => def.graphicData.Graphic;

		public bool PitFull => sewage >= SewageLimit;

		public float PitFillPercent => Mathf.Clamp01(sewage / SewageLimit);

		public override AcceptanceReport Working(float WaterUsed = 0f)
		{
			// 只看粪坑还装不装得下；没有水网/电网参与。
			return PitFull ? "CK.Hygiene.PitFull".Translate() : AcceptanceReport.WasAccepted;
		}

		/// <summary>盖一次坑。返回 false 表示坑已满，本次使用无效。</summary>
		public override bool TryUseFlush()
		{
			if (PitFull)
			{
				return false;
			}
			sewage = Mathf.Min(sewage + SewagePerUse, SewageLimit);
			// 旱厕没有水，用「如厕」声代替冲水声。
			DefDatabase<SoundDef>.GetNamed("CK_Defecate").PlayOneShot(this);
			FleckDef sewageMote = DefDatabase<FleckDef>.GetNamedSilentFail("CK_MoteSewage");
			if (sewageMote != null)
			{
				FleckMaker.Static(Position.ToVector3Shifted(), Map, sewageMote, 1f);
			}
			return true;
		}

		/// <summary>清空作业完工时调用；清到 0 时顺手把浮点残渣抹平。</summary>
		public float Empty(float amount)
		{
			float drained = Mathf.Min(sewage, amount);
			sewage -= drained;
			if (sewage < 0.01f)
			{
				sewage = 0f;
			}
			// 掏空粪坑时来一声冲水音，给玩家「清掉了」的反馈。
			if (drained > 0f)
			{
				DefDatabase<SoundDef>.GetNamed("CK_ToiletFlush").PlayOneShot(this);
			}
			return drained;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref sewage, "sewage", 0f);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			// 坑满时 base 已经把 Working() 的理由（坑满）写进去了，不再重复一行。
			if (!Spawned || PitFull)
			{
				return text;
			}
			string pit = "CK.Hygiene.PitLevel".Translate(PitFillPercent.ToStringPercent("0")).ToString();
			if (text.NullOrEmpty())
			{
				return pit;
			}
			return text + "\n" + pit;
		}
	}
}
