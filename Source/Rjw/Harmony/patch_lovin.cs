#nullable disable

using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace rjw
{
	/// <summary>
	/// 补丁：
	/// 核心的做爱与交配
	/// Rational Romance 的 LovinCasual
	/// </summary>

	// 为 JobDriver_Lovin 添加失败条件，使小人在身体条件不允许或没有生殖器时无法做爱
	[HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
	internal static class PATCH_JobDriver_Lovin_MakeNewToils
	{
		[HarmonyPrefix, HarmonyPriority(Priority.First)]
		private static bool lovin_patch_n_override(JobDriver_Lovin __instance)
		{
			Pawn pawn = __instance.pawn;
			Pawn partner = __instance.Partner;
			Building_Bed Bed = __instance.Bed;

			__instance.FailOn(() => (!(xxx.can_fuck(pawn) || xxx.can_be_fucked(pawn))));
			__instance.FailOn(() => (!(xxx.can_fuck(partner) || xxx.can_be_fucked(partner))));

			if (RJWSettings.override_lovin)
			{
				pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				Job job = JobMaker.MakeJob(xxx.casual_sex, partner, Bed);
				pawn.jobs.jobQueue.EnqueueFirst(job);
			}

			return true;
		}
	}

#nullable enable

	[HarmonyPatch(typeof(JobGiver_Mate), "TryGiveJob")]
	internal static class PATCH_JobGiver_Mate_TryGiveJob
	{
		public static void Postfix(ref Job? __result, Pawn pawn)
		{
			if (__result == null && pawn.gender != Gender.Male)
			{
				//补丁：让扶他动物也能发起交配（原版只有雄性会）
				FutaAnimalPartnerSearch(ref __result, pawn);
			}
			else if (__result != null)
			{
				VanillaMatingOverride(ref __result, pawn);
			}
		}

		private static void VanillaMatingOverride(ref Job? __result, Pawn pawn)
		{
			if (__result?.def != JobDefOf.Mate)
			{
				return;
			}

			if (__result.targetA.Thing is not Pawn partner)
			{
				__result = null;
				return;
			}

			if (!RJWSettings.override_matin)
			{
				return;
			}

			if (!pawn.IsAnimal() || !partner.IsAnimal())
			{
				//ModLog.Message($"Overriding vanilla mating because one of ({pawn},{partner}) is not an animal");
				// 原版为什么会试图发起人兽交？
				__result = JobMaker.MakeJob(xxx.animalBreed, partner);
			}
			else if (CanOverride(pawn))
			{
				__result = JobMaker.MakeJob(xxx.animalMate, partner);
			}
		}

		private static void FutaAnimalPartnerSearch(ref Job? __result, Pawn pawn)
		{
			//ModLog.Message("patches_lovin::JobGiver_Mate fail, try rjw for " + xxx.get_pawnname(pawn));
			var parts = pawn.GetGenitalsList();
			if (!Genital_Helper.has_male_bits(pawn, parts) || !pawn.ageTracker.CurLifeStage.reproductive)
			{
				//ModLog.Message("patches_lovin::JobGiver_Mate " + xxx.get_pawnname(pawn) + ", has no penis " + (Genital_Helper.has_penis(pawn) || Genital_Helper.has_penis_infertile(pawn)));
				//ModLog.Message("patches_lovin::JobGiver_Mate " + xxx.get_pawnname(pawn) + ", not reproductive " + !pawn.ageTracker.CurLifeStage.reproductive);
				return;
			}
			Predicate<Thing> validator = delegate (Thing t)
			{
				if (t is not Pawn pawn3)
				{
					return false;
				}
				var valid = !pawn3.Downed
							&& pawn3 != pawn
							&& pawn3.CanCasuallyInteractNow()
							&& !pawn3.IsForbidden(pawn)
							&& !pawn3.HostileTo(pawn)
							&& PawnUtility.FertileMateTarget(pawn, pawn3)
							&& (RJWSettings.WildMode || // 放开限制，与所有个体交配
								(!RJWSettings.matin_crossbreed && pawn.def == pawn3.def) || // 与同物种交配 —— 原版行为
								(RJWSettings.matin_crossbreed && pawn.RaceProps.animalType != AnimalType.None && pawn.RaceProps.animalType == pawn3.RaceProps.animalType)); // 与相同 animalType 交配
				//if (!valid && pawn3 != pawn)
				//{
					//ModLog.Message("patches_lovin::JobGiver_Mate " + xxx.get_pawnname(pawn3) + ", not valid");
					//ModLog.Message("patches_lovin::JobGiver_Mate Downed " + pawn3.Downed);
					//ModLog.Message("patches_lovin::JobGiver_Mate CanCasuallyInteractNow " + pawn3.CanCasuallyInteractNow());
					//ModLog.Message("patches_lovin::JobGiver_Mate IsForbidden " + pawn3.IsForbidden(pawn));
					//ModLog.Message("patches_lovin::JobGiver_Mate FertileMateTarget " + PawnUtility.FertileMateTarget(pawn, pawn3));
				//}
				return valid;
			};

			ThingRequest request = ThingRequest.ForGroup(ThingRequestGroup.Pawn);			// 与所有个体交配，由校验器过滤
			Pawn pawn2 = (Pawn)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, request, PathEndMode.Touch, TraverseParms.For(pawn), 30f, validator);
			if (pawn2 == null)
			{
				//ModLog.Message("patches_lovin::JobGiver_Mate " + xxx.get_pawnname(pawn) + ", no valid partner found");
				return;
			}
			if (RJWSettings.override_matin && CanOverride(pawn))
			{
				__result = JobMaker.MakeJob(xxx.animalMate, pawn2);
			}
			else
			{
				__result = JobMaker.MakeJob(JobDefOf.Mate, pawn2);
			}
			//ModLog.Message("patches_lovin::JobGiver_Mate " + xxx.get_pawnname(pawn) + ", female " + xxx.get_pawnname(pawn2) + ", job " + __result);
		}

		private static bool CanOverride(Pawn pawn)
		{
			if (RJWSettings.wild_mating_enabled)
			{
				//ModLog.Message($"Overriding vanilla mating because wild_mating_enabled");
				return true;
			}
			else if (RJWSettings.animal_on_animal_enabled && pawn.Faction == Faction.OfPlayer)
			{
				//ModLog.Message($"Overriding vanilla mating because {pawn} is a tamed animal");
				return true;
			}

			return false;
		}
	}

#nullable disable

	//检查雌性能否在动物交配中受孕（也会检查 RJW 怀孕）
	[HarmonyPatch(typeof(PawnUtility), "FertileMateTarget")]
	internal static class PATCH_PawnUtility_FertileMateTarget
	{
		[HarmonyPrefix]
		public static bool Prefix(Pawn male, Pawn female, ref bool __state)
		{
			__state = female.IsPregnant();
			//ModLog.Message("Prefix FertileMateTarget is vanilla/rjw pregnant: " + __state);
			return true;
		}

		[HarmonyPostfix]
		public static bool Postfix(bool __result, ref bool __state, Pawn male, Pawn female)
		{
			if (__result)
			{
				//ModLog.Message("Postfix FertileMateTarget is fertile and not vanilla pregnant: " + __result);
				if (__state)
				{
					//ModLog.Message("Postfix FertileMateTarget is fertile and rjw pregnant: " + __state);
					__result = false;
				}
			}

			var cantFuck = !(xxx.can_fuck(male) && xxx.can_be_fucked(female)) &&
						  !(xxx.can_fuck(female) && xxx.can_be_fucked(male));

			if (cantFuck && __result == true)
			{
				__result = false;
			}

			return __result;
		}
	}

	//RomanceDiversified 的 JobDriver_DoLovinCasual 应该已经处理了小人能否随意做爱，
	//所以我懒得在这里再做检查，除非这会引发什么 bug。
	//这里大概也需要上面那样的补丁，不过谁在乎呢

	// 在小人做完爱后调用 xxx.aftersex
	// 你可能会想：“把这段代码作为完成条件加到上面补丁的 JobDriver_Lovin 里，不是更省事吗？”我一开始
	// 确实这么试了，但没用，因为不管工作怎么结束（即是否被打断），完成条件总会被调用，
	// 而在完成条件内部无法得知工作是怎么结束的。我想确保在工作被以某种方式打断时，
	// 不施加性行为的效果。
	[HarmonyPatch(typeof(JobDriver), "Cleanup")]
	internal static class PATCH_JobDriver_Loving_Cleanup
	{
		//RomanceDiversified 的做爱
		//不是很好的办法，别的模组也可能有同名的工作驱动，不过无所谓
		private readonly static Type JobDriverDoLovinCasual = AccessTools.TypeByName("JobDriver_DoLovinCasual");

		//Nightmare Incarnation
		//不是很好的办法，别的模组也可能有同名的工作驱动，不过无所谓
		private readonly static Type JobDriverNIManaSqueeze = AccessTools.TypeByName("JobDriver_NIManaSqueeze");

		[HarmonyPrefix]
		private static bool on_cleanup_driver(JobDriver __instance, JobCondition condition)
		{
			if (__instance == null)
				return true;

			if (condition == JobCondition.Succeeded)
			{
				Pawn pawn = __instance.pawn;
				Pawn partner = null;

				//ModLog.Message("patches_lovin::on_cleanup_driver" + xxx.get_pawnname(pawn));

				var any_ins = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

				//[RF] Rational Romance [1.0] 的做爱
				if (xxx.RomanceDiversifiedIsActive && __instance.GetType() == JobDriverDoLovinCasual)
				{
					// 不确定 RR 到底会不会导致怀孕，不过无所谓
					partner = (Pawn)(__instance.GetType().GetProperty("Partner", any_ins).GetValue(__instance, null));
					ModLog.Message("patches_lovin::on_cleanup_driver RomanceDiversified/RationalRomance:" + xxx.get_pawnname(pawn) + "+" + xxx.get_pawnname(partner));
				}
				//Nightmare Incarnation
				else if (xxx.NightmareIncarnationIsActive && __instance.GetType() == JobDriverNIManaSqueeze)
				{
					partner = (Pawn)(__instance.GetType().GetProperty("Victim", any_ins).GetValue(__instance, null));
					ModLog.Message("patches_lovin::on_cleanup_driver Nightmare Incarnation:" + xxx.get_pawnname(pawn) + "+" + xxx.get_pawnname(partner));
				}
				//原版做爱
				else if (__instance is JobDriver_Lovin jobDriver_Lovin)
				{
					partner = jobDriver_Lovin.Partner;
					ModLog.Message("patches_lovin:: JobDriverLovin pregnancy:" + xxx.get_pawnname(pawn) + "+" + xxx.get_pawnname(partner));
				}
				//原版交配
				else if (__instance is JobDriver_Mate  jobDriver_Mate)
				{
					partner = jobDriver_Mate.Female;
					ModLog.Message("patches_lovin:: JobDriverMate pregnancy:" + xxx.get_pawnname(pawn) + "+" + xxx.get_pawnname(partner));
				}
				else
					return true;

				// TODO：在这里执行 TryUseCondom 有点奇怪……它应该更早发生。
				//var usedCondom = CondomUtility.TryUseCondom(pawn) || CondomUtility.TryUseCondom(partner);

				//原版对非人形生物大概会出问题……但只有人形生物才做爱，对吧？对吧？
				//如果启用了 RJW 怀孕，则移除原版处理，适用于：
				//人-人
				//动物-动物
				//人兽交
				//任一方是昆虫或机械时总是移除
				if (PregnancyHelper.HumanlikePregnancyPossible(pawn, partner) && xxx.is_human(pawn) && xxx.is_human(partner)
					|| RJWPregnancySettings.animal_pregnancy_enabled && xxx.is_animal(pawn) && xxx.is_animal(partner)
					|| PregnancyHelper.BeastialPregnancyPossible(pawn, partner)
					|| xxx.is_insect(pawn) || xxx.is_insect(partner) || xxx.is_mechanoid(pawn) || xxx.is_mechanoid(partner)
					)
				{
					ModLog.Message("patches_lovin::on_cleanup_driver vanilla pregnancy:" + xxx.get_pawnname(pawn) + "+" + xxx.get_pawnname(partner));
					PregnancyHelper.cleanup_vanilla(pawn);
					PregnancyHelper.cleanup_vanilla(partner);
				}

				SexProps nonRJWsexI = SexUtility.SelectSextype(pawn, partner, false, false);
				
				nonRJWsexI.isRevese = nonRJWsexI.interaction.HasInteractionTag(SexInteractionTag.Reverse);
				nonRJWsexI.isCoreLovin = true;

				SexUtility.ProcessSex(nonRJWsexI); //TODO：以后再说
				SexUtility.SatisfyPersonal(nonRJWsexI);
				SexUtility.SatisfyPersonal(nonRJWsexI.GetForPartner());
			}
			return true;
		}
	}
}
