#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

// 与怀孕无关的 Biotech 补丁
namespace rjw
{
    [HarmonyPatch]
    class LifeStageWorker_HumanlikeX_Notify_LifeStageStarted
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            const string lifeStageStarted = nameof(LifeStageWorker.Notify_LifeStageStarted);
            yield return AccessTools.Method(typeof(LifeStageWorker_HumanlikeChild), lifeStageStarted);
            yield return AccessTools.Method(typeof(LifeStageWorker_HumanlikeAdult), lifeStageStarted);
        }

        // 修复儿童进入新生命阶段时尝试生成仅 Biotech 可用的效果器所导致的错误
        // 以及儿童满三岁时尝试发送仅 Biotech 可用的信件所导致的错误
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FixLifeStageStartError(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            PropertyInfo thingSpawned = AccessTools.DeclaredProperty(typeof(Thing), nameof(Thing.Spawned));
            MethodInfo shouldSendNotificationsAbout = AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.ShouldSendNotificationAbout));
            bool foundAny = false;

            foreach (var instruction in instructions)
            {
                yield return instruction;

                // if (pawn.Spawned) SpawnBiotechOnlyEffector()
                // => if (pawn.Spawned && ModsConfig.IsBiotechActive) SpawnBiotechOnlyEffector()
                if (instruction.Calls(thingSpawned.GetMethod) || instruction.Calls(shouldSendNotificationsAbout))
                {
                    yield return CodeInstruction.Call(typeof(ModsConfig), "get_BiotechActive");
                    yield return new CodeInstruction(OpCodes.And);
                    foundAny = true;
                }
            }

            if (!foundAny)
            {
                ModLog.Error("Failed to patch " + original.Name);
            }
        }
    }

    [HarmonyPatch(typeof(WidgetsWork), "get_WorkBoxBGTex_AgeDisabled")]
    class WidgetsWork_WorkBoxBGTex_AgeDisabled
    {
        [HarmonyPrefix]
        static bool DontLoadMissingTexture(ref Texture2D __result)
        {
            if (!ModsConfig.BiotechActive)
            {
                __result = WidgetsWork.WorkBoxBGTex_Awful;
                return false;
            }

            return true;
        }
    }

    // 如果未启用 Biotech，TeachOpportunity 会因 ConceptDef 参数为 null 而抛出空引用异常
    // 通过添加空值检查，消除会中断 AgeTick 结果的错误。
    [HarmonyPatch(typeof(LessonAutoActivator), nameof(LessonAutoActivator.TeachOpportunity), new [] {typeof(ConceptDef), typeof(Thing), typeof(OpportunityType)})]
    static class Patch_LessonAutoActivator_TeachOpportunity
    {
        public static bool Prefix(ConceptDef conc)
        {
            // 如果 concept 非 null，则调用底层方法
            return conc != null;
        }
    }

    // 如果未启用 Biotech，则让目标方法中的 Baby() 方法返回 false。
    [HarmonyPatch]
    static class Patch_DevelopmentalStageExtensions_Baby_Callers
    {

        static IEnumerable<MethodBase> TargetMethods()
        {
            // 让婴儿在食物适格上遵循成人规则，而不是必须吃奶/婴儿食品
            yield return AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.WillEat), new [] {typeof(Pawn), typeof(ThingDef), typeof(Pawn), typeof(bool), typeof(bool)});
            // 让婴儿可以像病人一样在床上被喂食，而不是只能哺乳
            yield return AccessTools.Method(typeof(WorkGiver_FeedPatient), nameof(WorkGiver_FeedPatient.HasJobOnThing), new [] {typeof(Pawn), typeof(Thing), typeof(bool)});
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BabyMethodShouldReturnFalse(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            bool foundAny = false;
            MethodInfo developmentalStageIsBabyMethod = AccessTools.Method(typeof(DevelopmentalStageExtensions), nameof(DevelopmentalStageExtensions.Baby));
            List<CodeInstruction> codeInstructions = instructions.ToList();
            foreach (CodeInstruction instruction in codeInstructions)
            {
                yield return instruction;
                if (instruction.Calls(developmentalStageIsBabyMethod))
                {
                    foundAny = true;
                    if (!ModsConfig.BiotechActive)
                    {
                        // 调用 Baby() 方法后，将结果与 0 做按位与，使其等同于 DevelopmentalStageExtensions::Baby() 返回 false。
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.And);
                    }
                }
            }
            if (!foundAny)
            {
                ModLog.Error("Failed to patch " + original.Name);
            }
        }
    }

	// 在汇总可使卵子受精的小人列表时，检查有生育力的阴茎而不是性别
	[HarmonyPatch]
	static class Patch_HumanOvum_CanFertilizeReport
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(HumanOvum), "CanFertilizeReport", new[] { typeof(Pawn) });
		}

		static bool hasPenis(Pawn p)
		{
			// 必须使用当前的 hediffSet，否则 has_penis_fertile 会读取存档中的数据（如果小人此后失去了阴茎就会出问题）
			List<Hediff> parts = p?.health?.hediffSet?.hediffs;
			return Genital_Helper.has_penis_fertile(p, parts);
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> CheckForPenisInsteadOfGender(IEnumerable<CodeInstruction> instructions, MethodBase original)
		{
			MethodInfo checkForPenis = AccessTools.Method(typeof(Patch_HumanOvum_CanFertilizeReport), nameof(Patch_HumanOvum_CanFertilizeReport.hasPenis));
			bool foundGenderCheck = false;

			foreach (CodeInstruction ci in instructions)
			{
				string cis = ci.ToString();
				if (cis.Contains("Verse.Gender Verse.Pawn::gender"))
				{
					// 检查有生育力的阴茎而不是男性性别
					foundGenderCheck = true;
					yield return new CodeInstruction(OpCodes.Call, checkForPenis);
				}
				else
				{
					yield return ci;
				}
			}
			if (!foundGenderCheck)
			{
				ModLog.Warning("Failed to patch " + original.Name);
			}
		}
	}

	// 如果婴儿营养不良，即使其永久倒地，也将其标记为需要紧急医疗休息。
	[HarmonyPatch(typeof(HealthAIUtility), nameof(HealthAIUtility.ShouldSeekMedicalRestUrgent), new [] {typeof(Pawn)})]
    static class Patch_HealthAIUtility_ShouldSeekMedicalRestUrgent
    {
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (!ModsConfig.BiotechActive && pawn.DevelopmentalStage.Baby())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
