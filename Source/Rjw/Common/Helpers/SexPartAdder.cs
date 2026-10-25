#nullable enable

using Verse;
using System.Collections.Generic;

namespace rjw
{
	public class SexPartAdder
	{
		/// <summary>
		/// 生成部位状态
		/// </summary>
		public static Hediff MakePart(HediffDef def, Pawn pawn, BodyPartRecord bpr)
		{
			//ModLog.Message("SexPartAdder::PartMaker ( " + xxx.get_pawnname(pawn) + " ) " + def.defName);
			Hediff hd = HediffMaker.MakeHediff(def, pawn, bpr);
			//ModLog.Message("SexPartAdder::PartMaker ( " + xxx.get_pawnname(pawn) + " ) " + hd.def.defName);
			HediffComp_SexPart? compHediff = hd.TryGetComp<HediffComp_SexPart>();
			if (compHediff != null)
			{
				//ModLog.Message("SexPartAdder::PartMaker init comps " + hd.Label);
				compHediff.Init(pawn);
				compHediff.UpdateSeverity();
			}
			return hd;
		}

		/// <summary>
		/// 操作 - 将部位数据从物体转移到状态
		/// </summary>
		public static Hediff recipePartAdder(RecipeDef recipe, Pawn pawn, BodyPartRecord part, List<Thing> ingredients)
		{
			Hediff hd = HediffMaker.MakeHediff(recipe.addsHediff, pawn, part);
			Thing thing = ingredients.Find(x => x.def.defName == recipe.addsHediff.defName);

			CompThingBodyPart? thingComp = thing.TryGetComp<rjw.CompThingBodyPart>();
			HediffComp_SexPart? hediffComp = hd.TryGetComp<rjw.HediffComp_SexPart>();

			if (hediffComp != null && thingComp != null)
			{
				if (thingComp.HasSize())
				{
					if (thingComp.fluid != null && thingComp.fluid != hediffComp.Def.fluid)
					{
						hediffComp.Fluid = thingComp.fluid;
					}
					hediffComp.partFluidMultiplier = thingComp.partFluidMultiplier ?? 1.0f;
					hediffComp.originalOwnerSize = thingComp.originalOwnerSize;
					hediffComp.originalOwnerRace = thingComp.originalOwnerRace;
					hediffComp.previousOwner = thingComp.previousOwner;
					hediffComp.isTransplant = true;
					hediffComp.SetSeverity(thingComp.CalculateSize(hediffComp.GetBodySize()));
				}

				else  //未初始化的物体、空投舱、商人？  //TODO：弄清如何在生成时填充 rjw 部位
					hd = MakePart(hd.def, pawn, part);
			}

			return hd;
		}

		/// <summary>
		/// 操作 - 将部位数据从状态转移到物体
		/// </summary>
		public static Thing? recipePartRemover(Hediff hd)
		{
			Thing? thing = null;

			if (hd.def.spawnThingOnRemoved != null) 
			{
				thing = ThingMaker.MakeThing(hd.def.spawnThingOnRemoved);

				CompThingBodyPart? thingComp = thing.TryGetComp<rjw.CompThingBodyPart>();
				HediffComp_SexPart? hediffComp = hd.TryGetComp<rjw.HediffComp_SexPart>();

				if (thingComp != null && hediffComp != null)
				{
					thingComp.InitFromComp(hediffComp);
				}
			}
			return thing;
		}

		public static void add_genitals(Pawn pawn, Pawn? parent = null, Gender gender = Gender.None)
		{
			//--ModLog.Message("Genital_Helper::add_genitals( " + xxx.get_pawnname(pawn) + " ) called");
			BodyPartRecord? partBPR = Genital_Helper.get_genitalsBPR(pawn);

			//--ModLog.Message("Genital_Helper::add_genitals( " + xxx.get_pawnname(pawn) + " ) - checking genitals");
			if (partBPR == null)
			{
				//--ModLog.Message(" add_genitals( " + xxx.get_pawnname(pawn) + " ) doesn't have a genitals");
				return;
			}
			else if (pawn.health.hediffSet.PartIsMissing(partBPR))
			{
				//--ModLog.Message(" add_genitals( " + xxx.get_pawnname(pawn) + " ) had a genital but was removed.");
				return;
			}

			//用 racesupport 覆盖
			if (pawn.TryAddRacePart(partBPR, gender))
			{
				return;
			}

			if (RJWSettings.DevMode)
			{
				ModLog.Message($"Using default genitals for {pawn.def.defName}");
			}

			//通用 / 旧版添加器
			LegacySexPartAdder.AddGenitals(pawn, parent, gender, partBPR);
		}

		public static void add_breasts(Pawn pawn, Pawn? parent = null, Gender gender = Gender.None)
		{
			//--ModLog.Message(" add_breasts( " + xxx.get_pawnname(pawn) + " ) called");
			BodyPartRecord? partBPR = Genital_Helper.get_breastsBPR(pawn);

			if (partBPR == null)
			{
				//--ModLog.Message(" add_breasts( " + xxx.get_pawnname(pawn) + " ) - pawn doesn't have a breasts");
				return;
			}
			else if (pawn.health.hediffSet.PartIsMissing(partBPR))
			{
				//--ModLog.Message(" add_breasts( " + xxx.get_pawnname(pawn) + " ) had breasts but were removed.");
				return;
			}

			//TODO：弄清如何给男性添加（平坦的）胸部
			//对男性的（平坦）胸部检查在 RacepartDef_Helper.cs 检查严重度曲线时完成
			if (pawn.TryAddRacePart(partBPR, gender))
			{
				return;
			}

			if (RJWSettings.DevMode)
			{
				ModLog.Message($"Using default breasts for {pawn.def.defName}");
			}

			//通用 / 旧版添加器
			LegacySexPartAdder.AddBreasts(pawn, partBPR, parent);
		}

		public static void add_anus(Pawn pawn, Pawn? parent = null, Gender gender = Gender.None)
		{
			BodyPartRecord? partBPR = Genital_Helper.get_anusBPR(pawn);

			if (partBPR == null)
			{
				//--ModLog.Message(" add_anus( " + xxx.get_pawnname(pawn) + " ) doesn't have an anus");
				return;
			}
			else if (pawn.health.hediffSet.PartIsMissing(partBPR))
			{
				//--ModLog.Message(" add_anus( " + xxx.get_pawnname(pawn) + " ) had an anus but was removed.");
				return;
			}

			if (pawn.TryAddRacePart(partBPR, gender))
			{
				return;
			}

			if (RJWSettings.DevMode)
			{
				ModLog.Message($"Using default anus for {pawn.def.defName}");
			}

			//通用 / 旧版添加器
			LegacySexPartAdder.AddAnus(pawn, partBPR, parent);
		}
	}
}
