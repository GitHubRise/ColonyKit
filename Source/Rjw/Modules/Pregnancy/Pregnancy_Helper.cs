#nullable disable

using RimWorld;
using Verse;
using System;
using System.Linq;
using System.Collections.Generic;
using Multiplayer.API;
using rjw.Modules.Interactions;
using static rjw.Hediff_BasePregnancy;
using rjw.Modules.Shared.Logs;


namespace rjw
{
	/// <summary>
	/// 此方法负责在可用的不同类型怀孕之间进行选择
	/// 1：类人生物的 RJW 怀孕
	/// 2：兽交的 RJW 怀孕
	/// 3：虫族的 RJW 怀孕
	/// 4：机械族的 RJW 怀孕
	/// </summary>

	public static class PregnancyHelper
	{
		// TODO：将此方法以及所有 HediifDef.Named 调用移至合适的 DefOf 类
		private static HediffDef RJW_IUD = HediffDef.Named("RJW_IUD");

		//由 aftersex 调用（包括强暴、配种等）
		//由 mcevent 调用

		//pawn 为“父亲”；partner 为母亲

		//TODO：这里需要重写，以考虑接受方的群交（props？）
		public static void impregnate(SexProps props)
		{
			if (props == null) return;

			if (props.pawn == null || props.partner == null)
			{
				ImpregnateCore(props);
				return;
			}

			// 群交：父亲从所有参与者中挑选，而不是固定第一个发起者。
			Pawn father = PickGroupFather(props);
			if (father == null || father == props.pawn)
			{
				ImpregnateCore(props);
				return;
			}

			Pawn originalInitiator = props.pawn;
			props.pawn = father;
			try
			{
				ImpregnateCore(props);
			}
			finally
			{
				props.pawn = originalInitiator;
			}
		}

		/// <summary>
		/// 若母亲正在与多个发起者进行性行为，从"参与者 + 本次发起者"中随机挑一个能致孕的父亲。
		/// 单人行为、非人类、或没有合适候选时返回 <c>null</c>，交由原逻辑处理。
		/// </summary>
		private static Pawn PickGroupFather(SexProps props)
		{
			if (props.pawn == null || props.partner == null) return null;
			if (!xxx.is_human(props.pawn) || !xxx.is_human(props.partner)) return null;
			if (props.partner.jobs?.curDriver is not JobDriver_SexBaseReciever receiver) return null;
			if (receiver.parteners == null || receiver.parteners.Count < 2) return null;

			var candidates = new List<Pawn>();
			foreach (Pawn participant in receiver.parteners)
			{
				if (participant == null || participant.Dead || participant == props.partner) continue;
				if (!Genital_Helper.has_penis_fertile(participant)) continue;
				candidates.Add(participant);
			}

			// 发起者可能已经从 parteners 里移除（结束顺序），补回来。
			if (!candidates.Contains(props.pawn) && Genital_Helper.has_penis_fertile(props.pawn))
				candidates.Add(props.pawn);

			if (candidates.Count == 0) return null;
			return candidates.RandomElement();
		}

		private static void ImpregnateCore(SexProps props)
		{

			if (RJWSettings.DevMode) ModLog.Message("Rimjobworld::impregnate(" + props.sexType + "):: " + xxx.get_pawnname(props.pawn) + " + " + xxx.get_pawnname(props.partner) + ":");

			//“机械族”怀孕
			if (props.isMechImplant && props.pawn?.jobs?.curDriver is JobDriver_RapeEnemyByMech)
			{
				if (RJWPregnancySettings.mechanoid_pregnancy_enabled && xxx.is_mechanoid(props.pawn))
				{
					// 移除旧的怀孕
					var p = GetPregnancies(props.partner);
					if (!p.NullOrEmpty())
					{
						var i = p.Count;
						while (i > 0)
						{
							i -= 1;
							var h = GetPregnancies(props.partner);
							if (h[i] is Hediff_MechanoidPregnancy)
							{
								if (RJWSettings.DevMode) ModLog.Message(" already pregnant by mechanoid");
							}
							else if (h[i] is Hediff_BasePregnancy)
							{
								if (RJWSettings.DevMode) ModLog.Message(" removing rjw normal pregnancy");
								(h[i] as Hediff_BasePregnancy).Kill();
							}
							else
							{
								if (RJWSettings.DevMode) ModLog.Message(" removing vanilla or other mod pregnancy");
								props.partner.health.RemoveHediff(h[i]);
							}
						}
					}

					// 新的怀孕
					if (RJWSettings.DevMode) ModLog.Message(" mechanoid pregnancy started");
					Hediff_MechanoidPregnancy hediff = Hediff_BasePregnancy.Create<Hediff_MechanoidPregnancy>(props.partner, props.pawn);
					return;

					/*
					// Not an actual pregnancy. This implants mechanoid tech into the target.
					//may lead to pregnancy
					//old "chip pregnancies", maybe integrate them somehow?
					//Rand.PopState();
					//Rand.PushState(RJW_Multiplayer.PredictableSeed());
					HediffDef_MechImplants egg = (from x in DefDatabase<HediffDef_MechImplants>.AllDefs	select x).RandomElement();
					if (egg != null)
					{
						if (RJWSettings.DevMode) ModLog.Message(" planting MechImplants:" + egg.ToString());
						PlantSomething(egg, partner, !Genital_Helper.has_vagina(partner), 1);
						return;
					}
					else
					{
						if (RJWSettings.DevMode) ModLog.Message(" no mech implant found");
					}*/
				}
				return;
			}

			//“卵生”（ovi）怀孕/产卵
			var AnalOk = props.sexType == xxx.rjwSextype.Anal && RJWPregnancySettings.insect_anal_pregnancy_enabled;
			var OralOk = props.sexType == xxx.rjwSextype.Oral && RJWPregnancySettings.insect_oral_pregnancy_enabled;

			Pawn giver = props.pawn; // 高潮者
			Pawn receiver = props.partner;
			// 性类型可能导致怀孕。
			if (!(SexTypeCanImpregnate(props.sexType, giver, receiver) ||
				AnalOk || OralOk))
				return;

			List<Hediff> pawnparts = giver.GetGenitalsList();
			List<Hediff> partnerparts = receiver.GetGenitalsList();
			var interaction = props.interaction;

			//ModLog.Message(" RaceImplantEggs()" + pawn.RaceImplantEggs());
			//“虫族”怀孕
			//异性恋，女性（partner）接受来自另一方/性行为发起者（pawn）的卵子植入

			if (CouldBeEgging(props, giver, receiver, pawnparts, partnerparts))
			{
				//TODO：为“束缚全部/中立/敌对小人”添加控件开关
				//发起者/强暴者将受害者放入茧中，也许应移至 aftersex？
				if (!props.isReceiver)
					if (CanCocoon(giver))
						if (giver.HostileTo(receiver) || receiver.IsPrisonerOfColony || receiver.health.hediffSet.HasHediff(xxx.submitting))
							if (!receiver.health.hediffSet.HasHediff(HediffDef.Named("RJW_Cocoon")))
							{
								receiver.health.AddHediff(HediffDef.Named("RJW_Cocoon"));
							}

				if (RJWSettings.DevMode) ModLog.Message(" 'egg' pregnancy checks");
				//查看互动中谁插入谁
				if (!props.isReceiver &&
					interaction.Extension.initiatorRequirement.genitalTags.Contains(GenitalTag.CanPenetrate))
				{
					if (RJWSettings.DevMode) ModLog.Message(" impregnate(egg) - by initiator");
				}
				else if (props.isReceiver && props.isRevese &&
					interaction.Extension.recipientRequirement.genitalTags.Contains(GenitalTag.CanPenetrate))
				{
					if (RJWSettings.DevMode) ModLog.Message(" impregnate(egg) - by receiver (reverse)");
				}
				else
				{
					if (RJWSettings.DevMode) ModLog.Message(" no valid interaction tags/family");
					return;
				}
				//植入/使卵子受精
				DoEgg(props);
				return;
			}

			//检查给予方是否正在插入接受方
			bool propsImpregnate = false;
			foreach (Tuple<ILewdablePart, ILewdablePart> T in props.resolved.Penetrations)
			{
				if (T.Item1.Owner == giver && T.Item2.Owner == receiver)
				{
					propsImpregnate = true;
				}
			}

			//此处 sexType 不应为 None，但由于 CanImpregnate() 将其作为忽略性类型的提示，我们仍应检查，以免出怪事。
			if (propsImpregnate && CanImpregnate(giver, receiver, props.sexType) && props.sexType != xxx.rjwSextype.None)
			{
				DoImpregnate(giver, receiver);
			}
		}

		private static bool CouldBeEgging(SexProps props, Pawn giver, Pawn reciever, List<Hediff> pawnparts, List<Hediff> partnerparts)
		{
			List<Hediff_InsectEgg> eggs = new();
			reciever.health.hediffSet.GetHediffs(ref eggs);
			//无法产卵或受精
			if ((Genital_Helper.has_ovipositorF(giver, pawnparts) ||
				Genital_Helper.has_ovipositorM(giver, pawnparts) ||
				(Genital_Helper.has_penis_fertile(giver, pawnparts) && (giver.RaceImplantEggs() || eggs.Any()))
				) == false)
			{
				return false;
			}

			if ((props.sexType == xxx.rjwSextype.Vaginal || props.sexType == xxx.rjwSextype.DoublePenetration) &&
				Genital_Helper.has_vagina(reciever, partnerparts))
			{
				return true;
			}

			if ((props.sexType == xxx.rjwSextype.Anal || props.sexType == xxx.rjwSextype.DoublePenetration) &&
				Genital_Helper.has_anus(reciever) &&
				RJWPregnancySettings.insect_anal_pregnancy_enabled)
			{
				return true;
			}

			if (props.sexType == xxx.rjwSextype.Oral &&
				RJWPregnancySettings.insect_oral_pregnancy_enabled)
			{
				return true;
			}

			return false;
		}

		private static bool CanCocoon(Pawn pawn)
		{
			return xxx.is_insect(pawn);
		}

		/// <summary>
		/// 在存在多个怀孕（例如卵子植入造成）时，获取最可能
		/// 最先完成的那个怀孕。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>最可能最先完成的怀孕。</returns>
		public static Hediff GetPregnancy(Pawn pawn)
		{
			var allPregnancies = GetPregnancies(pawn);
			if (allPregnancies.Count == 0) return null;

			allPregnancies.SortBy((preg) =>
			{
				(var ticksCompleted, var ticksToBirth) = GetProgressTicks(preg);
				return ticksToBirth - ticksCompleted;
			});
			return allPregnancies[0];
		}

		/// <summary>
		/// 获取某个怀孕的妊娠进度。
		/// </summary>
		/// <param name="hediff">怀孕状态。</param>
		/// <returns>当前进度，取值在 0 与 1 之间。</returns>
		public static float GetProgress(Hediff hediff) => hediff switch
		{
			Hediff_BasePregnancy rjwPreg => rjwPreg.GestationProgress,
			Hediff_Pregnant vanillaPreg => vanillaPreg.GestationProgress,
			_ => 0f
		};

		/// <summary>
		/// <para>获取已完成的妊娠 tick 数以及分娩所需的总数。这两个
		/// 值都是近似值，因为营养不良之类的情况会影响
		/// 妊娠速率。</para>
		/// </summary>
		/// <param name="hediff">怀孕状态。</param>
		/// <returns>tick 元组。</returns>
		public static (int ticksCompleted, int ticksToBirth) GetProgressTicks(Hediff hediff)
		{
			if (hediff is Hediff_BasePregnancy rjwPreg)
			{
				// RJW 的怀孕在内部维护这些数值，因此它们
				// 无需被推断出来。
				var ticksToBirth = rjwPreg.p_end_tick - rjwPreg.p_start_tick;
				return (rjwPreg.ageTicks, Convert.ToInt32(ticksToBirth));
			}
			else
			{
				var progress = GetProgress(hediff);
				var raceProps = GetGestatingRace(hediff);
				var ticksToBirth = (int)(raceProps.gestationPeriodDays * GenDate.TicksPerDay);
				var ticksCompleted = (int)(progress * ticksToBirth);
				return (ticksCompleted, ticksToBirth);
			}
		}

		/// <summary>
		/// 检查传入的 xxx.rjwSextype 是否可能导致怀孕。
		/// 
		/// 当 CanImpregnate 被用作通用的“A 能否让小人 B 受孕”检查时，请使用通配值 xxx.rjwSextype.None
		/// </summary>
		/// <param name="sexType"></param>
		/// <param name="potentialFather">可能使对方受孕的小人。</param>
		/// <param name="potentialMother">可能被受孕的小人。</param>
		/// <returns>Bool</returns>
		public static bool SexTypeCanImpregnate(xxx.rjwSextype sexType, Pawn potentialFather, Pawn potentialMother)
		{
			if (sexType == xxx.rjwSextype.Vaginal || sexType == xxx.rjwSextype.DoublePenetration) return true;
			return false;
		}

		/// <summary>
		/// 检查该小人的种族是否能让对方受孕
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns>Bool</returns>
		public static bool RaceCanImpregnate(Pawn pawn)
		{
			if (pawn.IsUnsexyRobot())
			{
				if (RJWSettings.DevMode) ModLog.Message(" unsexy robot can't impregnate");
				return false;
			}
			if (!pawn.RaceHasPregnancy())
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(pawn) + " filtered race that cant be pregnant");
				return false;
			}
			return true;
		}

		/// <summary>
		/// 检查该小人的种族是否能怀孕
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns>Bool</returns>
		public static bool RaceCanGetPregnant(Pawn pawn)
		{
			if (pawn.IsUnsexyRobot())
			{
				if (RJWSettings.DevMode) ModLog.Message(" unsexy robot cant be pregnant");
				return false;
			}

			if (!pawn.RaceHasPregnancy())
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(pawn) + " filtered race that cant be pregnant");
				return false;
			}

			return true;
		}

		/// <summary>
		/// 检查该小人是否具备使他人受孕的部位
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="sexType">本次检查涉及的性类型。</param>
		/// <returns>Bool</returns>
		public static bool HasPartsToImpregnate(Pawn pawn, xxx.rjwSextype sexType)
		{
			var pawnparts = pawn.GetGenitalsList();

			if(!Genital_Helper.has_penis_fertile(pawn, pawnparts))
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(pawn) + " missing penis to impregnate");
				return false;
			}

			return true;
		}

		/// <summary>
		/// 检查该小人是否具备怀孕所需的部位
		/// 对于未定义或无法确定性类型的检查，请使用 xxx.rjwSextype.None
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="sexType">要检查的性类型；未定义时请使用 xxx.rjwSextype.None。</param>
		/// <returns>Bool</returns>
		public static bool HasPartsToGetPregnant(Pawn pawn, xxx.rjwSextype sexType)
		{
			var pawnparts = pawn.GetGenitalsList();

			if (!(Genital_Helper.has_vagina_fertile(pawn, pawnparts) && (sexType == xxx.rjwSextype.Vaginal || sexType == xxx.rjwSextype.DoublePenetration || sexType == xxx.rjwSextype.None)))
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(pawn) + " missing vagina to get pregnant");
				return false;
			}

			return true;
		}

		public static bool isFertile(Pawn pawn)
		{
			if (pawn.health.capacities.GetLevel(xxx.reproduction) <= 0)
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(pawn) + " infertile");
				return false;
			}
			return true;
		}

		public static bool HumanlikePregnancyPossible(Pawn pawn, Pawn partner)
		{
			if (RJWPregnancySettings.humanlike_pregnancy_enabled && RJWPregnancySettings.humanlike_impregnation_chance != 0)
			{
				return true;
			}
			return false;
		}

		public static bool RaceCompatibilityCheck(Pawn pawn, Pawn partner)
		{
			return true;
		}

		/// <summary>
		/// <para>根据双方是类人还是动物，检查两个小人之间是否应允许怀孕</para>
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="partner"></param>
		/// <returns>Bool</returns>
		public static bool BeastialPregnancyPossible(Pawn pawn, Pawn partner)
		{
			if (!((xxx.is_human(pawn) && xxx.is_animal(partner)) || (xxx.is_animal(pawn) && xxx.is_human(partner))))
			{
				return false;
			}
			if (RJWPregnancySettings.bestial_pregnancy == RJWPregnancySettings.BestialPregnancy.Unlimited)
			{
				return true;
			}
			if (RJWPregnancySettings.bestial_pregnancy == RJWPregnancySettings.BestialPregnancy.Limited)
			{
				return BeastialPregnancyCheck(pawn, partner);
			}

			return false;
		}

		/// <summary>
		/// 供其他模组通过 Harmony 后缀打补丁的入口，用于检查它们自己允许兽交怀孕的方式。
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="partner"></param>
		/// <returns>Bool</returns>
		public static bool BeastialPregnancyCheck(Pawn pawn, Pawn partner)
		{
			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("FertilityEnhancer")) || partner.health.hediffSet.HasHediff(HediffDef.Named("FertilityEnhancer")))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// <para>获取提供某次怀孕妊娠速率的种族属性。</para>
		/// <para>它未必准确，取决于该怀孕最终是
		/// 如何实现的。</para>
		/// </summary>
		/// <param name="hediff">怀孕状态。</param>
		/// <returns>假定正在妊娠的种族的属性。</returns>
		public static RaceProperties GetGestatingRace(Hediff hediff) => hediff switch
		{
			// 虫卵会使用植入者（若已设置）。否则，该卵大概
			// 仍在基因学上的母亲体内，因此我们可以改用载体。
			Hediff_InsectEgg eggPreg when eggPreg.implanter is { } implanter =>
				implanter.RaceProps,
			// 某些 RJW 怀孕会存储正在发育的婴儿；其中的第一个小人
			// 该列表即视为妊娠来源。
			Hediff_BasePregnancy rjwPreg when rjwPreg.babies is { } babies && babies.Count > 0 =>
				babies[0].RaceProps,
			// 其他情况则假定怀孕的小人为来源。
			_ => hediff.pawn.RaceProps
		};

		/// <summary>
		/// 计算小人可容纳的最大卵体积。
		/// </summary>
		public static float GetMaxEggsSize(Pawn pawn)
		{
			float maxEggSize = pawn.BodySize / 5f;

			if (Genital_Helper.has_ovipositorF(pawn))
			{			
				return maxEggSize * 2f * RJWPregnancySettings.egg_pregnancy_ovipositor_capacity_factor;
			}

			if (!RJWPregnancySettings.egg_pregnancy_unlimited_capacity)
			{		
				return maxEggSize;
			}
				return 9999f;
		}

		/// <summary>
		/// 根据植入者的体型计算卵的大小。
		/// 这与 Hediff_InsectEgg.ChangeEgg() 在植入时使用的公式相同。
		/// </summary>
		/// <param name="def">卵的状态定义。</param>
		/// <param name="implanter">植入卵的小人（决定卵的大小）。</param>
		/// <returns>计算得出的卵大小。</returns>
		public static float CalculateEggSize(HediffDef_InsectEgg def, Pawn implanter)
		{
			float eggSize = implanter.RaceProps.baseBodySize / 5f * def.eggsize * RJWPregnancySettings.egg_pregnancy_eggs_size;
			return System.Math.Max(0.01f, eggSize);
		}

		/// <summary>
		/// 获取小人身上所有虫卵当前占用的卵大小总和。
		/// 用于判断还剩多少容量。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>小人身上所有 egg.eggssize 值之和。</returns>
		public static float GetCurrentEggSize(Pawn pawn)
		{
			return pawn.health.hediffSet.hediffs
				.OfType<Hediff_InsectEgg>()
				.Sum(egg => egg.eggssize);
		}

		/// <summary>
		/// 以列表形式获取小人的所有怀孕。这可能同时包含 RJW
		/// 和原版的怀孕。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>怀孕状态的列表。</returns>
		public static List<Hediff> GetPregnancies(Pawn pawn) =>
			pawn.health.hediffSet.hediffs
				.Where(x => x is Hediff_BasePregnancy or Hediff_Pregnant)
				.ToList();

		///<summary>在满足上述条件时可以怀孕，则执行授孕。</summary>

		[SyncMethod]
		public static void DoEgg(SexProps props)
		{
			if (RJWPregnancySettings.insect_pregnancy_enabled)
			{
				if (RJWSettings.DevMode) ModLog.Message(" insect pregnancy");

				//雌性“虫”植入卵
				//扶她“虫”50% 植入卵，50% 为卵受精
				if ((Genital_Helper.has_ovipositorF(props.pawn) && !Genital_Helper.has_penis_fertile(props.pawn)) ||
					(Rand.Value > 0.5f && Genital_Helper.has_ovipositorF(props.pawn)))
					//以后会有阴茎产卵？
					//(Rand.Value > 0.5f && (Genital_Helper.has_ovipositorF(pawn) || Genital_Helper.has_penis_fertile(pawn) && pawn.RaceImplantEggs())))
				{
					float maxeggssize = GetMaxEggsSize(props.partner);
					float eggedsize = 0;

					// 获取植入者的卵
					List<Hediff_InsectEgg> eggs = new();
					props.partner.health.hediffSet.GetHediffs(ref eggs);

					// 检查是否为已受精的卵/非通用卵
					foreach (Hediff_InsectEgg egg in eggs)
					{
							eggedsize += egg.eggssize;
					}
					if (RJWSettings.DevMode) ModLog.Message(" determine " + xxx.get_pawnname(props.partner) + " size of eggs inside: " + eggedsize + ", max: " + maxeggssize);

					BodyPartRecord targetBodyPart = Genital_Helper.get_genitalsBPR(props.pawn); //获取我们的生殖器
					Predicate<Hediff> filterByGenitalia = hediff => hediff.Part == targetBodyPart; //构造筛选条件
					props.pawn.health.hediffSet.GetHediffs(ref eggs, filterByGenitalia); //只从我们的生殖器取卵（不取肛门或胃里的）
					BodyPartRecord partnerGenitals = null;

					if (props.sexType == xxx.rjwSextype.Anal)
						partnerGenitals = Genital_Helper.get_anusBPR(props.partner);
					else if (props.sexType == xxx.rjwSextype.Oral)
						partnerGenitals = Genital_Helper.get_stomachBPR(props.partner);
					else if (props.sexType == xxx.rjwSextype.DoublePenetration && Rand.Value > 0.5f && RJWPregnancySettings.insect_anal_pregnancy_enabled)
						partnerGenitals = Genital_Helper.get_anusBPR(props.partner);
					else
						partnerGenitals = Genital_Helper.get_genitalsBPR(props.partner);

					while (eggs.Any() && eggedsize < maxeggssize)
					{
						if (props.sexType == xxx.rjwSextype.Vaginal)
						{
							// 移除旧的怀孕
							var p = GetPregnancies(props.partner);
							if (!p.NullOrEmpty())
							{
								var i = p.Count;
								while (i > 0)
								{
									i -= 1;
									var h = GetPregnancies(props.partner);
									if (h[i] is Hediff_MechanoidPregnancy)
									{
										if (RJWSettings.DevMode) ModLog.Message(" egging - pregnant by mechanoid, skip");
									}
									else if (h[i] is Hediff_BasePregnancy)
									{
										if (RJWSettings.DevMode) ModLog.Message(" egging - removing rjw normal pregnancy");
										(h[i] as Hediff_BasePregnancy).Kill();
									}
									else
									{
										if (RJWSettings.DevMode) ModLog.Message(" egging - removing vanilla or other mod pregnancy");
										props.partner.health.RemoveHediff(h[i]);
									}
								}
							}
						}

						var egg = eggs.First();
						eggs.Remove(egg);

						props.pawn.health.RemoveHediff(egg);
						props.partner.health.AddHediff(egg, partnerGenitals);

						egg.InitImplanter(props.pawn);

						eggedsize += egg.eggssize;
					}
				}
				//男性或扶她为卵受精
				else if (!props.pawn.health.hediffSet.HasHediff(xxx.sterilized))
				{
					if (Genital_Helper.has_penis_fertile(props.pawn))
						if ((Genital_Helper.has_ovipositorF(props.pawn) || Genital_Helper.has_ovipositorM(props.pawn)) || (props.pawn.health.capacities.GetLevel(xxx.reproduction) > 0))
						{
							List<Hediff_InsectEgg> eggs = new();
							props.partner.health.hediffSet.GetHediffs(ref eggs);
							if (!RJWPregnancySettings.egg_pregnancy_fertOrificeCheck_enabled)
							{
								foreach (var egg in eggs)
									egg.Fertilize(props.pawn);
							}
							else
							{
								//Gargulecode
								BodyPartRecord targetBodyPart;
								if (props.sexType == xxx.rjwSextype.Anal || props.sexType == xxx.rjwSextype.DoublePenetration)
									targetBodyPart = Genital_Helper.get_anusBPR(props.partner); //肛交？取肛门
								else if (props.sexType == xxx.rjwSextype.Oral)
									targetBodyPart = Genital_Helper.get_stomachBPR(props.partner); //口交？取胃
								else
									targetBodyPart = Genital_Helper.get_genitalsBPR(props.partner); //以上都不适用？取生殖器
								//Gargulecode 结束

								foreach (var egg in eggs.Where(x=> x.Part == targetBodyPart))
									egg.Fertilize(props.pawn);

								if (props.sexType == xxx.rjwSextype.DoublePenetration)
								{
									targetBodyPart = Genital_Helper.get_genitalsBPR(props.partner);

									foreach (var egg in eggs.Where(x => x.Part == targetBodyPart))
										egg.Fertilize(props.pawn);
								}
							}
						}
				}
				return;
			}
		}

		[SyncMethod]
		public static void DoImpregnate(Pawn pawn, Pawn partner)
		{
			if (RJWSettings.DevMode) ModLog.Message(" Doimpregnate " + xxx.get_pawnname(pawn) + " is a father, " + xxx.get_pawnname(partner) + " is a mother");

			if (AndroidsCompatibility.IsAndroid(pawn) && !AndroidsCompatibility.AndroidPenisFertility(pawn))
			{
				if (RJWSettings.DevMode) ModLog.Message(" Father is android with no arcotech penis, abort");
				return;
			}
			if (AndroidsCompatibility.IsAndroid(partner) && !AndroidsCompatibility.AndroidVaginaFertility(partner))
			{
				if (RJWSettings.DevMode) ModLog.Message(" Mother is android with no arcotech vagina, abort");
				return;
			}

			// 生育力检查
			float basePregnancyChance = RJWPregnancySettings.humanlike_impregnation_chance / 100f;
			if (xxx.is_animal(partner))
				basePregnancyChance = RJWPregnancySettings.animal_impregnation_chance / 100f;

			// 跨物种修正系数
			if (pawn.def.defName != partner.def.defName)
			{
				if (RJWPregnancySettings.complex_interspecies)
					basePregnancyChance *= SexUtility.BodySimilarity(pawn, partner);
				else
					basePregnancyChance *= RJWPregnancySettings.interspecies_impregnation_modifier;
			}
			else
			{
				//卵的生育力检查
				CompEggLayer compEggLayer = partner.TryGetComp<CompEggLayer>();
				if (compEggLayer != null)
					basePregnancyChance = 1.0f;
			}

			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			float fertility = Math.Min(pawn.health.capacities.GetLevel(xxx.reproduction), partner.health.capacities.GetLevel(xxx.reproduction));

			if (partner.health.hediffSet.HasHediff(RJW_IUD))
			{
				fertility /= 99f;
			}

			float pregnancyChance = basePregnancyChance * fertility;

			if (!Rand.Chance(pregnancyChance))
			{
				if (RJWSettings.DevMode) ModLog.Message(" Impregnation failed. Chance: " + pregnancyChance.ToStringPercent());
				return;
			}
			if (RJWSettings.DevMode) ModLog.Message(" Impregnation succeeded. Chance: " + pregnancyChance.ToStringPercent());

			AddPregnancyHediff(partner, pawn);
		}


		///<summary>用于检查普通怀孕，不适用于卵植入之类的情况。</summary>
		public static bool CanImpregnate(Pawn potentialFather, Pawn potentialMother, xxx.rjwSextype sexType = xxx.rjwSextype.None)
		{
			if (potentialFather == null || potentialMother == null) return false;

			if (RJWSettings.DevMode) ModLog.Message("Rimjobworld::CanImpregnate checks (" + sexType + "):: " + xxx.get_pawnname(potentialFather) + " + " + xxx.get_pawnname(potentialMother) + ":");

			if (sexType != xxx.rjwSextype.None && !SexTypeCanImpregnate(sexType, potentialFather, potentialMother))
			{
				if (RJWSettings.DevMode) ModLog.Message(" sextype cannot result in pregnancy");
				return false;
			}

			if (AndroidsCompatibility.IsAndroid(potentialFather) && AndroidsCompatibility.IsAndroid(potentialMother))
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(potentialMother) + " androids cant breed/reproduce androids");
				return false;
			}

			if (!RaceCanImpregnate(potentialFather) || !RaceCanGetPregnant(potentialMother)) { return false; }

			if (!RaceCompatibilityCheck(potentialFather, potentialMother)) { return false; }

			if (!HasPartsToImpregnate(potentialFather, sexType) || !HasPartsToGetPregnant(potentialMother, sexType)) { return false; }

			if (!isFertile(potentialFather) || !isFertile(potentialMother)) { return false; }

			if (potentialMother.IsPregnant())
			{
				if (RJWSettings.DevMode) ModLog.Message(" already pregnant.");
				return false;
			}

			List<Hediff_InsectEgg> eggs = new();
			potentialMother.health.hediffSet.GetHediffs(ref eggs);
			if ((from x in eggs where x.def == DefDatabase<HediffDef_InsectEgg>.GetNamed(x.def.defName) select x).Any())
			{
				if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(potentialMother) + " cant get pregnant while eggs inside");
				return false;
			}

			

			if (!HumanlikePregnancyPossible(potentialFather, potentialMother) && xxx.is_human(potentialFather) && xxx.is_human(potentialMother))
			{
				if (RJWSettings.DevMode) ModLog.Message(" human pregnancy chance set to 0% or pregnancy disabled.");
				return false;
			}
			else if ((xxx.is_human(potentialFather) && xxx.is_animal(potentialMother)) || (xxx.is_animal(potentialFather) && xxx.is_human(potentialMother)))
			{
				if (!BeastialPregnancyPossible(potentialFather, potentialMother))
				{
					if (RJWSettings.DevMode)
					{
						if (RJWPregnancySettings.bestial_pregnancy == RJWPregnancySettings.BestialPregnancy.None) ModLog.Message(" bestiality pregnancy chance set to 0% or pregnancy disabled.");
						else if (RJWPregnancySettings.bestial_pregnancy == RJWPregnancySettings.BestialPregnancy.Limited) ModLog.Message(" bestiality pregnancy not possible between these pawns.");
					}
					return false;
				}
			}
			else if (xxx.is_animal(potentialMother) && xxx.is_animal(potentialFather) && (RJWPregnancySettings.animal_impregnation_chance == 0 || !RJWPregnancySettings.animal_pregnancy_enabled))
			{
				if (RJWSettings.DevMode) ModLog.Message(" animal-animal pregnancy chance set to 0% or pregnancy disabled.");
				return false;
			}
			else if (potentialFather.def.defName != potentialMother.def.defName && (RJWPregnancySettings.interspecies_impregnation_modifier <= 0.0f && !RJWPregnancySettings.complex_interspecies))
			{
				if (RJWSettings.DevMode) ModLog.Message(" interspecies pregnancy disabled.");
				return false;
			}

			if (!(potentialMother.RaceProps.gestationPeriodDays > 0))
			{
				CompEggLayer compEggLayer = potentialMother.TryGetComp<CompEggLayer>();
				if (compEggLayer == null)
				{
					if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(potentialMother) + " mother.RaceProps.gestationPeriodDays is " + potentialMother.RaceProps.gestationPeriodDays + " cant impregnate");
					return false;
				}
			}

			if (RJWSettings.DevMode) ModLog.Message("Rimjobworld::CanImpregnate checks (" + sexType + "):: " + xxx.get_pawnname(potentialFather) + " + " + xxx.get_pawnname(potentialMother) + ": TRUE");
			return true;
		}

		//为人×兽交怀孕植入胎儿
		public static void AddPregnancyHediff(Pawn mother, Pawn father)
		{
			//人×人
			if (HumanlikePregnancyPossible(father, mother) && xxx.is_human(father) && xxx.is_human(mother))
			{
				CompEggLayer compEggLayer = mother.TryGetComp<CompEggLayer>();
				if (compEggLayer != null)
				{
					// 为人形生物的卵受精？！
					if (!compEggLayer.FullyFertilized)
					{
						compEggLayer.Fertilize(father);
						//if (!mother.kindDef.defName.Contains("Chicken"))
						//	if (compEggLayer.Props.eggFertilizedDef.defName.Contains("RJW"))
					}
				}
				else
				{
					if (RJWPregnancySettings.UseVanillaPregnancy)
					{
						// 原版 1.4 的人类怀孕状态
						ModLog.Message("preg hediffdefof PregnantHuman " + HediffDefOf.PregnantHuman);
						StartVanillaPregnancy(mother, father);
					}
					else
					{
						// 使用 RJW 状态
						Hediff_BasePregnancy.Create<Hediff_HumanlikePregnancy>(mother, father);
					}
				}
			}
			//人×动物
			//也许要为人男与雌性动物分别做个选项？？？
			else if (BeastialPregnancyPossible(mother, father))
			{
				CompEggLayer compEggLayer = mother.TryGetComp<CompEggLayer>();
				if (compEggLayer != null)
				{
					if (!compEggLayer.FullyFertilized)
						compEggLayer.Fertilize(father);
				}
				else
				{
					DnaGivingParent dnaGivingParent = Hediff_BasePregnancy.SelectDnaGivingParent(mother, father);
					if (RJWPregnancySettings.UseVanillaPregnancy && xxx.is_human(mother) && dnaGivingParent == DnaGivingParent.Mother)
					{
						// 原版 1.4 的人类怀孕状态
						ModLog.Message("preg hediffdefof PregnantHuman " + HediffDefOf.PregnantHuman);
						StartVanillaPregnancy(mother, father);
					}
					else
					{
						Hediff_BasePregnancy.Create<Hediff_BestialPregnancy>(mother, father, dnaGivingParent);
					}
				}
			}
			//动物×动物
			else if (xxx.is_animal(mother) && xxx.is_animal(father))
			{
				CompEggLayer compEggLayer = mother.TryGetComp<CompEggLayer>();
				if (compEggLayer != null)
				{
					// 为同物种的卵受精
					if (!compEggLayer.FullyFertilized)
						if (mother.kindDef == father.kindDef)
							compEggLayer.Fertilize(father);
				}
				else if (RJWPregnancySettings.animal_pregnancy_enabled)
				{
					Hediff_BasePregnancy.Create<Hediff_BestialPregnancy>(mother, father);
				}
			}
		}

		public static void StartVanillaPregnancy(Pawn mother, Pawn father, Pawn geneticMother = null, HediffDef def = null)
		{
			def ??= HediffDefOf.PregnantHuman;
			Hediff_Pregnant pregnancy = (Hediff_Pregnant) HediffMaker.MakeHediff(def, mother);
			pregnancy.SetParents(geneticMother, father, PregnancyUtility.GetInheritedGeneSet(father, mother));
			mother.health.AddHediff(pregnancy);
		}

		//植入虫卵/机械芯片/其他怀孕模组的状态？
		public static bool PlantSomething(HediffDef def, Pawn target, bool isToAnal = false, int amount = 1)
		{
			if (def == null)
				return false;
			if (!isToAnal && !Genital_Helper.has_vagina(target))
				return false;
			if (isToAnal && !Genital_Helper.has_anus(target))
				return false;

			BodyPartRecord Part = (isToAnal) ? Genital_Helper.get_anusBPR(target) : Genital_Helper.get_genitalsBPR(target);
			if (Part != null || Part.parts.Count != 0)
			{
				//清除普通怀孕
				if (!isToAnal)
				{
					if (RJWSettings.DevMode) ModLog.Message(" removing other pregnancies");
					var p = GetPregnancies(target);
					if (!p.NullOrEmpty())
					{
						foreach (var x in p)
						{
							if (x is Hediff_BasePregnancy)
							{
								var preg = x as Hediff_BasePregnancy;
								preg.Kill();
							}
							else
							{
								target.health.RemoveHediff(x);
							}
						}
					}
				}

				for (int i = 0; i < amount; i++)
				{

					if (RJWSettings.DevMode) ModLog.Message(" planting something weird");
					target.health.AddHediff(def, Part);
				}

				return true;
			}
			return false;
		}

		/// <summary>
		/// 移除未经 RJW 检查就添加的 CnP 怀孕
		/// </summary>
		public static void cleanup_CnP(Pawn pawn)
		{
			//它们的概率检查不达标，也不尊重我们的设置，但我没法直接阻止它们对 doloving 的覆盖。
			//大概需要 Harmony 补丁
			//所以如果该状态被创建出来，我就把它移除；之后在我们的处理逻辑里需要时再重新创建

			if (RJWSettings.DevMode) ModLog.Message(" cleanup_CnP after love check");

			var h = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("HumanPregnancy"));
			if (h != null && h.ageTicks < 100)
			{
				pawn.health.RemoveHediff(h);
				if (RJWSettings.DevMode) ModLog.Message(" removed hediff from " + xxx.get_pawnname(pawn));
			}
		}

		/// <summary>
		/// 移除原版怀孕
		/// </summary>
		public static void cleanup_vanilla(Pawn pawn)
		{
			if (RJWSettings.DevMode) ModLog.Message(" cleanup_vanilla after love check");

			var h = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant);
			if (h != null && h.ageTicks < 100)
			{
				pawn.health.RemoveHediff(h);
				if (RJWSettings.DevMode) ModLog.Message(" removed hediff from " + xxx.get_pawnname(pawn));
			}
		}
	}
}
