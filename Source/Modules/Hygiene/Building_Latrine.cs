using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_Latrine 的完整移植（含逻辑与数值）：
	/// 没接污水网时是干厕所，每次使用把 14 堆进坑里，坑满 500 后必须清空（清出粪渣）；
	/// 一旦水网与污水网都接通、且网上有没堵的化粪池/污水处理设施，就按 DBH 的规则
	/// 当冲水厕所用：先 PushSewage（连坑里存货一起推）成功，才抽 14 的水。
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

		private CompPipe sewagePipe;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		/// <summary>坑里每多一次使用就多 14；接上污水网后这 14 同时也是冲水量。</summary>
		public override float waterUsed => SewagePerUse * HygieneSettings.Current.waterUseMultiplier;

		/// <summary>旱厕本身不依赖供水；接网后由 Working() 里的 base 检查水网。</summary>
		public override bool HasWaterSupply => true;

		public override bool PowerRequiredToUse => false;

		/// <summary>旱厕没有掀盖贴图，别沿用 Building_Toilet 的 CK_ToiletUp。</summary>
		public override Graphic Graphic => def.graphicData.Graphic;

		public bool PitFull => sewage >= SewageLimit;

		public float PitFillPercent => Mathf.Clamp01(sewage / SewageLimit);

		public CompPipe SewagePipe
		{
			get
			{
				if (sewagePipe == null)
				{
					foreach (CompPipe candidate in GetComps<CompPipe>())
					{
						if (candidate.mode == PipeType.Sewage)
						{
							sewagePipe = candidate;
							break;
						}
					}
				}
				return sewagePipe;
			}
		}

		/// <summary>水管与污水管都接通，且污水网上有可用设施：按 DBH 当冲水厕所。</summary>
		public bool Plumbed
		{
			get
			{
				PlumbingNet waterNet = pipe?.pipeNet;
				PlumbingNet sewageNet = SewagePipe?.pipeNet;
				if (waterNet == null || sewageNet == null)
				{
					return false;
				}
				for (int i = 0; i < sewageNet.Sewers.Count; i++)
				{
					CompSewageHandler sewer = sewageNet.Sewers[i];
					if (sewer != null && !sewer.Blocked)
					{
						return true;
					}
				}
				return false;
			}
		}

		public override AcceptanceReport Working(float WaterUsed = 0f)
		{
			if (Plumbed)
			{
				return base.Working(WaterUsed);
			}
			// 没接污水网：干厕所，只看粪坑还装不装得下。
			return PitFull ? "CK.Hygiene.PitFull".Translate() : AcceptanceReport.WasAccepted;
		}

		public override bool TryUseFlush()
		{
			if (!Plumbed)
			{
				if (PitFull)
				{
					return false;
				}
				sewage = Mathf.Min(sewage + waterUsed, SewageLimit);
				return true;
			}

			// DBH 的顺序：先把坑里的存货和这次的一起推给污水网，推成功才算这次使用，
			// 然后再抽水；抽不到水时污水已经走了，和原版行为一致。
			float load = Mathf.Min(sewage + waterUsed, SewageLimit);
			CompPipe sewer = SewagePipe;
			if (sewer?.pipeNet == null || !sewer.pipeNet.PushSewage(load))
			{
				return false;
			}
			sewage = 0f;
			return pipe.pipeNet.PullWater(waterUsed, out _);
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
			// 坑满且没接网时 base 已经写了「粪坑已满」，不再重复。
			if (!Spawned || (PitFull && !Plumbed))
			{
				return text;
			}
			string line = Plumbed
				? "CK.Hygiene.LatrinePlumbed".Translate().ToString()
				: "CK.Hygiene.PitLevel".Translate(PitFillPercent.ToStringPercent("0")).ToString();
			if (text.NullOrEmpty())
			{
				return line;
			}
			return text + "\n" + line;
		}
	}
}
