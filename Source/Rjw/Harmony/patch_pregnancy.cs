#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Multiplayer.API;
using UnityEngine;
using Verse;
using RimWorld;
using rjw.Modules.Testing;

using Seeded = rjw.Modules.Rand.Seeded;
using LudeonTK;

namespace rjw
{
	[HarmonyPatch(typeof(Hediff_Pregnant), "DoBirthSpawn")]
	internal static class PATCH_Hediff_Pregnant_DoBirthSpawn
	{
		/// <summary>
		/// 这个方法覆盖原版的怀孕状态行为。
		/// 0 - 为调试怀孕寻找合适的父亲
		/// 第一部分：若角色怀孕且启用了 RJW 怀孕 —— 创建 RJW 怀孕并立即分娩，而不是走原版
		/// 第二部分：若角色怀的是 RJW 怀孕 —— 分娩
		/// 第三部分：调试 —— 创建 RJW／原版怀孕并分娩
		/// </summary>
		/// <param name="mother"></param>
		/// <param name="father"></param>
		/// <returns></returns>
		[HarmonyPrefix]
		[SyncMethod]
		private static bool on_begin_DoBirthSpawn(ref Pawn mother, ref Pawn father)
		{
			//--ModLog.Message("patches_pregnancy::PATCH_Hediff_Pregnant::DoBirthSpawn() called");

			if (mother == null)
			{
				ModLog.Error("Hediff_Pregnant::DoBirthSpawn() - no mother defined -> exit");
				return false;
			}

			//CompEggLayer compEggLayer = mother.TryGetComp<CompEggLayer>();
			//if (compEggLayer != null)
			//{
			//	ProcessVanillaEggPregnancy(mother, father);
			//	return false;
			//}

			//原版调试？
			if (mother.gender == Gender.Male)
			{
				ModLog.Error("Hediff_Pregnant::DoBirthSpawn() - mother is male -> exit");
				return false;
			}

			// 获取我们将要施加的状态的引用
			//为原版怀孕状态执行分娩
			//如果使用 RJW 怀孕 —— 改为添加 RJW 怀孕状态并分娩
			Hediff_Pregnant self = (Hediff_Pregnant)mother.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant);
			if (self != null)
			{
				return ProcessVanillaPregnancy(self, mother, father);
			}

			// 为已有的 RJW 怀孕执行分娩
			if (ProcessRJWPregnancy(mother, father))
			{
				return false;
			}

			return ProcessDebugPregnancy(mother, father);
		}

		private static bool ProcessVanillaEggPregnancy(Pawn mother, Pawn father)
		{
			CompEggLayer compEggLayer = mother.TryGetComp<CompEggLayer>();
			if (compEggLayer != null)
			{
				if (!compEggLayer.FullyFertilized)
				{
					if (father == null)
						compEggLayer.Fertilize(mother);
					else
						compEggLayer.Fertilize(father);

					compEggLayer.ProduceEgg();
				}
			}

			CompHatcher compHatcher = mother.TryGetComp<CompHatcher>();
			if (compHatcher != null)
			{
				compHatcher.Hatch();
			} 

			ModLog.Message("patches_pregnancy::PATCH_Hediff_Pregnant::DoBirthSpawn():ProcessVanillaEggPregnancy birthing:" + xxx.get_pawnname(mother));
			return true;
		}


		private static bool ProcessVanillaPregnancy(Hediff_Pregnant pregnancy, Pawn mother, Pawn father)
		{
			void CreateAndBirth<T>() where T : Hediff_BasePregnancy
			{
				T hediff = Hediff_BasePregnancy.Create<T>(mother, father);
				hediff.GiveBirth();
				if (pregnancy != null)
					mother.health.RemoveHediff(pregnancy);
			}

			if (father == null)
			{
				father = Hediff_BasePregnancy.Trytogetfather(ref mother);
			}

			ModLog.Message("patches_pregnancy::PATCH_Hediff_Pregnant::DoBirthSpawn():Vanilla_pregnancy birthing:" + xxx.get_pawnname(mother));
			if (RJWPregnancySettings.animal_pregnancy_enabled && ((father == null || xxx.is_animal(father)) && xxx.is_animal(mother)))
			{
				//RJW 人兽怀孕 动物-动物
				ModLog.Message(" override as Bestial birthing(animal-animal): Father-" + xxx.get_pawnname(father) + " Mother-" + xxx.get_pawnname(mother));
				CreateAndBirth<Hediff_BestialPregnancy>();
				return false;
			}
			else if (PregnancyHelper.BeastialPregnancyPossible(mother, father))
			{
				//RJW 人兽怀孕 人-动物
				ModLog.Message(" override as Bestial birthing(human-animal): Father-" + xxx.get_pawnname(father) + " Mother-" + xxx.get_pawnname(mother));
				CreateAndBirth<Hediff_BestialPregnancy>();
				return false;
			}
			else if (PregnancyHelper.HumanlikePregnancyPossible(father, mother) && xxx.is_human(father) && xxx.is_human(mother))
			{
				//RJW 人形怀孕
				ModLog.Message(" override as Humanlike birthing: Father-" + xxx.get_pawnname(father) + " Mother-" + xxx.get_pawnname(mother));
				CreateAndBirth<Hediff_HumanlikePregnancy>();
				return false;
			}
			else
			{
				ModLog.Warning("Hediff_Pregnant::DoBirthSpawn() - checks failed, vanilla pregnancy birth");
				ModLog.Warning("Hediff_Pregnant::DoBirthSpawn(): Father-" + xxx.get_pawnname(father) + " Mother-" + xxx.get_pawnname(mother));
				//原版怀孕代码，对 RJW 没有影响

				return true;
			}
		}

		private static bool ProcessRJWPregnancy(Pawn mother, Pawn father)
		{
			var p = PregnancyHelper.GetPregnancies(mother);
			if (p.NullOrEmpty())
			{
				return false;
			}

			var birth = false; 
			foreach (var x in p)
			{
				if (x is Hediff_BasePregnancy)
				{
					var preg = x as Hediff_BasePregnancy;
					ModLog.Message($"patches_pregnancy::{preg.GetType().Name}::DoBirthSpawn() birthing:" + xxx.get_pawnname(mother));
					preg.GiveBirth();
					birth = true;
				}
			}

			return birth;
		}

		private static bool ProcessDebugPregnancy(Pawn mother, Pawn father)
		{
			void CreateAndBirth<T>() where T : Hediff_BasePregnancy
			{
				T hediff = Hediff_BasePregnancy.Create<T>(mother, father);
				hediff.GiveBirth();
			}
			//CreateAndBirth<Hediff_HumanlikePregnancy>();
			//CreateAndBirth<Hediff_BestialPregnancy>();
			//CreateAndBirth<Hediff_MechanoidPregnancy>();
			//return false;

			//调试：添加 RJW 怀孕并分娩
			ModLog.Message("patches_pregnancy::PATCH_Hediff_Pregnant::DoBirthSpawn():Debug_pregnancy birthing:" + xxx.get_pawnname(mother));
			if (father == null)
			{
				father = Hediff_BasePregnancy.Trytogetfather(ref mother);

				if (PregnancyHelper.BeastialPregnancyPossible(mother, father)
					|| (xxx.is_animal(mother) && RJWPregnancySettings.animal_pregnancy_enabled))
				{
					//RJW 人兽怀孕
					ModLog.Message(" override as Bestial birthing, mother: " + xxx.get_pawnname(mother));
					CreateAndBirth<Hediff_BestialPregnancy>();
				}
				else if (RJWPregnancySettings.humanlike_pregnancy_enabled && ((father == null || xxx.is_human(father)) && xxx.is_human(mother)))
				{
					//RJW 人形怀孕
					ModLog.Message(" override as Humanlike birthing, mother: " + xxx.get_pawnname(mother));
					CreateAndBirth<Hediff_HumanlikePregnancy>();
				}
				else
				{
					ModLog.Warning("Hediff_Pregnant::DoBirthSpawn() - debug vanilla pregnancy birth");
					return true;
				}
			}
			return false;
		}
	}


	[HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.TickInterval))]
	class PATCH_Hediff_Pregnant_Tick
	{
		[HarmonyPrefix]
		static bool AbortOnMissingGenitals(Hediff_Pregnant __instance, int delta)
		{
			if (__instance.pawn.IsHashIntervalTick(1000, delta))
			{
				if (!Genital_Helper.has_vagina(__instance.pawn))
				{
					__instance.pawn.health.RemoveHediff(__instance);
				}
			}
			return true;
		}
	}

	// 只要同性伴侣拥有正确的生殖器，就允许他们使用怀孕方式，
	// 防止第一个小人无性别时界面出错，并确保
	// 在男性怀孕的情况下不会误报 "Pawns are sterile"。
	[HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.CanEverProduceChild))]
	class PregnancyUtility_CanEverProduceChild
	{
		static Type thisType = typeof(PregnancyUtility_CanEverProduceChild);

		static readonly MethodInfo SterileMethod =
			AccessTools.Method(typeof(Pawn), nameof(Pawn.Sterile));
		static readonly MethodInfo GetPregnancyHediffMethod =
			AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.GetPregnancyHediff));

		// 2023 年 2 月的 TODO：等 `rjw-race-support` 把
		// 自己的烂摊子收拾好、并修补了它的性别检查之后，再取消第二个版本的注释。
		static bool BypassGenderChecks => true;
		// static bool BypassGenderChecks =>
		// 	!ModsConfig.IsActive("erdelf.HumanoidAlienRaces") ||
		// 	!ModsConfig.IsActive("ASMR.RJW.RaceSupport");

		[HarmonyPrefix]
		static bool CheckPlumbing(Pawn first, Pawn second, ref AcceptanceReport __result)
		{
			if (first.Dead || second.Dead)
			{
				return true;
			}
			
			var plumbingReport = GetPlumbingReport(first, second);
			if (plumbingReport.Accepted)
			{
				return true;
			}

			__result = plumbingReport;
			return false;
		}

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> SkipGenderCheckAndMakeSterilityCheckNotSexist(
			IEnumerable<CodeInstruction> instructionEnumberable, ILGenerator generator)
		{
			var instructions = instructionEnumberable.ToList();

			bool foundGenderCheck = false;
			bool foundSupposedLocalFemaleAssignment = false;
			bool foundFemaleSterilityCheck = false;
			bool foundMaleSterilityCheck = false;

			// 把这个方法按块拆开，可以按返回值的位置来分，也可以按
			// 我们设置局部变量的位置来分。
			static bool findBlock(CodeInstruction il) => il.opcode == OpCodes.Ret || il.IsStloc();
			(var curIns, var nextIns) = instructions.SplitAfter(findBlock);

			while(curIns.Count > 0)
			{
				if (curIns.Any((il) => il.Is(OpCodes.Ldstr, "PawnsHaveSameGender")))
				{
					// 处理同性检查。

					// 我们希望绕过这项检查，除非 Alien Races 模组处于启用状态。
					// RJW 的兼容模组可以改为修补它的 `GenderReproductionCheck`
					// 方法来改变其行为。
					if (!BypassGenderChecks)
					{
						foundGenderCheck = true;
						goto yieldOriginal;
					}

					// 由于优化器处理分支的方式有点古怪，我们必须把绕过逻辑
					// 插在条件判断之后，否则编译器会直接把它删掉。
					// 文档里不提这类事，真是太好了，不是吗？
					(var beforeBr, var afterBr) = curIns.SplitAfter((il) => il.Branches(out _));

					// 确认我们找到了该分支。
					if (afterBr.Count is 0) goto yieldOriginal;

					foreach (var il in beforeBr) yield return il;
					if (beforeBr.LastOrDefault() is { } br && br.Branches(out var bypassLabel))
					{
						foundGenderCheck = true;

						// 在条件判断之后添加 `&& false`。
						yield return new(OpCodes.Ldc_I4_0);
						yield return new(OpCodes.Brfalse_S, bypassLabel);
					}
					foreach (var il in afterBr) yield return il;
					goto setupNextLoop;
				}
				else if (curIns.Any((il) => il.IsStlocOf(1)))
				{
					// 处理把 `pawn2` 初始化到局部变量 #1 的过程：
					// Pawn pawn2 = (first.gender == Gender.Female) ? first : second;

					// 这里需要改成只选择另一个小人，也就是
					// 不是假定的父亲的那一个。Alien Races 可能已经
					// 修补过这里，所以我们得把这点考虑进去。

					// 定位分支指令。
					(var beforeBr, var afterBr) = curIns.SplitAfter((il) => il.Branches(out _));

					// 确认我们找到了该分支。
					if (afterBr.Count is 0) goto yieldOriginal;
					if (beforeBr.LastOrDefault() is not { } br) goto yieldOriginal;
					if (!br.Branches(out var label)) goto yieldOriginal;

					foundSupposedLocalFemaleAssignment = true;

					// 改写为：
					// Pawn pawn2 = (first != pawn) ? first : second;

					// 确保标签保持不变。
					yield return curIns[0].Replace(new CodeInstruction(OpCodes.Ldarg_0));
					yield return new(OpCodes.Ldloc_0);
					yield return new(OpCodes.Bne_Un_S, label);
					// 其余指令负责选择小人，所以我们
					// 可以把它们原样输出。
					foreach (var il in afterBr) yield return il;
					goto setupNextLoop;
				}
				else if (curIns.Any((il) => il.IsStlocOf(6)))
				{
					// 处理把 `flag5` 初始化到局部变量 #6 的过程：
					// bool flag5 = pawn2.Sterile(false) && PregnancyUtility.GetPregnancyHediff(pawn2) == null;

					// 这里需要改成调用 `PregnancyHelper.GetPregnancy`。

					// 查找对 `pawn2.Sterile(false)` 的调用。
					(var callToSterile, var remainderSterile) = curIns.CutMatchingSequence(
						(il) => il.opcode == OpCodes.Ldloc_1,
						(il) => il.Calls(SterileMethod),
						(il) => il.opcode == OpCodes.Brfalse_S
					);

					// 确认找到了该指令序列，找不到就中止。
					if (callToSterile.Count == 0) goto yieldOriginal;

					// 查找对 `PregnancyUtility.GetPregnancyHediff(pawn2)` 的调用。
					(var callToPregnancy, var remainderPregnancy) = remainderSterile.CutMatchingSequence(
						(il) => il.opcode == OpCodes.Ldloc_1,
						(il) => il.Calls(GetPregnancyHediffMethod)
					);

					// 确认找到了该指令序列，找不到就中止。
					if (callToPregnancy.Count == 0) goto yieldOriginal;

					foundFemaleSterilityCheck = true;

					// 把这里改为调用 `PregnancyHelper.GetPregnancy`。
					// 我们保留对 `Sterile` 的调用。
					foreach (var il in callToSterile) yield return il;
					// 设置并调用。
					yield return new(OpCodes.Ldloc_1);
					yield return CodeInstruction.Call(typeof(PregnancyHelper), nameof(PregnancyHelper.GetPregnancy));
					// 其余指令只是检查 `null`，所以应该没问题。
					foreach (var il in remainderPregnancy) yield return il;
					goto setupNextLoop;
				}
				else if (curIns.Any((il) => il.IsStlocOf(7)))
				{
					// 处理把 `flag6` 初始化到局部变量 #7 的过程：
					// bool flag6 = pawn.Sterile(false);

					// 我们需要像对雌性那样，也在这里添加一个对 `PregnancyHelper.GetPregnancy` 的调用——
					// 因为这个小人也可能是扶他之类的。

					// 查找对 `pawn.Sterile(false)` 的调用。
					(var callToSterile, var remainder) = curIns.CutMatchingSequence(
						(il) => il.opcode == OpCodes.Ldloc_0,
						(il) => il.Calls(SterileMethod)
					);

					// 确认找到了该指令序列，找不到就中止。
					if (callToSterile.Count == 0) goto yieldOriginal;

					// 应该只剩 1 条指令，用来设置该变量。
					if (remainder.Count != 1) goto yieldOriginal;

					foundMaleSterilityCheck = true;

					// 我们需要设置一个标签，指向那条会在 `pawn.Sterile(false)` 返回 false 时
					// 把值设为 `false` 的指令。
					var labelToFalse = generator.DefineLabel();
					var IL_SetToFalse = new CodeInstruction(OpCodes.Ldc_I4_0);
					IL_SetToFalse.labels.Add(labelToFalse);

					// 我们还需要在设置该变量的那条指令上放一个标签。
					var labelToSet = generator.DefineLabel();
					remainder[0].labels.Add(labelToSet);

					foreach (var il in callToSterile) yield return il;
					// 如果返回值为 false，就跳到 `IL_SetToFalse`。
					yield return new(OpCodes.Brfalse_S, labelToFalse);
					// 现在写入 `PregnancyHelper.GetPregnancy(pawn) == null`。
					yield return new(OpCodes.Ldloc_0);
					yield return CodeInstruction.Call(typeof(PregnancyHelper), nameof(PregnancyHelper.GetPregnancy));
					yield return new(OpCodes.Ldnull);
					yield return new(OpCodes.Ceq);
					// 如果结果为 `true`，就跳过下一条指令，让该值被写入变量。
					yield return new(OpCodes.Br_S, labelToSet);
					// 否则我们把它设为 `false`。
					yield return IL_SetToFalse;
					// 剩下的指令只是把最后压入栈的值
					// 赋给该变量。
					foreach (var il in remainder) yield return il;
					goto setupNextLoop;
				}

			yieldOriginal:
				foreach (var il in curIns)
					yield return il;
			
			setupNextLoop:
				(curIns, nextIns) = nextIns.SplitAfter(findBlock);
			}

			if (!foundGenderCheck)
				ModLog.Error("Failed to patch PregnancyUtility.CanEverProduceChild: Could not find gender check");
			if (!foundSupposedLocalFemaleAssignment)
				ModLog.Error("Error when patching PregnancyUtility.CanEverProduceChild: Could not find assignment to local variable `pawn2`");
			if (!foundFemaleSterilityCheck)
				ModLog.Error("Error when patching PregnancyUtility.CanEverProduceChild: Could not find sterility check on local variable `pawn2`");
			if (!foundMaleSterilityCheck)
				ModLog.Error("Error when patching PregnancyUtility.CanEverProduceChild: Could not find sterility check on local variable `pawn`");
		}

		static AcceptanceReport GetPlumbingReport(Pawn first, Pawn second)
		{
			bool firstHasPenis = Genital_Helper.has_penis_fertile(first);
			bool firstHasVagina = Genital_Helper.has_vagina(first);

			if (!firstHasPenis && !firstHasVagina)
			{
				return "PawnLacksFunctionalGenitals".Translate(first.Named("PAWN")).Resolve();
			}

			bool secondHasPenis = Genital_Helper.has_penis_fertile(second);
			bool secondHasVagina = Genital_Helper.has_vagina(second);

			if (!secondHasPenis && !secondHasVagina)
			{
				return "PawnLacksFunctionalGenitals".Translate(second.Named("PAWN")).Resolve();
			}
			if ((firstHasPenis && secondHasVagina) || (firstHasVagina && secondHasPenis))
			{
				return true;
			}
			if (firstHasPenis && !secondHasVagina)
			{
				return "PawnLacksVagina".Translate(second.Named("PAWN")).Resolve();
			}
			if (firstHasVagina && !secondHasPenis)
			{
				return "PawnLacksFunctionalPenis".Translate(second.Named("PAWN")).Resolve();
			}
			return true;
		}

		/// <summary>
		/// <para>测试各种配对，以确认补丁工作
		/// 正常。本测试的结果可能受其他模组影响，
		/// 请留意这一点。</para>
		/// <para>测试不会太深入，主要只检查殖民者
		/// 和少量动物。目前不测试与 Alien
		/// Races 之类的集成。RJW 的 Alien Races 兼容模组应当
		/// 自己提供相应的自动测试。</para>
		/// </summary>
		[DebugOutput("RJW Auto-Tests", onlyWhenPlaying = true)]
		public static void TestCanEverProduceChild()
		{
			var logger = Modules.Shared.Logs.LogManager.GetLogger("TestCanEverProduceChild");
			var sb = new StringBuilder();
			var passedColor = GenColor.FromHex("66CC00");
			var failedColor = GenColor.FromHex("FF3300");

			var maleColonistRequest = new PawnGenerationRequest(
				kind: PawnKindDefOf.Colonist,
				fixedGender: Gender.Male,
				excludeBiologicalAgeRange: new(0f, 18f)
			).RequestDefaults();

			var femaleColonistRequest = new PawnGenerationRequest(
				kind: PawnKindDefOf.Colonist,
				fixedGender: Gender.Female,
				allowPregnant: false,
				excludeBiologicalAgeRange: new(0f, 18f)
			).RequestDefaults();

			var maleThrumboRequest = new PawnGenerationRequest(
				kind: PawnKindDefOf.Thrumbo,
				fixedGender: Gender.Male,
				excludeBiologicalAgeRange: new(0f, 8f)
			).RequestDefaults();

			var femaleThrumboRequest = new PawnGenerationRequest(
				kind: PawnKindDefOf.Thrumbo,
				fixedGender: Gender.Female,
				allowPregnant: false,
				excludeBiologicalAgeRange: new(0f, 8f)
			).RequestDefaults();

			// 关于基本性器官的检查。

			Trial("Male Colonist + Female Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				TryToReport(male, female, true);
			});

			Trial("Male Colonist + Male Colonist", () =>
			{
				var male1 = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var male2 = TestHelper.GenerateSeededPawn(maleColonistRequest);
				TryToReport(male1, male2, false);
			});

			Trial("Female Colonist + Female Colonist", () =>
			{
				var female1 = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				var female2 = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				TryToReport(female1, female2, false);
			});

			Trial("Futa Colonist + Female Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (TestHelper.MakeIntoFuta(male))
					TryToReport(male, female, true);
				else
				{
					var text = "Failed to Create Futa".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			Trial("Male Colonist + Futa Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (TestHelper.MakeIntoFuta(female))
					TryToReport(male, female, true);
				else
				{
					var text = "Failed to Create Futa".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			Trial("Futa Colonist + Futa Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (TestHelper.MakeIntoFuta(male) && TestHelper.MakeIntoFuta(female))
					TryToReport(male, female, true);
				else
				{
					var text = "Failed to Create Futa".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			// 关于怀孕的测试。

			Trial("Male Colonist + Pregnant Female Colonist (Vanilla)", () =>
			{
				if (!ModsConfig.BiotechActive)
				{
					sb.AppendLine("  Biotech Not Active");
					return;
				}

				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (VanillaHumanPregnancy(female, male) is { } hediff)
				{
					female.health.AddHediff(hediff);
					TryToReport(male, female, true);
				}
				else
				{
					var text = "Failed to Impregnate".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			Trial("Male Colonist + Pregnant Female Colonist (RJW)", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				PregnancyHelper.AddPregnancyHediff(female, male);
				TryToReport(male, female, true);
			});

			Trial("Pregnant Male Colonist + Female Colonist (Vanilla)", () =>
			{
				if (!ModsConfig.BiotechActive)
				{
					sb.AppendLine("  Biotech Not Active");
					return;
				}

				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (VanillaHumanPregnancy(male, female) is { } hediff)
				{
					male.health.AddHediff(hediff);
					TryToReport(male, female, true);
				}
				else
				{
					var text = "Failed to Impregnate".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			Trial("Pregnant Male Colonist + Female Colonist (RJW)", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				PregnancyHelper.AddPregnancyHediff(male, female);
				TryToReport(male, female, true);
			});

			// 关于人造生殖器和绝育的测试。

			Trial("Sterilized Male Colonist + Female Colonist", () =>
			{
				if (!ModsConfig.BiotechActive)
				{
					sb.AppendLine("  Biotech Not Active");
					return;
				}

				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				male.health.AddHediff(HediffDefOf.Sterilized);
				TryToReport(male, female, false);
			});

			Trial("Artificially Male Colonist + Female Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (TestHelper.GiveArtificialGenitals(male))
					TryToReport(male, female, false);
				else
				{
					var text = "Failed to Render Artifical".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			Trial("Male Colonist + Sterilized Female Colonist", () =>
			{
				if (!ModsConfig.BiotechActive)
				{
					sb.AppendLine("  Biotech Not Active");
					return;
				}

				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				female.health.AddHediff(HediffDefOf.Sterilized);
				TryToReport(male, female, false);
			});

			Trial("Male Colonist + Artificially Female Colonist", () =>
			{
				// 顺带一提，那个液压阴道居然还是有生育力的。
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				if (TestHelper.GiveArtificialGenitals(female))
					TryToReport(male, female, true);
				else
				{
					var text = "Failed to Render Artifical".Colorize(failedColor);
					sb.AppendLineTagged($"  {text}");
				}
			});

			// 检查以确认性别检查已被正确禁用。

			Trial("Male Colonist + Cuntboy Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				female.gender = Gender.Male;
				TryToReport(male, female, true);
			});

			Trial("Dickgirl Colonist + Female Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				male.gender = Gender.Female;
				TryToReport(male, female, true);
			});

			// 针对动物的测试。

			Trial("Male Thrumbo + Female Thrumbo", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleThrumboRequest);
				var female = TestHelper.GenerateSeededPawn(femaleThrumboRequest);
				TryToReport(male, female, true);
			});

			Trial("Male Thrumbo + Female Colonist", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleThrumboRequest);
				var female = TestHelper.GenerateSeededPawn(femaleColonistRequest);
				TryToReport(male, female, true);
			});

			Trial("Male Colonist + Female Thrumbo", () =>
			{
				var male = TestHelper.GenerateSeededPawn(maleColonistRequest);
				var female = TestHelper.GenerateSeededPawn(femaleThrumboRequest);
				TryToReport(male, female, true);
			});

			Find.WindowStack.Add(new Dialog_MessageBox(sb.ToString().Trim()));

			void Trial(string title, Action action)
			{
				try
				{
					sb.AppendLine(title);
					action();
				}
				catch (Exception ex)
				{
					var result = "Exception".Colorize(failedColor);
					sb.AppendLineTagged($"  {result} (see console logs)");
					logger.Error("Auto-test exception.", ex);
				}
				finally
				{
					sb.AppendLine();
				}
			}

			// 尝试给 `mother` 一个人类怀孕。
			Hediff_Pregnant VanillaHumanPregnancy(Pawn mother, Pawn father)
			{
				using var _seeded = Seeded.With(mother);
				var hediff = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, mother);
				hediff.Severity = 0.1f;
				var inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(father, mother, out var success);
				if (success)
				{
					hediff.SetParents(null, father, inheritedGeneSet);
					return hediff;
				}
				return null;
			}

			// 把这两个小人分别放到两个参数位置上各试一次。
			void TryToReport(Pawn pawn1, Pawn pawn2, bool expectAccepted)
			{
				var text = expectAccepted ? "Accepted" : "Rejected";
				sb.AppendLine($"  Expecting: {text}");
				var result1 = PregnancyUtility.CanEverProduceChild(pawn1, pawn2);
				sb.AppendLine("  Given Order");
				sb.AppendLineTagged($"    Status: {ReportToStatus(result1, expectAccepted)}");
				sb.AppendLineTagged($"    Reason: {ReportToReason(result1)}");
				var result2 = PregnancyUtility.CanEverProduceChild(pawn2, pawn1);
				sb.AppendLine("  Reversed Order");
				sb.AppendLineTagged($"    Status: {ReportToStatus(result2, expectAccepted)}");
				sb.AppendLineTagged($"    Reason: {ReportToReason(result2)}");
			}

			string ReportToStatus(AcceptanceReport report, bool expectAccepted) =>
				(report.Accepted, expectAccepted) switch
				{
					(true, true) or (false, false) => "Passed".Colorize(passedColor),
					_ => "Failed".Colorize(failedColor)
				};

			string ReportToReason(AcceptanceReport report) =>
				report.Reason.NullOrEmpty() ? "(None)" : report.Reason;
		}
	}

	[HarmonyPatch(typeof(PregnancyUtility), "RandomLastName")]
	static class Patch_PregnancyUtility_RandomLastName
	{
		public static bool Prefix(ref Pawn geneticMother, ref Pawn birthingMother, ref Pawn father)
		{
			if (geneticMother != null)
				if (geneticMother.Name == null || !xxx.is_human(geneticMother))
				{
					geneticMother = null;
				}
			if (father != null)
				if (father.Name == null || !xxx.is_human(father))
				{
					father = null;
				}
			if (birthingMother != null)
				if (birthingMother.Name == null || !xxx.is_human(birthingMother))
				{
					birthingMother = null;
				}
			return true;
		}
	}
	
	[HarmonyPatch(typeof(PawnNamingUtility), nameof(PawnNamingUtility.NamePawnDialog))]
	public class Patch_NamePawnDialog_Nullcheck_pawn_Name
	{
		// 原版假定参与怀孕的每个小人都必须有名字
		// 这在人类×野生动物的怀孕中会出问题，因为野生动物没有名字
		// 原版只检查小人本身是否为 null，却不检查 pawn.Name
		// 这个转译器插入一个对 checkName 的调用，把任何没有名字的小人置为 null
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{

			MethodInfo getFather =
				AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetFather));
			
			MethodInfo getMother =
				AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetMother));
			
			MethodInfo getBirthParent =
				AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetBirthParent));

			MethodInfo helper =
				AccessTools.Method(typeof(Patch_NamePawnDialog_Nullcheck_pawn_Name), nameof(CheckName));

			foreach (CodeInstruction code in instructions)
			{
				yield return code;
				if (code.Calls(getFather) || code.Calls(getMother) || code.Calls(getBirthParent))
				{
					yield return new CodeInstruction(OpCodes.Call, helper);
				}
			}
		}

		public static Pawn CheckName(Pawn pawn)
		{
			return pawn?.Name==null ? null : pawn;
		}
	}

	// 修复 CompEggLayer 以及人形生物的交配。
	// 注意这会跳过对 Biotech DLC 的检查，但截至 1.4.3534 版本，这样做并不会解锁
	// 任何 Biotech 专属功能之类的东西。
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.Sterile))]
	class Pawn_Sterile
	{
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> NotOwningBiotechWillNotMakeYouSterileButModdingProbablyWill(
			IEnumerable<CodeInstruction> instructions)
		{
			PropertyInfo biotechActive = AccessTools.DeclaredProperty(typeof(ModsConfig), nameof(ModsConfig.BiotechActive));
			bool foundBiotechCheck = false;

			foreach (var instruction in instructions)
			{
				// if (ModsConfig.BiotechActive) ...
				// => if (true) ...
				if (instruction.Calls(biotechActive.GetMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					foundBiotechCheck = true;
					continue;
				}
				yield return instruction;
			}

			if (!foundBiotechCheck)
			{
				ModLog.Error("Failed to patch Pawn.Sterile: Could not find `ModsConfig.BiotechActive`");
			}
		}
	}

	// 让游戏在原版怀孕概率计算中使用 RJW 的生育力
	[HarmonyPatch]
	class Various_GetStatValue
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.CanEverProduceChild));
			yield return AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.PregnancyChanceForPawn));
			yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.Sterile));
		}

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> UseRjwFertilityInstead(IEnumerable<CodeInstruction> instructionEnumerable, MethodBase original)
		{
			bool foundFertilityStatDefLoad = false;
			FieldInfo statDefOfFertility = AccessTools.DeclaredField(typeof(StatDefOf), nameof(StatDefOf.Fertility));

			var instructions = instructionEnumerable.ToList();
			for (int i = 0; i < instructions.Count; i++)
			{
				// pawn.GetStatValue(StatDefOf.Fertility)
				// => pawn.health.capacities.GetLevel(xxx.reproduction)
				if (instructions[i].LoadsField(statDefOfFertility) && i + 3 < instructions.Count)
				{
					foundFertilityStatDefLoad = true;
					yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.health)).WithLabels(instructions[i].ExtractLabels());
					yield return CodeInstruction.LoadField(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.capacities));
					yield return CodeInstruction.LoadField(typeof(xxx), nameof(xxx.reproduction));
					yield return CodeInstruction.Call(typeof(PawnCapacitiesHandler), nameof(PawnCapacitiesHandler.GetLevel));

					// 跳过对 GetStatValue 的调用
					i += 3;
					continue;
				}
				yield return instructions[i];
			}

			if (!foundFertilityStatDefLoad)
			{
				ModLog.Error($"Failed to patch {original.Name}: Could not find `pawn.GetStatValue(StatDefOf.Fertility)`");
			}
		}
	}

	// 主要用于防止由 RJW 怀孕出生的小人被命名为 "Baby"。
	[HarmonyPatch(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
	class PawnBioAndNameGenerator_GiveAppropriateBioAndNameTo
	{
		static readonly MethodInfo getPawnDevelopmentalStage = AccessTools.Method(typeof(Pawn), "get_DevelopmentalStage");

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionEnumerable, MethodBase original)
		{
			var instructions = instructionEnumerable.ToList();
			bool found = false;

			for (int i = 0; i < instructions.Count; i++)
			{
				// 把 bool flag = pawn.DevelopmentalStage.Baby(); 替换为 bool flag = isBiotechBaby(Pawn pawn);
				if (instructions[i].Calls(getPawnDevelopmentalStage))
				{
					yield return CodeInstruction.Call(typeof(PawnBioAndNameGenerator_GiveAppropriateBioAndNameTo), nameof(isBiotechBaby));
					i++;
					found = true;

				}
				else
				{
					yield return instructions[i];
				}

			}
			if (!found)
			{
				ModLog.Error($"Failed to patch {original.Name}: could not find pawn.DevelopmentalStage");
			}
		}

		public static bool isBiotechBaby(Pawn pawn)
		{
			return pawn.DevelopmentalStage.Baby() && RJWPregnancySettings.UseVanillaPregnancy;
		}

	}

	// 调整怀孕方式的提示信息，以反映它已不再直接影响怀孕概率这一事实。
	[HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.GetDescription))]
	class PregnancyUtility_GetDescription
	{
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> ModifyPregnancyApproachTooltip(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsConstant("PregnancyChance"))
				{
					yield return new CodeInstruction(OpCodes.Ldstr, "VaginalWeight");
				}
				else
				{
					yield return instruction;
				}
			}
		}
	}

	[HarmonyPatch(typeof(PawnColumnWorker_Pregnant), "GetIconFor")]
	public class PawnColumnWorker_Patch_Icon
	{
		public static void Postfix(Pawn pawn, ref Texture2D __result)
		{
			if (pawn.IsVisiblyPregnant()) __result = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Pregnant", true);
		}
	}

	[HarmonyPatch(typeof(PawnColumnWorker_Pregnant), "GetTooltipText")]
	public class PawnColumnWorker_Patch_Tooltip
	{
		public static bool Prefix(Pawn pawn, ref string __result)
		{
			// 处理多胎怀孕：取最接近分娩的那一个。
			var pregHediff = PregnancyHelper.GetPregnancy(pawn);
			(var ticksCompleted, var ticksToBirth) = PregnancyHelper.GetProgressTicks(pregHediff);
			__result = "PregnantIconDesc".Translate(
				ticksCompleted.ToStringTicksToDays("F0"),
				ticksToBirth.ToStringTicksToDays("F0")
			);
			return false;
		}
	}

	[HarmonyPatch(typeof(TransferableUIUtility), "DoExtraIcons")]
	public class TransferableUIUtility_Patch_Icon
	{
		//private static readonly Texture2D PregnantIcon = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Pregnant", true);
		public static void Postfix(Transferable trad, Rect rect, ref float curX, Texture2D ___PregnantIcon)
		{
			Pawn pawn = trad.AnyThing as Pawn;
			if (pawn?.health?.hediffSet != null && pawn.IsVisiblyPregnant())
			{
				Rect rect3 = new Rect(curX - 24f, (rect.height - 24f) / 2f, 24f, 24f);
				curX -= 24f;
				if (Mouse.IsOver(rect3))
				{
					TooltipHandler.TipRegion(rect3, PawnColumnWorker_Pregnant.GetTooltipText(pawn));
				}
				GUI.DrawTexture(rect3, ___PregnantIcon);
			}
		}
	}

	//[HarmonyPatch(typeof(AutoSlaughterManager))]
	//public class AutoSlaughterManager_AnimalsToSlaughter_rjw_preg
	//{
	//	public static void Postfix(AutoSlaughterManager __instance)
	//	{
	//		List<Pawn> __result = new List<Pawn>();
	//		var any_ins = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	//		__result = (List<Pawn>)(__instance.GetType().GetField("AnimalsToSlaughter", any_ins).GetValue(__instance));
	//		//(__instance.GetType().GetField("AnimalsToSlaughter", any_ins).SetValue(__instance, newresult));

	//		//List<Pawn> __result = new List<Pawn>((List < Pawn > )typeof(AutoSlaughterManager).GetField("AnimalsToSlaughter", BindingFlags.Instance | BindingFlags.NonPublic));
	//		if (!__result.NullOrEmpty())
	//		{
	//			var newresult = __result;
	//			foreach (var pawn in __result)
	//			{
	//				if (pawn?.health?.hediffSet != null && pawn.IsPregnant(true))
	//				{
	//					if (__instance.configs.Any(x => x.allowSlaughterPregnant != true && x.animal == pawn.def))
	//					{
	//						newresult.Remove(pawn);
	//					}
	//				}

	//			}
	//			__result = newresult;
	//		}

	//		//return __result;
	//	}
	//}
}
