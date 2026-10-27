using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_Latrine 的移植：不接水管的旱厕，每次使用把 14 单位粪污堆进坑里，
	/// 坑满（500）后停用，必须由殖民者清空（WorkGiver_EmptyLatrine）才能继续用。
	///
	/// 继承 Building_Toilet，是为了直接复用 "is Building_Toilet" 的全部既有逻辑：
	/// JobGiver_UseToilet 的选点、JobDriver_UseToilet 的使用流程、Alert 的统计，
	/// 以及 RoomRoleWorker_PublicBathroom 的 fixture 归类。行为差异全部集中在
	/// 这里的水/电/粪坑覆盖上，避免再维护一套平行的马桶代码。
	/// </summary>
	public class Building_Latrine : Building_Toilet
	{
		/// <summary>屎坑容量（单位相当于升）；14/次，约 35 次一清。</summary>
		public const float PitCapacity = 500f;

		/// <summary>每次使用堆进坑里的量。旱厕不冲水，所以不受 waterUseMultiplier 影响。</summary>
		public const float PitPerUse = 14f;

		public float sewage;

		public bool PitFull => sewage >= PitCapacity;

		public float PitFillPercent => Mathf.Clamp01(sewage / PitCapacity);

		public override FixtureQuality Quality => FixtureQuality.shabby;

		/// <summary>旱厕不消耗水，Working() 也不会走供水检查。</summary>
		public override float waterUsed => 0f;

		/// <summary>没有水管也算有水：坑是干的，用水检查整条跳过。</summary>
		public override bool HasWaterSupply => true;

		public override bool PowerRequiredToUse => false;

		/// <summary>旱厕没有掀盖贴图，别沿用 Building_Toilet 的 CK_ToiletUp。</summary>
		public override Graphic Graphic => def.graphicData.Graphic;

		public override AcceptanceReport Working(float WaterUsed = 0f)
		{
			// 坑满时先挡住，再谈堵塞/供电；否则小人会一直排队等一个永远修不好的设施。
			if (PitFull)
			{
				return "CK.Hygiene.PitFull".Translate();
			}
			return base.Working(WaterUsed);
		}

		/// <summary>盖一次坑。返回 false 表示坑已满，本次使用无效。</summary>
		public override bool TryUseFlush()
		{
			if (PitFull)
			{
				return false;
			}
			sewage = Mathf.Min(sewage + PitPerUse, PitCapacity);
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
			// base 可能已经带了无法使用的理由（例如堵塞），这条放在最后一行。
			return text + "\n" + pit;
		}
	}
}
