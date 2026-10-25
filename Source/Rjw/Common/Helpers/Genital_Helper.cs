#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using rjw.Modules.Testing;
using LudeonTK;

namespace rjw
{
	public static class Genital_Helper
	{
		[Obsolete("Use Genital_Helper.average_anus instead")]
		public static HediffDef generic_anus = HediffDef.Named("GenericAnus");
		[Obsolete("Use Genital_Helper.average_penis instead")]
		public static HediffDef generic_penis = HediffDef.Named("GenericPenis");
		[Obsolete("Use Genital_Helper.average_vagina instead")]
		public static HediffDef generic_vagina = HediffDef.Named("GenericVagina");
		[Obsolete("Use Genital_Helper.average_breasts instead")]
		public static HediffDef generic_breasts = HediffDef.Named("GenericBreasts");

		public static HediffDef average_penis = HediffDef.Named("Penis");
		public static HediffDef hydraulic_penis = HediffDef.Named("HydraulicPenis");
		public static HediffDef bionic_penis = HediffDef.Named("BionicPenis");
		public static HediffDef archotech_penis = HediffDef.Named("ArchotechPenis");

		public static HediffDef average_vagina = HediffDef.Named("Vagina");
		public static HediffDef hydraulic_vagina = HediffDef.Named("HydraulicVagina");
		public static HediffDef bionic_vagina = HediffDef.Named("BionicVagina");
		public static HediffDef archotech_vagina = HediffDef.Named("ArchotechVagina");

		public static HediffDef average_breasts = HediffDef.Named("Breasts");
		public static HediffDef hydraulic_breasts = HediffDef.Named("HydraulicBreasts");
		public static HediffDef bionic_breasts = HediffDef.Named("BionicBreasts");
		public static HediffDef archotech_breasts = HediffDef.Named("ArchotechBreasts");
		public static HediffDef featureless_chest = HediffDef.Named("FeaturelessChest");
		public static HediffDef udder_breasts = HediffDef.Named("UdderBreasts");

		public static HediffDef average_anus = HediffDef.Named("Anus");
		public static HediffDef hydraulic_anus = HediffDef.Named("HydraulicAnus");
		public static HediffDef bionic_anus = HediffDef.Named("BionicAnus");
		public static HediffDef archotech_anus = HediffDef.Named("ArchotechAnus");

		public static HediffDef peg_penis = HediffDef.Named("PegDick");

		public static HediffDef insect_anus = HediffDef.Named("InsectAnus");
		public static HediffDef ovipositorM = HediffDef.Named("OvipositorM");
		public static HediffDef ovipositorF = HediffDef.Named("OvipositorF");

		public static HediffDef demonT_penis = HediffDef.Named("DemonTentaclePenis");
		public static HediffDef demon_penis = HediffDef.Named("DemonPenis");
		public static HediffDef demon_vagina = HediffDef.Named("DemonVagina");
		public static HediffDef demon_anus = HediffDef.Named("DemonAnus");

		public static HediffDef slime_breasts = HediffDef.Named("SlimeBreasts");
		public static HediffDef slime_penis = HediffDef.Named("SlimeTentacles");
		public static HediffDef slime_vagina = HediffDef.Named("SlimeVagina");
		public static HediffDef slime_anus = HediffDef.Named("SlimeAnus");

		public static HediffDef feline_penis = HediffDef.Named("CatPenis");
		public static HediffDef feline_vagina = HediffDef.Named("CatVagina");

		public static HediffDef canine_penis = HediffDef.Named("DogPenis");
		public static HediffDef canine_vagina = HediffDef.Named("DogVagina");

		public static HediffDef equine_penis = HediffDef.Named("HorsePenis");
		public static HediffDef equine_vagina = HediffDef.Named("HorseVagina");

		public static HediffDef dragon_penis = HediffDef.Named("DragonPenis");
		public static HediffDef dragon_vagina = HediffDef.Named("DragonVagina");

		public static HediffDef raccoon_penis = HediffDef.Named("RaccoonPenis");

		public static HediffDef hemipenis = HediffDef.Named("HemiPenis");

		public static HediffDef crocodilian_penis = HediffDef.Named("CrocodilianPenis");

		// 我不太喜欢通过字符串匹配来查找定义，但
		// 我想这样能应付那些古怪不听话的种族模组。

		public static readonly BodyPartTagDef BodyPartTagDefEatingSource = DefDatabase<BodyPartTagDef>.GetNamed("EatingSource");
		public static readonly BodyPartTagDef BodyPartTagDefMetabolismSource = DefDatabase<BodyPartTagDef>.GetNamed("MetabolismSource");
		public static readonly BodyPartTagDef BodyPartTagDefTongue = DefDatabase<BodyPartTagDef>.GetNamed("Tongue");

		private static readonly HashSet<BodyPartDef> mouthBPs =
			DefDatabase<BodyPartDef>.AllDefsListForReading
				.Where((def) => def.defName.ToLower().ContainsAny("mouth", "teeth", "jaw", "beak") || def.tags.Contains(BodyPartTagDefEatingSource))
				.ToHashSet();

		private static readonly HashSet<BodyPartDef> tongueBPs =
			DefDatabase<BodyPartDef>.AllDefsListForReading
				.Where((def) => def.defName.ToLower().Contains("tongue") || def.tags.Contains(BodyPartTagDefTongue))
				.ToHashSet();

		private static readonly HashSet<BodyPartDef> stomachBPs =
			DefDatabase<BodyPartDef>.AllDefsListForReading
				.Where((def) => def.defName.ToLower().Contains("stomach") || def.tags.Contains(BodyPartTagDefMetabolismSource))
				.ToHashSet();

		private static readonly HashSet<BodyPartDef> torsoBPs =
			DefDatabase<BodyPartDef>.AllDefsListForReading
				.Where((def) => def.defName.ToLower().Contains("torso"))
				.ToHashSet();

		private static readonly HashSet<BodyPartDef> tailBPs =
			DefDatabase<BodyPartDef>.AllDefsListForReading
				.Where((def) => def.defName.ToLower().Contains("tail"))
				.ToHashSet();

		private static readonly HashSet<BodyPartDef> bodyBPs =
			DefDatabase<BodyPartDef>.AllDefsListForReading
				.Where((def) => def.defName.ToLower().Contains("body"))
				.ToHashSet();

		private static readonly BodyPartTagDef bodyPartTagDef_tongue =
			DefDatabase<BodyPartTagDef>.GetNamed("Tongue");

		// 这些 BPR 由 RJW 添加。它们必须附有某种性部件
		// 才算作存在。

		public static BodyPartRecord get_genitalsBPR(Pawn pawn)
		{
			//--ModLog.Message("Genital_Helper::get_genitals( " + xxx.get_pawnname(pawn) + " ) called");
			var bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == xxx.genitalsDef);
			if (bpr is null) bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == xxx.mechgenitalsDef);
			if (bpr is not null) return bpr;

			//--ModLog.Message(" get_genitals( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		public static BodyPartRecord get_anusBPR(Pawn pawn)
		{
			//--ModLog.Message(" get_anus( " + xxx.get_pawnname(pawn) + " ) called");
			var bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == xxx.anusDef);
			if (bpr is null) bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == xxx.mechanusDef);
			if (bpr is not null) return bpr;

			//--ModLog.Message(" get_anus( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		public static BodyPartRecord get_breastsBPR(Pawn pawn)
		{
			//--ModLog.Message(" get_breasts( " + xxx.get_pawnname(pawn) + " ) called");
			var bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == xxx.breastsDef);
			if (bpr is null) bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == xxx.mechbreastsDef);
			if (bpr is not null) return bpr;

			//--ModLog.Message(" get_breasts( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		public static BodyPartRecord get_BPR(Pawn pawn, BodyPartDef bprdef)
		{
			if (bprdef is null) return null;
			//--ModLog.Message(" get_BPR( " + xxx.get_pawnname(pawn) + " ) called");
			var bpr = pawn?.RaceProps.body.AllParts.Find(bpr => bpr.def == bprdef);
			if (bpr is not null) return bpr;

			//--ModLog.Message(" get_BPR( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		// 这些 BPR 要么是原有的，要么由其他模组添加。只要它们上面
		// 没有 `Hediff_PartRemoved`，就被视为存在。使用 `has_bpr`
		// 方法来检查它们没有被砍掉、炸掉、砸掉，或遭遇
		// 其他可能降临到可怜小人身上的可怕事情。

		public static BodyPartRecord get_mouthBPR(Pawn pawn)
		{
			//--ModLog.Message(" get_mouth( " + xxx.get_pawnname(pawn) + " ) called");
			var bpr = pawn?.RaceProps.body.AllParts.Find((bpr) => mouthBPs.Contains(bpr.def));
			if (bpr is not null) return bpr;

			//--ModLog.Message(" get_mouth( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		public static BodyPartRecord get_tongueBPR(Pawn pawn)
		{
			//--ModLog.Message(" get_tongue( " + xxx.get_pawnname(pawn) + " ) called");
			if (pawn?.RaceProps.body.AllParts is { } allParts)
			{
				foreach (var bpr in allParts)
				{
					if (tongueBPs.Contains(bpr.def)) return bpr;
					if (bpr.def.tags.Contains(bodyPartTagDef_tongue)) return bpr;
				}
			}

			//--ModLog.Message(" get_tongue( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		public static BodyPartRecord get_stomachBPR(Pawn pawn)
		{
			//--ModLog.Message(" get_stomach( " + xxx.get_pawnname(pawn) + " ) called");
			if (pawn?.RaceProps.body.AllParts is { } allParts)
			{
				var bpr = allParts.Find((bpr) => stomachBPs.Contains(bpr.def));
				if (bpr is not null) return bpr;

				//--ModLog.Message(" get_stomach( " + xxx.get_pawnname(pawn) + " ) - Part is null, trying to get torso...");
				bpr = allParts.Find((bpr) => torsoBPs.Contains(bpr.def));
				if (bpr is not null) return bpr;
			}

			//--ModLog.Message(" get_stomach( " + xxx.get_pawnname(pawn) + " ) - Part is null, no stomach or torso.");
			return null;
		}

		public static BodyPartRecord get_tailBPR(Pawn pawn)
		{
			//大概应该按体型/种族做个检查之类的
			//--ModLog.Message(" get_tail( " + xxx.get_pawnname(pawn) + " ) called");
			var bpr = pawn?.RaceProps.body.AllParts.Find(bpr => tailBPs.Contains(bpr.def));
			if (bpr is not null) return bpr;

			//--ModLog.Message(" get_tail( " + xxx.get_pawnname(pawn) + " ) - Part is null");
			return null;
		}

		public static BodyPartRecord get_torsoBPR(Pawn pawn)
		{
			//--ModLog.Message(" get_torsoBPR( " + xxx.get_pawnname(pawn) + " ) called");
			if (pawn?.RaceProps.body.AllParts is { } allParts)
			{
				var bpr = allParts.Find((bpr) => torsoBPs.Contains(bpr.def));
				if (bpr is not null) return bpr;

				//--ModLog.Message(" get_torsoBPR( " + xxx.get_pawnname(pawn) + " ) - Part is null, trying to get body...");
				bpr = allParts.Find((bpr) => bodyBPs.Contains(bpr.def));
				if (bpr is not null) return bpr;
			}

			//--ModLog.Message(" get_torsoBPR( " + xxx.get_pawnname(pawn) + " ) - Part is null, no torso or body");
			return null;
		}

		public static bool breasts_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_breasts);
		}

		public static bool anus_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_anus);
		}

		public static bool genitals_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_penis || def.blocks_vagina);
		}

		public static bool hands_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_hands);
		}

		public static bool penis_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_penis);
		}

		public static bool oral_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_oral);
		}

		public static bool vagina_blocked(Pawn pawn)
		{
			if (pawn.apparel?.WornApparel is not { } wornApparel) return false;
			return wornApparel.ToBondageGear().Any((def) => def.blocks_vagina);
		}

		private static IEnumerable<bondage_gear_def> ToBondageGear(this List<Apparel> wornApparel) =>
			wornApparel.Select((app) => app.def).OfType<bondage_gear_def>();

		public static bool is_sex_part(Hediff hed) => hed is ISexPartHediff;

		public static List<Hediff> get_PartsHediffList(Pawn pawn, BodyPartRecord Part) =>
			pawn.health.hediffSet.hediffs.FindAll((hed) => is_sex_part(hed) && hed.Part == Part);

		public static List<Hediff> get_AllPartsHediffList(Pawn pawn) =>
			pawn.health.hediffSet.hediffs.FindAll(is_sex_part);

		public static bool has_genitals(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			return !parts.NullOrEmpty();
		}

		public static bool has_breasts(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetBreastList();
			if (parts.NullOrEmpty()) return false;

			return parts.Any((hed) => hed.def != featureless_chest);
		}

		public static bool has_male_breasts(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetBreastList();
			if (parts.NullOrEmpty()) return false;

			return parts.Any((hed) => is_sex_part(hed) && hed.CurStageIndex == 0);
		}

		/// <summary>
		/// 如果乳房为平均水平或更大，可以做乳交
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="parts">预先获取的部件（如果有）。</param>
		/// <returns></returns>
		public static bool can_do_breastjob(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetBreastList();
			if (parts.NullOrEmpty()) return false;

			return parts.Any((hed) => is_sex_part(hed) && hed.CurStageIndex > 1);
		}

		public static bool has_tail(Pawn pawn) =>
			has_bpr(pawn, get_tailBPR(pawn));

		public static bool has_mouth(Pawn pawn) =>
			has_bpr(pawn, get_mouthBPR(pawn));

		public static bool has_tongue(Pawn pawn) =>
			has_bpr(pawn, get_tongueBPR(pawn));

		/// <summary>
		/// <para>检查某个身体部位是否实际连接着。</para>
		/// <para>这适用于不是由 RJW 添加的 BPR。对于那些 BPR，
		/// 如果它们没有附带的性部件，我们就认为
		/// 该部件缺失。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="bpr">指明该身体部位的 `BodyPartRecord`。</param>
		/// <returns>该部件是否缺失。</returns>
		public static bool has_bpr(Pawn pawn, BodyPartRecord bpr)
		{
			if (bpr is null) return false;
			return !pawn.health.hediffSet.hediffs
				.Where((hed) => hed.Part == bpr)
				.Any((hed) => hed is Hediff_MissingPart);
		}

		public static bool has_anus(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetAnusList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_anus(parts[i]))
					return true;
			return false;
		}

		public static bool is_anus(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			return def.genitalFamily == GenitalFamily.Anus;
		}

		/// <summary>
		/// 可插入的，既包括阴道也包括产卵管
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="parts"></param>
		/// <returns></returns>
		public static bool has_vagina(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_vagina(parts[i]))
					return true;
			return false;
		}

		public static bool has_vagina_fertile(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_fertile_vagina(parts[i]))
					return true;
			return false;
		}

		private static bool is_vagina_family(GenitalFamily family) => family switch
		{
			GenitalFamily.Vagina => true,
			GenitalFamily.FemaleOvipositor => true,
			_ => false
		};

		public static bool is_vagina(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def)
			{
				return false;
			}
			return is_vagina_family(def.genitalFamily);
		}

		public static bool is_fertile_vagina(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			if (!is_vagina_family(def.genitalFamily)) return false;

			if (def.genitalTags.Contains(GenitalTag.CanBeFertilized))
				return true;

			return false;
		}

		/// <summary>
		/// <para>检查小人是否有阴茎，无论是否有生育力。</para>
		/// <para>这包括雄性产卵管，但不包括雌性产卵管（后者
		/// 更被视为阴道）。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="parts">预先获取的部件（如果有）。</param>
		public static bool has_male_bits(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			return parts.Any(is_penis);
		}

		public static bool has_penis_fertile(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_fertile_penis(parts[i]))
					return true;
			return false;
		}

		public static bool has_penis_infertile(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_infertile_penis(parts[i]))
					return true;
			return false;
		}

		private static bool is_penis_family(GenitalFamily family) => family switch
		{
			GenitalFamily.Penis => true,
			GenitalFamily.MaleOvipositor => true,
			_ => false
		};

		private static bool is_fertile_tag(GenitalTag tag) => tag switch
		{
			GenitalTag.CanFertilize => true,
			GenitalTag.CanFertilizeEgg => true,
			_ => false
		};

		public static bool is_penis(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			return is_penis_family(def.genitalFamily);
		}


		public static bool is_fertile_penis(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			if (!is_penis_family(def.genitalFamily)) return false;

			for (int i = 0; i < def.genitalTags.Count; i++)
				if (is_fertile_tag(def.genitalTags[i]))
					return true;
			return false;
		}

		public static bool is_infertile_penis(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			if (!is_penis_family(def.genitalFamily)) return false;

			for (int i = 0; i < def.genitalTags.Count; i++)
				if (is_fertile_tag(def.genitalTags[i]))
					return false;
			return true;
		}

		public static bool has_multipenis(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			int count = 0;
			foreach (Hediff hed in parts)
			{
				if (!is_penis(hed)) continue;

				// 匹配半阴茎。
				var props = (hed.def as HediffDef_SexPart)?.tags;
				if (!props.NullOrEmpty() && props.Contains("Multiple"))
				{
					return true;
				}


				count += 1;
				if (count > 1) return true;
			}

			return false;
		}

		public static bool has_ovipositorM(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_male_ovipositor(parts[i]))
					return true;
			return false;

		}

		public static bool is_male_ovipositor(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			return def.genitalFamily == GenitalFamily.MaleOvipositor;
		}

		public static bool has_ovipositorF(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			for (int i = 0; i < parts.Count; i++)
				if (is_female_ovipositor(parts[i]))
					return true;
			return false;
		}

		public static bool is_female_ovipositor(Hediff hed)
		{
			if (hed.def is not HediffDef_SexPart def) return false;
			return def.genitalFamily == GenitalFamily.FemaleOvipositor;
		}

		/// <summary>
		/// 如果阴茎巨大或更大，可以自我口交
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="parts">预先获取的部件（如果有）。</param>
		/// <returns></returns>
		public static bool can_do_autofelatio(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			if (parts.NullOrEmpty()) return false;

			return parts.Any((hed) => is_penis(hed) && hed.CurStageIndex > 3);
		}

		/// <summary>
		/// 只统计有生育力的阴茎
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="parts">预先获取的部件（如果有）。</param>
		/// <returns></returns>
		public static bool is_futa(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			return has_vagina(pawn, parts) && has_penis_fertile(pawn, parts);
		}

		public static int min_EggsProduced(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			// 没有合适的部件 - 返回 0
			if (parts.NullOrEmpty()) return 0;

			// 如果 xml 文件中未定义，则将最小值设为 -1
			float minEggsProduced = -1;

			var ovipositors = parts.Select(part => part.TryGetComp<HediffComp_Ovipositor>()).Where(p => p != null);
			foreach (var ovi in ovipositors)
			{
				//if (hed.minEggAmount)
				if (minEggsProduced < 0)
				{
					minEggsProduced = ovi.Props.eggCount.min;
				}
			}
			return (int)minEggsProduced;
		}

		public static int max_EggsProduced(Pawn pawn, List<Hediff> parts = null)
		{
			parts ??= pawn.GetGenitalsList();
			// 没有合适的部件 - 返回 0
			if (parts.NullOrEmpty()) return 0;

			// 如果 xml 文件中未定义，则将最小值设为 -1
			float maxEggsProduced = -1;

			var ovipositorComps = parts.Select(part => part.TryGetComp<HediffComp_Ovipositor>()).Where(p => p != null);
			foreach (var ovi in ovipositorComps)
			{
				if (maxEggsProduced < 0)
				{
					maxEggsProduced = ovi.Props.eggCount.max;
				}
			}
			return (int)maxEggsProduced;
		}

		/// <summary>
		/// <para>此工具出现在调试菜单的“输出”标签页中。</para>
		/// <para>它会生成游戏中每一种小人，并对它们运行所有这些方法。
		/// 你可以用它来验证对这些工具的改动。</para>
		/// <para>只需在改动前运行一次，改动后再运行一次，然后用某种 diff
		/// 工具看看有没有……不一样的地方。</para>
		/// <para>请注意，有时小人生成得并不完美，
		/// 比如一条尾巴刚被切断的鬣蜥会改变 `has_tail`
		/// 生成其输出的方式。</para>
		/// </summary>
		[DebugOutput("RJW", onlyWhenPlaying = true)]
		public static void GenitalsOfAllPawnKinds()
		{
			var artificialParts = DefDatabase<RecipeDef>.AllDefsListForReading
				.Where((r) => r.workerClass == typeof(Recipe_InstallGenitals))
				.Where((r) => r.addsHediff is HediffDef_SexPart && !r.addsHediff.organicAddedBodypart)
				.ToArray();

			var dataSources = DefDatabase<PawnKindDef>.AllDefsListForReading
				.Where((d) => d.race != null)
				.OrderBy((d) => d.defName)
				.SelectMany(CreatePawns)
				.ToArray();

			var table = new TableDataGetter<(Pawn pawn, string label)>[]
			{
				new("kind", (d) => d.pawn.kindDef.defName),
				new("sex", (d) => GenderHelper.GetSex(d.pawn).ToString().CapitalizeFirst()),
				new("gender", (d) => d.pawn.gender),
				new("label", (d) => d.label),
				new("genitalsBPR", (d) => get_genitalsBPR(d.pawn)?.def.defName),
				new("breastsBPR", (d) => get_breastsBPR(d.pawn)?.def.defName),
				new("mouthBPR", (d) => get_mouthBPR(d.pawn)?.def.defName),
				new("tongueBPR", (d) => get_tongueBPR(d.pawn)?.def.defName),
				new("stomachBPR", (d) => get_stomachBPR(d.pawn)?.def.defName),
				new("tailBPR", (d) => get_tailBPR(d.pawn)?.def.defName),
				new("anusBPR", (d) => get_anusBPR(d.pawn)?.def.defName),
				new("torsoBPR", (d) => get_torsoBPR(d.pawn)?.def.defName),
				new("has_genitals", (d) => has_genitals(d.pawn)),
				new("has_breasts", (d) => has_breasts(d.pawn)),
				new("has_male_breasts", (d) => has_male_breasts(d.pawn)),
				new("can_do_breastjob", (d) => can_do_breastjob(d.pawn)),
				new("has_tail", (d) => has_tail(d.pawn)),
				new("has_mouth", (d) => has_mouth(d.pawn)),
				new("has_tongue", (d) => has_tongue(d.pawn)),
				new("has_anus", (d) => has_anus(d.pawn)),
				new("has_vagina", (d) => has_vagina(d.pawn)),
				new("has_male_bits", (d) => has_male_bits(d.pawn)),
				new("has_penis_fertile", (d) => has_penis_fertile(d.pawn)),
				new("has_penis_infertile", (d) => has_penis_infertile(d.pawn)),
				new("has_multipenis", (d) => has_multipenis(d.pawn)),
				new("has_ovipositorM", (d) => has_ovipositorM(d.pawn)),
				new("has_ovipositorF", (d) => has_ovipositorF(d.pawn)),
				new("is_futa", (d) => is_futa(d.pawn)),
				new("min_EggsProduced", (d) => min_EggsProduced(d.pawn)),
				new("max_EggsProduced", (d) => max_EggsProduced(d.pawn))
			};

			DebugTables.MakeTablesDialog(dataSources, table);

			IEnumerable<PawnGenerationRequest> CreateRequests(PawnKindDef def)
			{
				if (!def.RaceProps.hasGenders)
				{
					yield return new PawnGenerationRequest(kind: def)
						.RequestDefaults();
				}
				else
				{
					yield return new PawnGenerationRequest(kind: def, fixedGender: Gender.Male)
						.RequestDefaults();

					yield return new PawnGenerationRequest(kind: def, fixedGender: Gender.Female)
						.RequestDefaults();
				}
			}

			IEnumerable<(Pawn pawn, string label)> CreatePawns(PawnKindDef def)
			{
				foreach (var request in CreateRequests(def))
					if (TestHelper.GenerateSeededPawn(request) is { } pawn)
						yield return (pawn, "natural");

				// 只在殖民者身上测试人造部件和复杂性别。
				if (def.defName != "Colonist") yield break;

				// 生成有生育力和无生育力的扶她。
				foreach (var request in CreateRequests(def))
				{
					if (request.FixedGender == Gender.Female)
					{
						if (TestHelper.GenerateSeededPawn(request) is { } fertileFemale)
							if (TestHelper.MakeIntoFuta(fertileFemale))
								yield return (fertileFemale, "as fertile female futa");

						if (TestHelper.GenerateSeededPawn(request) is { } infertileFemale)
							if (TestHelper.MakeIntoFuta(infertileFemale, true))
								yield return (infertileFemale, "as infertile female futa");
					}
					else if (request.FixedGender == Gender.Male)
					{
						if (TestHelper.GenerateSeededPawn(request) is { } fertileMale)
							if (TestHelper.MakeIntoFuta(fertileMale))
								yield return (fertileMale, "as fertile male futa");

						if (TestHelper.GenerateSeededPawn(request) is { } infertileMale)
							if (TestHelper.MakeIntoFuta(infertileMale))
								yield return (infertileMale, "as infertile male futa");
					}
				}

				// 生成伪娘。
				foreach (var request in CreateRequests(def))
				{
					if (request.FixedGender != Gender.Male) continue;

					if (TestHelper.GenerateSeededPawn(request) is { } malePawn)
						if (TestHelper.MakeIntoTrap(malePawn))
							yield return (malePawn, "as trap");
				}

				// 测试人造部件。
				foreach (var recipe in artificialParts)
				{
					foreach (var request in CreateRequests(def))
					{
						var pawn = TestHelper.GenerateSeededPawn(request);
						if (pawn is null) continue;
						if (!TestHelper.ApplyPartToPawn(pawn, recipe)) continue;

						yield return (pawn, $"has {recipe.addsHediff.defName}");
					}
				}
			}
		}
	}

}
