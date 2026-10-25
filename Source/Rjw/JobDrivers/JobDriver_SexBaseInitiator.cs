#nullable disable

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace rjw
{
	public abstract class JobDriver_SexBaseInitiator : JobDriver_Sex
	{
		/// <summary>同一场里由本发起者「同时」服务的额外接受者（双飞：男方插入两人）。</summary>
		public List<Pawn> extraReceivers = new();

		private bool extrasSettled;

		/// <summary>下一次「调情/play」环节的 tick；-1 表示尚未安排。</summary>
		public int nextFlirtTick = -1;

		private bool groupSexExtended;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref extraReceivers, "extraReceivers", LookMode.Reference);
			Scribe_Values.Look(ref nextFlirtTick, "nextFlirtTick", -1);
			Scribe_Values.Look(ref groupSexExtended, "groupSexExtended", false);
		}

		public void AddExtraReceiver(Pawn receiver)
		{
			if (receiver == null) return;
			extraReceivers ??= new List<Pawn>();
			extraReceivers.AddDistinct(receiver);

			// 第一次变成 3 人场时随机延长 2–4 倍，并安排调情环节。
			if (!groupSexExtended)
			{
				groupSexExtended = true;
				ExtendForGroupSex();
			}
		}

		/// <summary>多人场：把剩余时长与高潮进度整体拉长 2–4 倍。</summary>
		private void ExtendForGroupSex()
		{
			float factor = Rand.Range(2f, 4f);

			duration = (int)(duration * factor);
			ticks_left = (int)(ticks_left * factor);
			sex_ticks = (int)(sex_ticks * factor);
			orgasmStartTick = (int)(orgasmStartTick * factor);

			nextFlirtTick = Find.TickManager.TicksGame + Rand.RangeInclusive(300, 700);

			if (RJWSettings.DevMode)
				ModLog.Message($"[GroupSex] duration extended x{factor:0.00}");
		}

		/// <summary>多人场里定期来一次调情/play：给随机一位接受者与发起者冒爱心。</summary>
		public void GroupSexFlirtTick(Pawn mainReceiver)
		{
			if (nextFlirtTick < 0 || Find.TickManager.TicksGame < nextFlirtTick) return;
			if (extraReceivers == null || extraReceivers.Count == 0) return;

			nextFlirtTick = Find.TickManager.TicksGame + Rand.RangeInclusive(400, 900);

			Pawn chosen = extraReceivers.Where(p => p != null && !p.Dead).RandomElement() ?? mainReceiver;
			if (chosen == null) return;

			ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
			if (chosen.Map == pawn.Map)
				ThrowMetaIconF(chosen.Position, chosen.Map, FleckDefOf.Heart);

			pawn.GainComfortFromCellIfPossible(1);
			chosen.GainComfortFromCellIfPossible(1);
		}

		/// <summary>结束额外接受者的 job，并按每一对补做一次结算。</summary>
		public void SettleExtraReceivers()
		{
			if (extrasSettled) return;
			extrasSettled = true;

			if (extraReceivers == null || extraReceivers.Count == 0) return;

			foreach (Pawn extra in extraReceivers)
			{
				if (extra == null) continue;

				if (extra.jobs?.curDriver is JobDriver_SexBaseReciever receiver)
					receiver.parteners.Remove(pawn);

				if (extra.Dead) continue;

				try
				{
					SexProps props = SexUtility.SelectSextype(pawn, extra, false, false);
					SexUtility.ProcessSex(props);
				}
				catch (Exception exception)
				{
					ModLog.Warning($"[GroupSex] failed to settle extra receiver {extra.LabelShort}: {exception.Message}");
				}
			}

			extraReceivers.Clear();
		}


		public void Start()
		{
			bool isRape = pawn?.jobs?.curDriver is JobDriver_Rape || Partner?.jobs?.curDriver is JobDriver_SexBaseRecieverRaped;
			bool isWhoring = pawn?.CurJob?.def == xxx.whore_is_serving_visitors;
			bool isMechImplant = pawn?.jobs?.curDriver is JobDriver_RapeEnemyByMech;
			Pawn partner = Partner;
			var receiverDriver = partner?.jobs?.curDriver as JobDriver_SexBaseReciever;

			isAnimalOnAnimal = xxx.is_animal(pawn) && xxx.is_animal(partner);
			if (partner == null || partner == pawn)
			{
				// 这里可以放你的广告
			}
			else if (partner.Dead)
			{
				isRape = true;
			}
			else if (receiverDriver is not null)
			{
				receiverDriver.parteners.AddDistinct(pawn);

				//防止已倒地的接受者站起来打断强暴
				if (partner.health?.hediffSet?.GetFirstHediffOfDef(xxx.submitting) is Hediff_Submitting subHediff)
				{
					var props = xxx.submitting.CompProps<HediffCompProperties_Disappears>();
					var hediffComp = subHediff.TryGetComp<HediffComp_Disappears>();
					int range = props.disappearsAfterTicks.RandomInRange;
					hediffComp.ticksToDisappear = Mathf.Min(hediffComp.ticksToDisappear + range, props.disappearsAfterTicks.max);
				}

				//(Target.jobs.curDriver as JobDriver_SexBaseReciever).parteners.Count; //TODO: add multipartner support so sex doesn't repeat, maybe, someday

				//开关
				NymphSucc();
				RoMSucc();
				NISucc();
			}

			if (Sexprops == null)
			{
				//ModLog.Message("rulePack1: " + Sexprops);
				Sexprops = pawn.GetRMBSexPropsCache();
				//ModLog.Message("rulePack2: " + Sexprops);
				pawn.GetRJWPawnData().SexProps = null;
				//ModLog.Message("rulePack3: " + Sexprops);
				//ModLog.Message("rulePack4: " + pawn.GetRJWPawnData().SexProps);

				if (Sexprops == null)
				{
					Sexprops = SexUtility.SelectSextype(pawn, partner, isRape, isWhoring, isMechImplant);
				}
				Sexprops.isRevese = Sexprops.interaction.HasInteractionTag(SexInteractionTag.Reverse);
			}

			if (receiverDriver is not null && receiverDriver.parteners.Count == 1 && receiverDriver.Sexprops == null)
			{
				receiverDriver.Sexprops = Sexprops.GetForPartner();
			}
			Sexprops.interaction.AddToPlayLog(Sexprops.pawn, Sexprops.partner);
			if (RJWPreferenceSettings.sex_wear == RJWPreferenceSettings.Clothing.Clothed) return;
			var comp = pawn.GetCompRJW();
			if (comp != null)
			{
				comp.drawNude = true;
				pawn.Drawer.renderer.SetAllGraphicsDirty();
			}

			if(partner != null)
			{
				comp = partner.GetCompRJW();
				if (comp != null)
				{
					comp.drawNude = true;
					partner.Drawer.renderer.SetAllGraphicsDirty();
				}
			}
		}

		//public void Change(xxx.rjwSextype sexType)
		//{
		//	if (pawn.jobs?.curDriver is JobDriver_SexBaseInitiator)
		//	{
		//		(pawn.jobs.curDriver as JobDriver_SexBaseInitiator).increase_time(duration);
		//		Sexprops = SexUtility.SelectSextype(pawn, Partner, isRape, isWhoring, Partner);
		//		sexType = Sexprops.SexType;
		//		SexUtility.LogSextype(Sexprops.Giver, Sexprops.Reciever, Sexprops.RulePack, Sexprops.DictionaryKey);
		//	}
		//	if (Partner.jobs?.curDriver is JobDriver_SexBaseReciever)
		//	{
		//		(Partner.jobs.curDriver as JobDriver_SexBaseReciever).increase_time(duration);
		//		Sexprops = SexUtility.SelectSextype(pawn, Partner, isRape, isWhoring, Partner);
		//		sexType = Sexprops.SexType;
		//		SexUtility.LogSextype(Sexprops.Giver, Sexprops.Reciever, Sexprops.RulePack, Sexprops.DictionaryKey);
		//	}
		//	sexType = sexType
		//}

		public void End()
		{
			if (pawn?.GetCompRJW() is CompRJW comp)
			{
				comp.drawNude = false;
				pawn.Drawer.renderer.SetAllGraphicsDirty();
			}

			if (Partner?.GetCompRJW() is CompRJW partnerComp && Partner.Dead)
			{
				partnerComp.drawNude = false;
				partnerComp.keep_hat_on = false;
				Partner.Drawer.renderer.SetAllGraphicsDirty();
			}

			GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

			if (Partner?.jobs?.curDriver is JobDriver_SexBaseReciever receiverDriver)
			{
				if (receiverDriver.parteners.Count == 1 && !Sexprops.isCoreLovin)
				{
					var isPenetrative = Sexprops.sexType switch
					{
						xxx.rjwSextype.Anal => true,
						xxx.rjwSextype.Vaginal => true,
						xxx.rjwSextype.DoublePenetration => true,
						_ => false
					};
					if (!isPenetrative) goto notKnotting;

					// 反向互动，检查插入方伙伴能否打结
					var ableToKnot = Sexprops.isRevese ? canKnott(Partner) : canKnott(pawn);
					if (!ableToKnot) goto notKnotting;

					pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(xxx.knotted, Partner));
					Partner.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(xxx.knotted, pawn));
				}

				notKnotting:
				receiverDriver.parteners.Remove(pawn);
			}

			SettleExtraReceivers();
		}

		public bool canKnott(Pawn pawn)
		{
			// 恋尸情况下，死亡的小人永远无法打结。
			if (pawn.Dead) return false;

			foreach (var part in pawn.GetGenitalsList().OfType<ISexPartHediff>())
				if (part.Def.partTags.Contains("Knotted")) return true;
			return false;
		}

		/// <summary>
		/// 非魅魔的专注值获取
		/// </summary>
		public void NymphSucc()
		{
			if (MeditationFocusTypeAvailabilityCache.PawnCanUse(pawn, xxx.SexMeditationFocus))
			{
				shouldGainFocus = true;
				SexUtility.OffsetPsyfocus(pawn, 0.01f);
			}
			else if (xxx.is_zoophile(pawn) && xxx.is_animal(Partner) && MeditationFocusTypeAvailabilityCache.PawnCanUse(pawn, MeditationFocusDefOf.Natural))
			{
				shouldGainFocus = true;
			}

			if (MeditationFocusTypeAvailabilityCache.PawnCanUse(Partner, xxx.SexMeditationFocus))
			{
				shouldGainFocusP = true;
			}
			else if (xxx.is_zoophile(Partner) && xxx.is_animal(pawn) && MeditationFocusTypeAvailabilityCache.PawnCanUse(Partner, MeditationFocusDefOf.Natural))
			{
				shouldGainFocusP = true;
			}
		}

		/// <summary>
		/// Rimworld of Magic 魅魔的专注值获取
		/// </summary>
		public void RoMSucc()
		{
			if (xxx.RoMIsActive)
			{
				if (xxx.has_traits(pawn))
					if (pawn.story.traits.HasTrait(xxx.Succubus))
					{
						isSuccubus = true;
					}

				if (xxx.has_traits(Partner))
					if (Partner.story.traits.HasTrait(xxx.Succubus))
					{
						isSuccubusP = true;
					}
			}
		}
		/// <summary>
		/// Nightmare Incarnation 魅魔的专注值获取
		/// </summary>
		public void NISucc()
		{
			if (xxx.NightmareIncarnationIsActive)
			{
				if (xxx.has_traits(pawn))
					foreach (var x in pawn.AllComps?.Where(x => x?.props?.ToStringSafe() == "NightmareIncarnation.CompProperties_SuccubusRace"))
					{
						isSuccubus = true;
						break;
					}

				if (xxx.has_traits(Partner))
					foreach (var x in Partner.AllComps?.Where(x => x?.props?.ToStringSafe() == "NightmareIncarnation.CompProperties_SuccubusRace"))
					{
						isSuccubusP = true;
						break;
					}
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			//ModLog.Message("shouldreserve " + shouldreserve);
			if (shouldreserve && Target != null)
				return pawn.Reserve(Target, job, xxx.max_sex_partners_for(pawn, Target as Pawn), stackCount, null, errorOnFailed);
			else if (shouldreserve && Bed != null)
				return pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
			else
				return true; // 无需预定。

			//return this.pawn.Reserve(this.Partner, this.job, 1, 0, null) && this.pawn.Reserve(this.Bed, this.job, 1, 0, null);
		}
	}
}
