#nullable enable

using Multiplayer.API;
using rjw.Modules.Shared.Extensions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw
{
	class RacePartDef_Helper
	{
		/// <summary>
		/// 若已选定 partAdders 则返回 true（即使该部位是“无部位”）。
		/// </summary>
		public static bool TryRacePartDef_partAdders(Pawn pawn)
		{
			if (!RaceGroupDef_Helper.TryGetRaceGroupDef(pawn.kindDef, out RaceGroupDef? raceGroupDef))
				return false;

			return TryRacePartDef_partAdders(pawn, raceGroupDef);
		}

		/// <summary>
		/// 若已选定 partAdders 则返回 true（即使该部位是“无部位”）。
		/// </summary>
		[SyncMethod]
		public static bool TryRacePartDef_partAdders(Pawn pawn, RaceGroupDef raceGroupDef)
		{
			if (raceGroupDef.partAdders.NullOrEmpty())
				return false;

			List<BodyPartRecord>? pawnParts = pawn.RaceProps?.body?.AllParts;

			if (pawnParts == null)
			{
				ModLog.Error($"{pawn} has no body");
				return true;
			}

			pawnParts = pawnParts.FindAll(record => !pawn.health.hediffSet.PartIsMissing(record));

			foreach (var Adder in raceGroupDef.partAdders)
			{
				if (Adder.chance <= Rand.Value)
					continue;

				var racePartDef = DefDatabase<RacePartDef>.GetNamedSilentFail(Adder.rjwPart);

				if (racePartDef == null)
				{
					//未找到部位
					continue;
				}

				if (!racePartDef.TryGetHediffDef(out var rjwPartDef))
				{
					//未找到部位
					continue;
				}

				pawnParts = pawnParts.FindAll(record => Adder.bodyParts.Contains(record.def.defName));

				foreach (BodyPartRecord record in pawnParts)
				{
					if (record.IsMissingForPawn(pawn)) continue;

					var hediff = RacePartDef_Helper.MakePart(rjwPartDef, pawn, record, racePartDef);
					pawn.health.AddHediff(hediff, record);
				}
			}
			return true;
		}

		/// <summary>
		/// 若已选定种族部位则返回 true（即使该部位是“无部位”）。
		/// </summary>
		[SyncMethod]
		public static bool TryChooseRacePartDef(Pawn pawn, RaceGroupDef raceGroupDef, BodyPartRecord partBPR, out RacePartDef? racePartDef, Gender gender = Gender.None)
		{
			var partNames = raceGroupDef.GetRacePartDefNames(pawn, partBPR, gender);
			if (partNames == null)
			{
				// 列表缺失，因此未选定任何内容。
				racePartDef = null;
				return false;
			}
			else if (!partNames.Any())
			{
				// 列表为空，因此选定了“无部位”。
				racePartDef = RacePartDef.None;
				return true;
			}

			var chances = raceGroupDef.GetChances(pawn, partBPR, gender);
			var hasChances = chances != null && chances.Count() > 0;

			if (hasChances && chances.Count() != partNames.Count())
			{
				// 无需在运行时处理，这大概应该是 RaceGroupDef 中的配置错误。
				ModLog.Error($"RaceGroupDef named {raceGroupDef.defName} has {partNames.Count()} parts but {chances.Count()} chances for {partBPR.def.defName}.");
				racePartDef = null;
				return false;
			}

			string partName;
			if (hasChances)
			{
				var indexes = partNames.Select((x, i) => i);
				partName = partNames[indexes.RandomElementByWeight(i => chances![i])];
			}
			else
			{
				partName = partNames.RandomElement();
			}

			racePartDef = DefDatabase<RacePartDef>.GetNamedSilentFail(partName);
			if (racePartDef == null)
			{
				ModLog.Error($"Could not find a RacePartDef named {partName} referenced by RaceGroupDef named {raceGroupDef.defName}.");
				return false;
			}
			else
			{
				return true;
			}
		}

		[SyncMethod]
		public static Hediff MakePart(HediffDef hediffDef, Pawn pawn, BodyPartRecord bodyPartRecord, RacePartDef racePartDef)
		{
			var hediff = HediffMaker.MakeHediff(hediffDef, pawn, bodyPartRecord);
			var compHediff = hediff.TryGetComp<HediffComp_SexPart>();
			if (compHediff != null)
			{
				//ModLog.Message("RacePartDef_Helper::PartMaker init comps " + hediff.Label);
				compHediff.Init(pawn);
				if (racePartDef.FluidDef != null)
				{
					compHediff.Fluid = racePartDef.FluidDef;
				}
				if (racePartDef.severityCurve != null && racePartDef.severityCurve.Any())
				{
					// 如果该部位以最小尺寸初始化，就保持不动，
					//  否则按我们的曲线重新随机。（用于男性乳头）
					if(compHediff.GetSeverity() != 0.01f)
					{
						compHediff.SetSeverity(racePartDef.severityCurve.Evaluate(Rand.Value));
					}
				}
				if (racePartDef.fluidModifier.HasValue)
				{
					compHediff.partFluidMultiplier *= racePartDef.fluidModifier.Value;
				}
				compHediff.UpdateSeverity();
			}

			return hediff;
		}

		/// <summary>
		/// 为每个 RJW 状态生成并记录 RacePartDef 的 xml 骨架，以便手动保存为定义文件并由 RaceGroupDef 引用。
		/// 理论上这可以在运行时自动完成。但那样我们可能还想给骨架加入真正的配置，
		/// 所以目前只是把骨架提交进版本库。
		/// </summary>
		public static void GeneratePartShells()
		{
			var defs = DefDatabase<HediffDef_SexPart>.AllDefs.OrderBy(def => def.defName);
			var template = "\t<rjw.RacePartDef>\n\t\t<defName>{0}</defName>\n\t\t<hediffName>{0}</hediffName>\n\t</rjw.RacePartDef>";
			var strings = defs.Select(def => string.Format(template, def.defName));
			ModLog.Message(" RacePartDef shells:\n" + string.Join("\n", strings));
		}
	}
}
