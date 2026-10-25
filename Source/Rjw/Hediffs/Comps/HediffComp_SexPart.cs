#nullable disable

using Verse;
using RimWorld;
using Multiplayer.API;
using System.Collections.Generic;
using System.Linq;

using ShowParts = rjw.RJWSettings.ShowParts;
using System.Text;

namespace rjw
{
	/// <summary>
	/// rjw 状态部位的组件
	/// </summary>
	public class HediffComp_SexPart : HediffCompDesc
	{
		public string SizeLabel => parent.CurStage?.label ?? "";

		public string originalOwnerRace = "";      //部位创建时的基础种族
		public string previousOwner = "";            //部位创建时的基础种族

		private float? forcedSize;
		public float baseSize;                                  // 部位创建时的绝对基础大小，将来可通过手术改变
		public float originalOwnerSize = 0;                         // 该部位首位拥有者的体型。不参与计算。
		public bool isTransplant = false;                   //移植自他人

		// 根据模组设置和服装，该部位是否可能曾被玩家看到。
		public bool discovered;

		private bool initialised;

		public new HediffDef_SexPart Def => (HediffDef_SexPart)base.Def;

		public HediffCompProperties_SexPart Props => (HediffCompProperties_SexPart)props;


		public float partFluidMultiplier = -1;            // 乳汁/射精/湿润度的乘数
		private SexFluidDef fluidOverride;           // 精液/乳汁——虫胶/蜂蜜等
		public float FluidAmount => GetFluidAmount();
		public float FluidMultiplier => GetFluidMultiplier();
		public SexFluidDef Fluid
		{
			get => fluidOverride ?? Def.fluid;
			set
			{
				if (value == Def.fluid)
				{
					fluidOverride = null;
				}
				else
				{
					fluidOverride = value;
				}
			}
		}
		public float GetFluidMultiplier()
		{
			return Def.GetFluidMultiplier(GetSeverity(), partFluidMultiplier, GetBodySize(), SexUtility.ScaleToHumanAge(Pawn));
		}

		public float GetFluidAmount()
		{
			return Def.GetFluidAmount(GetFluidMultiplier());
		}


		private List<string> DefaultPositionNames
		{
			get
			{
				List<string> result = Def.defaultBodyPartList ?? new();
				string singleDefaultPart = Def.defaultBodyPart;
				if (!singleDefaultPart.NullOrEmpty() && !result.Contains(singleDefaultPart))
				{
					result.Add(singleDefaultPart);
				}
				return result;
			}
		}

		public float Size
		{
			get => GetSize();
		}

		// 使部位看起来随小人成长（即青春期）
		public bool HasForcedSize => forcedSize.HasValue;
		public float GetSize()
		{
			if (forcedSize.HasValue)
			{
				return forcedSize.Value;
			}
			return baseSize;
		}
		public float GetSeverity()
		{
			// Severity = baseSize * LifeStageFactor / Pawn.BodySize;
			return GetSize() / Pawn.BodySize;
		}
		public void SetSeverity(float newSeverity, bool sync = true)
		{
			if (!forcedSize.HasValue)
			{
				baseSize = newSeverity * Pawn.BodySize;
			}
			if (sync)
			{
				SyncSeverity();
			}
		}

		public void SyncSeverity()
		{
			parent.Severity = GetSeverity();
		}
		public void UpdateSeverity(float newSeverity = -1f)
		{
			if (newSeverity < 0)
			{
				SyncSeverity();
			}
			else
			{
				SetSeverity(newSeverity);
			}
		}
		public void ForceSize(float size)
		{
			forcedSize = size;
			UpdateSeverity();
		}
		public void UnforceSize()
		{
			forcedSize = null;
			UpdateSeverity();
		}
		public float GetBodySize()
		{
			return Pawn.BodySize;
		}

		/// <summary>
		/// 保存数据
		/// </summary>
		public override void CompExposeData()
		{
			base.CompExposeData();

			Scribe_Values.Look(ref baseSize, nameof(baseSize));
			Scribe_Values.Look(ref originalOwnerSize, nameof(originalOwnerSize));
			Scribe_Values.Look(ref originalOwnerRace, nameof(originalOwnerRace));
			Scribe_Values.Look(ref previousOwner, nameof(previousOwner));
			Scribe_Defs.Look(ref fluidOverride, nameof(fluidOverride));
			Scribe_Values.Look(ref partFluidMultiplier, nameof(partFluidMultiplier), defaultValue: Def.GetRandomPartFluidMultiplier());
			Scribe_Values.Look(ref isTransplant, nameof(isTransplant));
			Scribe_Values.Look(ref forcedSize, nameof(forcedSize));
			Scribe_Values.Look(ref discovered, nameof(discovered));
			Scribe_Values.Look(ref initialised, nameof(initialised), defaultValue: true);
		}

		public override void CompPostPostAdd(DamageInfo? dinfo)
		{
			base.CompPostPostAdd(dinfo);
			try//世界生成时出错
			{
				Pawn.GetRJWPawnData().genitals = new List<Hediff>();
				Pawn.GetRJWPawnData().breasts = new List<Hediff>();
				Pawn.GetRJWPawnData().anus = new List<Hediff>();
			}
			catch
			{
			}
			if (!initialised)
			{
				//ModLog.Message("CompPostPostAdd");
				Init(reroll: false);
			}
		}

		// 可用于替代 SexPartAdder::PartMaker、RacePartDef_Helper::PartMaker
		//public override void CompPostMake()
		//{
		//	base.CompPostMake();
		//	// 原版中只有与 verb 相关的调试输出才会出现 Pawn 为 null 的情况，但小心为上。
		//	if (Pawn != null)	
		//	{
		//		if (!initialised)
		//		{
		//			ModLog.Message("CompPostMake");
		//			Init();
		//		}
		//	}
		//}

		public override void CompPostPostRemoved()
		{
			base.CompPostPostRemoved();
			try
			{
				Pawn.GetRJWPawnData().genitals = new List<Hediff>();
				Pawn.GetRJWPawnData().breasts = new List<Hediff>();
				Pawn.GetRJWPawnData().anus = new List<Hediff>();
			}
			catch
			{
			}
		}

		public override bool CompDisallowVisible()
		{
			if (base.CompDisallowVisible())
			{
				return true;
			}

			if (RJWSettings.ShowRjwParts == ShowParts.Hide)
			{
				return true;
			}

			if (discovered)
			{
				return false;
			}

			// 若设置要求则显示，或在开发者模式下于游戏开始时显示
			if (RJWSettings.ShowRjwParts != ShowParts.Known || (Current.ProgramState != ProgramState.Playing && Prefs.DevMode))
			{
				discovered = true;
				return false;
			}

			// 对主角和无衣物的小人显示
			if (Pawn.IsDesignatedHero() && Pawn.IsHeroOwner() || Pawn.apparel == null)
			{
				discovered = true;
			}
			else
			{
				var partGroups = parent.Part.groups;
				if (!partGroups.Any(group => Pawn.apparel.BodyPartGroupIsCovered(group)))
				{
					discovered = true;
				}
			}
			return !discovered;
		}

		private static readonly StringBuilder sb = new();

		/// <summary>
		/// 在健康标签页中显示部位信息
		/// </summary>
		public override string CompTipStringExtra
		{
			get
			{
				if (!initialised)
				{
					ModLog.Warning(" " + xxx.get_pawnname(Pawn) + " part " + parent.def.defName + " is broken, resetting");
					Init();
					UpdateSeverity();
					updatepartposition();
				}
				// 理论上提示应是精简形式，较长的形式应放在别处，但放哪儿呢？
				sb.Clear();
				IEnumerable<string> tipLines = GetTipString(RJWSettings.ShowRjwParts == RJWSettings.ShowParts.Extended);
				foreach (string line in tipLines)
				{
					sb.AppendLine(line);
				}
				return sb.ToString();
			}
		}

		public IEnumerable<string> GetTipString(bool extended)
		{
			//ModLog.Message(" CompTipStringExtra " + xxx.get_pawnname(Pawn) + " " + parent.def.defName);
			// TODO：部位类型应是扩展中的属性。
			//StringBuilder defstringBuilder = new StringBuilder();
			if (parent.Part != null)
			{
				if (Def.genitalFamily == GenitalFamily.Breasts)
				{
					return GetBreastTip(extended);
				}
				else if (Def.genitalFamily == GenitalFamily.Penis)
				{
					return GetPenisTip(extended);
				}
				else if (Def.genitalFamily == GenitalFamily.Anus || Def.genitalFamily == GenitalFamily.Vagina)
				{
					return GetOrificeTip(extended);
				}
				else if (Def.genitalFamily == GenitalFamily.FemaleOvipositor || Def.genitalFamily == GenitalFamily.MaleOvipositor)
				{
					return GetEggTip();
				}
			}

			return Enumerable.Empty<string>();
		}

		private string _eggTip = null;
		public static string FormatLengthTip(float length) => "RJW_PartInfo_length".Translate(length.ToString("F1"));
		public static string FormatGirthTip(float girth) => "RJW_PartInfo_girth".Translate(girth.ToString("F1"));
		public static string FormatMaxLengthTip(float length) => "RJW_PartInfo_maxLength".Translate(length.ToString("F1"));
		public static string FormatMaxGirthTip(float girth) => "RJW_PartInfo_maxGirth".Translate(girth.ToString("F1"));
		public static string FormatWeightTip(float weight) => "RJW_PartInfo_weight".Translate(weight.ToString("F1"));
		public static string FormatBreastWeightTip(float weight) => "RJW_PartInfo_weight".Translate(weight.ToString("F3"));
		public static string FormatBraCupTip(string cup) => "RJW_PartInfo_braCup".Translate(cup);
		public static string FormatBraSizeTip(string band, string cup) => "RJW_PartInfo_braSize".Translate(band, cup);
		public static string FormatFluidTip(string fluidLabel, float amount) => "RJW_PartInfo_fluidTypeFluidAmount".Translate(fluidLabel, amount.ToString("F1"));
		public static string FormatPropertiesTip(string tagList) => "RJW_PartInfo_properties".Translate(tagList);

		public enum PartsPreviewKind
		{
			Penis,
			Breasts,
			Vagina,
			Anus
		}

		// 预览平均部位时固定使用的样本。显示的每个数值都
		// 通过下方真实的尺寸/液体计算从真实定义中求得，因此
		// 预览内容与真实部位的提示信息相符。
		private const float PreviewSeverity = 0.5f;
		private const float PreviewBodySize = 1.0f;
		private const float PreviewFluidMultiplier = 1.0f;
		private const float PreviewAge = 40f;
		private const float MinSeverityForCupDisplay = 0.02f;

		private static bool previewSamplesResolved;
		private static HediffDef_SexPart penisSampleDef;
		private static HediffDef_SexPart breastsSampleDef;
		private static HediffDef_SexPart vaginaSampleDef;
		private static HediffDef_SexPart anusSampleDef;

		private static HediffDef_SexPart SampleDef(PartsPreviewKind kind)
		{
			if (!previewSamplesResolved)
			{
				penisSampleDef = FindSampleDef("Penis", GenitalFamily.Penis);
				breastsSampleDef = FindSampleDef("Breasts", GenitalFamily.Breasts);
				vaginaSampleDef = FindSampleDef("Vagina", GenitalFamily.Vagina);
				anusSampleDef = FindSampleDef("Anus", GenitalFamily.Anus);
				previewSamplesResolved = true;
			}
			return kind switch
			{
				PartsPreviewKind.Penis => penisSampleDef,
				PartsPreviewKind.Breasts => breastsSampleDef,
				PartsPreviewKind.Vagina => vaginaSampleDef,
				PartsPreviewKind.Anus => anusSampleDef,
				_ => penisSampleDef,
			};
		}

		private static HediffDef_SexPart FindSampleDef(string defName, GenitalFamily family)
		{
			return DefDatabase<HediffDef_SexPart>.GetNamedSilentFail(defName)
				?? DefDatabase<HediffDef_SexPart>.AllDefs.FirstOrDefault(d => d.genitalFamily == family);
		}

		private static string GetSamplePropertyTagList(HediffDef_SexPart def)
		{
			if (def == null || !def.partTags.Any())
			{
				return null;
			}
			var tags = new List<string>();
			def.partTags.ForEach(tag => tags.Add(("PartsTag_" + tag).Translate()));
			return tags.ToCommaList();
		}

		private static (bool hasFluid, string fluidLabel, float? fluidAmount, float totalMultiplier) SampleFluid(HediffDef_SexPart def)
		{
			if (def == null)
			{
				return (false, null, null, 0f);
			}
			float total = def.GetFluidMultiplier(PreviewSeverity, PreviewFluidMultiplier, PreviewBodySize, PreviewAge);
			float amount = def.GetFluidAmount(total);
			if (def.fluid == null || amount == 0f)
			{
				return (false, null, null, total);
			}
			return (true, def.fluid.LabelCap, amount, total);
		}

		private static string FormatFluidMultiplierTip(float totalMultiplier, float partMultiplier)
		{
			return "RJW_PartInfo_fluidMultiplier".Translate($"{totalMultiplier:F3} ({partMultiplier:F3})");
		}

		private static IEnumerable<string> BuildPenisLines(float? length, float? girth, float? weight, string fluidLabel, float? fluidAmount, string propertyTagList, bool extended)
		{
			if (length.HasValue)
			{
				yield return FormatLengthTip(length.Value);
			}
			if (!extended)
			{
				yield break;
			}
			if (girth.HasValue)
			{
				yield return FormatGirthTip(girth.Value);
			}
			if (weight.HasValue)
			{
				yield return FormatWeightTip(weight.Value);
			}
			if (!string.IsNullOrEmpty(fluidLabel) && fluidAmount.HasValue)
			{
				yield return FormatFluidTip(fluidLabel, fluidAmount.Value);
			}
			if (!string.IsNullOrEmpty(propertyTagList))
			{
				yield return FormatPropertiesTip(propertyTagList);
			}
		}

		private static IEnumerable<string> BuildBreastLines(string cup, string band, float? breastWeight, string fluidLabel, float? fluidAmount, string propertyTagList, bool extended)
		{
			if (!extended)
			{
				if (!string.IsNullOrEmpty(cup))
				{
					yield return FormatBraCupTip(cup);
				}
				yield break;
			}
			if (!string.IsNullOrEmpty(band) && !string.IsNullOrEmpty(cup))
			{
				yield return FormatBraSizeTip(band, cup);
			}
			if (breastWeight.HasValue)
			{
				yield return FormatBreastWeightTip(breastWeight.Value);
			}
			if (!string.IsNullOrEmpty(fluidLabel) && fluidAmount.HasValue)
			{
				yield return FormatFluidTip(fluidLabel, fluidAmount.Value);
			}
			if (!string.IsNullOrEmpty(propertyTagList))
			{
				yield return FormatPropertiesTip(propertyTagList);
			}
		}

		private static IEnumerable<string> BuildOrificeLines(float? maxLength, float? maxGirth, string fluidLabel, float? fluidAmount, string propertyTagList, bool extended)
		{
			if (!extended)
			{
				yield break;
			}
			if (maxLength.HasValue)
			{
				yield return FormatMaxLengthTip(maxLength.Value);
			}
			if (maxGirth.HasValue)
			{
				yield return FormatMaxGirthTip(maxGirth.Value);
			}
			if (!string.IsNullOrEmpty(fluidLabel) && fluidAmount.HasValue)
			{
				yield return FormatFluidTip(fluidLabel, fluidAmount.Value);
			}
			if (!string.IsNullOrEmpty(propertyTagList))
			{
				yield return FormatPropertiesTip(propertyTagList);
			}
		}

		public static IEnumerable<string> GetPenisPreviewLines(bool extended)
		{
			HediffDef_SexPart def = SampleDef(PartsPreviewKind.Penis);
			float? length = null;
			float? girth = null;
			float? weight = null;
			if (def != null)
			{
				if (PartSizeCalculator.Internal.TryGetLength(def, PreviewSeverity, PreviewBodySize, out float lengthValue))
				{
					length = lengthValue;
				}
				if (PartSizeCalculator.Internal.TryGetGirth(def, PreviewSeverity, PreviewBodySize, out float girthValue))
				{
					girth = girthValue;
				}
				if (length.HasValue && girth.HasValue && PartSizeCalculator.Internal.TryGetPenisWeight(def, length.Value, girth.Value, out float weightValue))
				{
					weight = weightValue;
				}
			}
			var fluid = SampleFluid(def);
			foreach (var line in BuildPenisLines(length, girth, weight, fluid.fluidLabel, fluid.fluidAmount, GetSamplePropertyTagList(def), extended))
			{
				yield return line;
			}
			if (extended && RJWSettings.DevMode)
			{
				yield return FormatFluidMultiplierTip(fluid.totalMultiplier, PreviewFluidMultiplier);
			}
		}

		public static IEnumerable<string> GetBreastPreviewLines(bool extended)
		{
			HediffDef_SexPart def = SampleDef(PartsPreviewKind.Breasts);
			string cup = null;
			string band = null;
			float? breastWeight = null;
			if (def != null && PartSizeCalculator.Internal.TryGetBreastSize(def, PreviewSeverity, PreviewBodySize, out BreastSize breastSize))
			{
				if (!extended)
				{
					if (PreviewSeverity >= MinSeverityForCupDisplay)
					{
						cup = breastSize.GetCupSize();
					}
				}
				else
				{
					band = breastSize.GetBandSize();
					cup = breastSize.GetCupSize();
					breastWeight = breastSize.weight;
				}
			}
			var fluid = SampleFluid(def);
			foreach (var line in BuildBreastLines(cup, band, breastWeight, fluid.fluidLabel, fluid.fluidAmount, GetSamplePropertyTagList(def), extended))
			{
				yield return line;
			}
			if (extended && RJWSettings.DevMode)
			{
				yield return FormatFluidMultiplierTip(fluid.totalMultiplier, PreviewFluidMultiplier);
			}
		}

		public static IEnumerable<string> GetVaginaPreviewLines(bool extended)
		{
			return SampleOrificeLines(PartsPreviewKind.Vagina, extended);
		}

		public static IEnumerable<string> GetAnusPreviewLines(bool extended)
		{
			return SampleOrificeLines(PartsPreviewKind.Anus, extended);
		}

		private static IEnumerable<string> SampleOrificeLines(PartsPreviewKind kind, bool extended)
		{
			HediffDef_SexPart def = SampleDef(kind);
			float? maxLength = null;
			float? maxGirth = null;
			if (def != null)
			{
				if (PartSizeCalculator.Internal.TryGetLength(def, PreviewSeverity, PreviewBodySize, out float lengthValue))
				{
					maxLength = lengthValue;
				}
				if (PartSizeCalculator.Internal.TryGetGirth(def, PreviewSeverity, PreviewBodySize, out float girthValue))
				{
					maxGirth = girthValue;
				}
			}
			var fluid = SampleFluid(def);
			foreach (var line in BuildOrificeLines(maxLength, maxGirth, fluid.fluidLabel, fluid.fluidAmount, GetSamplePropertyTagList(def), extended))
			{
				yield return line;
			}
			if (extended && RJWSettings.DevMode)
			{
				yield return FormatFluidMultiplierTip(fluid.totalMultiplier, PreviewFluidMultiplier);
			}
		}

		private static string FormatPreviewBody(IEnumerable<string> lines)
		{
			var bodySb = new StringBuilder();
			foreach (var line in lines)
			{
				bodySb.AppendLine("  " + line);
			}
			return bodySb.ToString().TrimEnd();
		}

		private static string FormatPreviewSection(string titleKey, IEnumerable<string> lines)
		{
			return titleKey.Translate() + ":\n" + FormatPreviewBody(lines);
		}

		private static string PreviewTitleKey(PartsPreviewKind kind)
		{
			return kind switch
			{
				PartsPreviewKind.Penis => "RJW_PartInfo_T_Penis",
				PartsPreviewKind.Breasts => "RJW_PartInfo_T_Breast",
				PartsPreviewKind.Vagina => "RJW_PartInfo_T_Vagina",
				PartsPreviewKind.Anus => "RJW_PartInfo_T_Anus",
				_ => "RJW_PartInfo_T_Penis",
			};
		}

		private static IEnumerable<string> PreviewLines(bool extended, PartsPreviewKind kind)
		{
			return kind switch
			{
				PartsPreviewKind.Penis => GetPenisPreviewLines(extended),
				PartsPreviewKind.Breasts => GetBreastPreviewLines(extended),
				PartsPreviewKind.Vagina => GetVaginaPreviewLines(extended),
				PartsPreviewKind.Anus => GetAnusPreviewLines(extended),
				_ => GetPenisPreviewLines(extended),
			};
		}

		public static string GetSinglePartPreviewTitle(PartsPreviewKind kind)
		{
			return PreviewTitleKey(kind).Translate() + ":";
		}

		public static string GetSinglePartPreviewBody(ShowParts mode, PartsPreviewKind kind)
		{
			if (mode == ShowParts.Hide)
			{
				return "RJW_ShowParts_Preview_Hidden".Translate();
			}
			return FormatPreviewBody(PreviewLines(mode == ShowParts.Extended, kind));
		}

		public static string GetSinglePartPreview(ShowParts mode, PartsPreviewKind kind)
		{
			if (mode == ShowParts.Hide)
			{
				return "RJW_ShowParts_Preview_Hidden".Translate();
			}
			bool extended = mode == ShowParts.Extended;
			return FormatPreviewSection(PreviewTitleKey(kind), PreviewLines(extended, kind));
		}

		public static string GetPartsDisplayPreview(ShowParts mode)
		{
			if (mode == ShowParts.Hide)
			{
				return "RJW_ShowParts_Preview_Hidden".Translate();
			}
			return string.Join("\n",
				GetSinglePartPreview(mode, PartsPreviewKind.Penis),
				GetSinglePartPreview(mode, PartsPreviewKind.Breasts),
				GetSinglePartPreview(mode, PartsPreviewKind.Vagina),
				GetSinglePartPreview(mode, PartsPreviewKind.Anus));
		}
		IEnumerable<string> GetEggTip()
		{
			if (Def.genitalTags.Contains(GenitalTag.CanEgg))
			{
				if (_eggTip == null)
				{
					var possibleEggNames = HediffComp_Ovipositor.PossibleEggs(Pawn.kindDef?.defName)
						.SelectMany(eggDef => eggDef.childrenDefs)
						.Select(defName => DefDatabase<PawnKindDef>.GetNamedSilentFail(defName)?.LabelCap.ToString())
						.Where(label => label != null)
						.Distinct().ToList();

					_eggTip = possibleEggNames.ToCommaList();
				}
				yield return "RJW_PartInfo_eggs".Translate(_eggTip);
			}
		}

		private bool TryGetFluidData(out string fluidLabel, out float fluidAmount)
		{
			if (FluidAmount != 0 && Fluid != null)
			{
				fluidLabel = Fluid.LabelCap;
				fluidAmount = FluidAmount;
				return true;
			}
			fluidLabel = null;
			fluidAmount = 0f;
			return false;
		}

		private string GetFluidDevModeLine()
		{
			if (RJWSettings.DevMode)
			{
				return FormatFluidMultiplierTip(FluidMultiplier, partFluidMultiplier);
			}
			return null;
		}

		private string GetPropertyTagListRaw()
		{
			var showTransplant = isTransplant && parent is Hediff_NaturalSexPart;
			if (!Def.partTags.Any() && !showTransplant)
			{
				return null;
			}
			var tags = new List<string>();
			Def.partTags.ForEach(tag => tags.Add(("PartsTag_" + tag).Translate()));
			var tagList = tags.ToCommaList();
			if (showTransplant)
			{
				tagList = Def.partTags.Prepend("Transplant").ToCommaList();
			}
			return tagList;
		}

		IEnumerable<string> GetPenisTip(bool extended)
		{
			float? length = PartSizeCalculator.TryGetLength(parent, out float lengthValue) ? lengthValue : null;
			float? girth = PartSizeCalculator.TryGetGirth(parent, out float girthValue) ? girthValue : null;
			float? weight = PartSizeCalculator.TryGetPenisWeight(parent, out float weightValue) ? weightValue : null;
			bool hasFluid = TryGetFluidData(out string fluidLabel, out float fluidAmount);
			string propertyTagList = GetPropertyTagListRaw();

			foreach (var line in BuildPenisLines(length, girth, weight, hasFluid ? fluidLabel : null, hasFluid ? fluidAmount : null, propertyTagList, extended))
			{
				yield return line;
			}

			if (extended)
			{
				string devModeLine = GetFluidDevModeLine();
				if (devModeLine != null)
				{
					yield return devModeLine;
				}
			}
		}

		IEnumerable<string> GetOrificeTip(bool extended)
		{
			//defstringBuilder.Append(this.Def.description);
			//defstringBuilder.AppendLine("Cum: " + FluidType);
			//defstringBuilder.AppendLine("Ejaculation: " + (FluidAmmount * FluidModifier).ToString("F2") + "ml");

			// TODO：无论在 xml 中还是此处，都应缩放到比阴茎尺寸更大。
			float? maxLength = PartSizeCalculator.TryGetLength(parent, out float lengthValue) ? lengthValue : null;
			float? maxGirth = PartSizeCalculator.TryGetGirth(parent, out float girthValue) ? girthValue : null;
			bool hasFluid = TryGetFluidData(out string fluidLabel, out float fluidAmount);
			string propertyTagList = GetPropertyTagListRaw();

			foreach (var line in BuildOrificeLines(maxLength, maxGirth, hasFluid ? fluidLabel : null, hasFluid ? fluidAmount : null, propertyTagList, extended))
			{
				yield return line;
			}

			if (extended)
			{
				string devModeLine = GetFluidDevModeLine();
				if (devModeLine != null)
				{
					yield return devModeLine;
				}
			}
		}

		IEnumerable<string> GetBreastTip(bool extended)
		{
			bool hasFluid = TryGetFluidData(out string fluidLabel, out float fluidAmount);
			string propertyTagList = GetPropertyTagListRaw();
			string cup = null;
			string band = null;
			float? breastWeight = null;

			if (PartSizeCalculator.TryGetBreastSize(parent, out BreastSize breastSize))
			{
				if (!extended)
				{
					if (GetSeverity() >= MinSeverityForCupDisplay)
					{
						cup = breastSize.GetCupSize();
					}
				}
				else
				{
					band = breastSize.GetBandSize();
					cup = breastSize.GetCupSize();
					breastWeight = breastSize.weight;
				}
			}

			foreach (var line in BuildBreastLines(cup, band, breastWeight, hasFluid ? fluidLabel : null, hasFluid ? fluidAmount : null, propertyTagList, extended))
			{
				yield return line;
			}

			if (extended)
			{
				string devModeLine = GetFluidDevModeLine();
				if (devModeLine != null)
				{
					yield return devModeLine;
				}
			}
		}

		[SyncMethod]
		public void updatepartposition()
		{
			if (parent.def is not HediffDef_SexPart def)
			{
				return;
			}

			//ModLog.Message("0 " + (parent.Part == null));
			//ModLog.Message("1 " +parent.Part);
			//ModLog.Message("2 " +parent.Part.def);
			//ModLog.Message("3 " +parent.Part.def.defName);
			//ModLog.Message("4 " +partBase);
			//ModLog.Message("5 " +partBase.DefaultBodyPart);
			//ModLog.Message("pawn " + xxx.get_pawnname(Pawn) + ", part: " + parent.def.defName);
			if (parent.Part == null)
			{
				var bp = Pawn.RaceProps.body.AllParts.Find(x => DefaultPositionNames.Any(x.def.defName.Contains));
				if (bp != null)
				{
					ModLog.Warning(" " + xxx.get_pawnname(Pawn) + " part is null/wholebody? is in wrong BodyPart position, resetting to default: " + def.defaultBodyPart);
					parent.Part = bp;
					Pawn.GetRJWPawnData().genitals = new List<Hediff>();
					Pawn.GetRJWPawnData().breasts = new List<Hediff>();
					Pawn.GetRJWPawnData().anus = new List<Hediff>();
				}
				else
				{
					ModLog.Message(" " + xxx.get_pawnname(Pawn) + " default bodypart not found: " + def.defaultBodyPart + " -skip.");
				}
			}
			else if (def.defaultBodyPart != "" && !parent.Part.def.defName.Contains(def.defaultBodyPart))
			{
				//ModLog.Message("f");
				//ModLog.Message(partBase.DefaultBodyPartList.ToLineList());
				if (def.defaultBodyPartList.Any() && def.defaultBodyPartList.Any(parent.Part.def.defName.Contains))
				{
					return;
				}
				var bp = Pawn.RaceProps.body.AllParts.Find(x => x.def.defName.Contains(def.defaultBodyPart));
				if (bp != null)
				{
					ModLog.Warning(" " + xxx.get_pawnname(Pawn) + " part " + parent.def.defName + " is in wrong BodyPart position, resetting to default: " + def.defaultBodyPart);
					parent.Part = bp;
					Pawn.GetRJWPawnData().genitals = new List<Hediff>();
					Pawn.GetRJWPawnData().breasts = new List<Hediff>();
					Pawn.GetRJWPawnData().anus = new List<Hediff>();
				}
				else
				{
					ModLog.Message(" " + xxx.get_pawnname(Pawn) + " default bodypart not found: " + def.defaultBodyPart + " -skip.");
				}
				//if (pawn.IsColonist)
				//{
				//	ModLog.Message(xxx.get_pawnname(pawn) + " has broken hediffs, removing " + this.ToString());
				//	ModLog.Message(Part.ToString());
				//	ModLog.Message(bp.def.defName);
				//	ModLog.Message(partBase.DefaultBodyPart.ToString());
				//}
			}
		}

		/// <summary>
		/// 填充组件数据
		/// </summary>
		[SyncMethod]
		public void Init(Pawn pawn = null, bool reroll = true)
		{
			pawn ??= Pawn;

			if (originalOwnerSize == 0)
			{
				originalOwnerSize = pawn.BodySize;
			}

			if (reroll || baseSize == 0)
			{
				//var s = (parent is Hediff_NaturalSexPart) ? originalOwnerSize : Pawn.RaceProps.baseBodySize;
				baseSize = CalculateSize(pawn, originalOwnerSize);
			}

			if (reroll || partFluidMultiplier == -1)
			{
				partFluidMultiplier = Def.GetRandomPartFluidMultiplier();
			}

			if (originalOwnerRace.NullOrEmpty())
			{
				originalOwnerRace = pawn?.kindDef.race.LabelCap ?? "RJW_PartInfo_unknownSpecies".Translate();
			}
			previousOwner = pawn?.LabelNoCount ?? "RJW_PartInfo_unknownOwner".Translate();

			//ModLog.Message("1 "+originalOwnerSize);
			//ModLog.Message("2 " + baseSize);
			UpdateSeverity();
			//ModLog.Message("3 " + baseSize);
			initialised = true;
		}

		[SyncMethod]
		public float CalculateSize(Pawn pawn, float bodySize)
		{
			float relativeSize;
			if (Def.genitalFamily == GenitalFamily.Breasts && pawn?.gender == Gender.Male && !Rand.Chance(TrapChance(pawn)))
			{
				relativeSize = .01f;
			}
			else
			{
				relativeSize = Def.GetRandomSize();
			}
			return relativeSize * bodySize;
		}

		private static float TrapChance(Pawn pawn)
		{
			if (RJWSettings.MaleTrap && !xxx.is_animal(pawn))
			{
				if (xxx.is_nympho(pawn))
				{
					return RJWSettings.futa_nymph_chance;
				}
				if (pawn.Faction != null && (int)pawn.Faction.def.techLevel < 5)
				{
					return RJWSettings.futa_natives_chance;
				}
				return RJWSettings.futa_spacers_chance;
			}
			return 0f;
		}



		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{

			// 半随意选定的。如果属性排序看起来别扭，就改动这些。
			int statOrder = 5000;
			var genitalStatsCat = RJWStatCategoryDefOf.GenitalStats;
			var ownerCategory = RJWStatCategoryDefOf.OriginalPartOwner;

			if (parent.Part != null)
			{
				if (Def.genitalFamily == GenitalFamily.Breasts)
				{
					yield return new StatDrawEntry(genitalStatsCat, "RJW_PartInfo_T_Title".Translate(), "RJW_PartInfo_T_Breast".Translate(), "", statOrder--);

					if (PartSizeCalculator.TryGetBreastSize(parent, out BreastSize breastSize))
					{
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartCupSize".Translate(), $"{breastSize.GetBandSize()} - {breastSize.GetCupSize()}", "", statOrder--);
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartWeight".Translate(), breastSize.weight.ToString("F3"), "", statOrder--);
					}

				}
				else if (Def.genitalFamily == GenitalFamily.Penis)
				{
					yield return new StatDrawEntry(genitalStatsCat, "RJW_PartInfo_T_Title".Translate(), "RJW_PartInfo_T_Penis".Translate(), "", statOrder--);
					if (PartSizeCalculator.TryGetLength(parent, out float penisLength))
					{
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartLength".Translate(), penisLength.ToString("F1"), "", statOrder--);
					}

					if (PartSizeCalculator.TryGetGirth(parent, out float girth))
					{
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartGirth".Translate(), girth.ToString("F1"), "", statOrder--);
						//yield return "RJW_PartInfo_girth".Translate(girth.ToString("F1"));
					}

					if (PartSizeCalculator.TryGetPenisWeight(parent, out float weight))
					{
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartWeight".Translate(), weight.ToString("F1"), "", statOrder--);
						//yield return "RJW_PartInfo_weight".Translate(weight.ToString("F1"));
					}
				}
				else if (Def.genitalFamily == GenitalFamily.Anus)
				{
					yield return new StatDrawEntry(genitalStatsCat, "RJW_PartInfo_T_Title".Translate(), "RJW_PartInfo_T_Anus".Translate(), "", statOrder--);
					if (PartSizeCalculator.TryGetLength(parent, out float length))
					{
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartDepth".Translate(), length.ToString("F1"), "", statOrder--);
					}

					if (PartSizeCalculator.TryGetGirth(parent, out float girth))
					{

						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartGirth".Translate(), girth.ToString("F1"), "", statOrder--);
					}
				}
				else if (Def.genitalFamily == GenitalFamily.Vagina)
				{
					yield return new StatDrawEntry(genitalStatsCat, "RJW_PartInfo_T_Title".Translate(), "RJW_PartInfo_T_Vagina".Translate(), "", statOrder--);
					if (PartSizeCalculator.TryGetLength(parent, out float length))
					{
						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartDepth".Translate(), length.ToString("F1"), "", statOrder--);
					}

					if (PartSizeCalculator.TryGetGirth(parent, out float girth))
					{

						yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartGirth".Translate(), girth.ToString("F1"), "", statOrder--);
					}
				}
				else if (Def.genitalFamily == GenitalFamily.FemaleOvipositor || Def.genitalFamily == GenitalFamily.MaleOvipositor)
				{
					yield return new StatDrawEntry(genitalStatsCat, "RJW_PartInfo_T_Title".Translate(), "RJW_PartInfo_T_Ovipositor".Translate(), "", statOrder--);
					if (Def.genitalTags.Contains(GenitalTag.CanEgg))
					{
						var possibleEggNames = HediffComp_Ovipositor.PossibleEggs(Pawn.kindDef?.defName)
							.SelectMany(eggDef => eggDef.childrenDefs)
							.Select(defName => DefDatabase<PawnKindDef>.GetNamedSilentFail(defName)?.LabelCap.ToString())
							.Where(label => label != null)
							.Distinct().ToList();

						yield return new StatDrawEntry(genitalStatsCat, "RJW_PartInfo_EggTypes".Translate(), possibleEggNames.ToCommaList(), "", statOrder--);
					}
				}
			}

			/*
			if (PartSizeCalculator.TryGetLength(parent, out float length))
			{
				yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartSize".Translate(), length.ToString("F1"), "", statOrder--);
			}*/

			if (Fluid != null)
			{
				yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_Fluid".Translate(), Fluid.LabelCap, "", statOrder--);
			}

			if (Fluid != null && FluidAmount != 0)
			{
				string fluidAmountKey = Def.genitalFamily switch
				{
					GenitalFamily.Penis or GenitalFamily.MaleOvipositor => "RJW_StatEntry_FluidAmount_ejaculation",
					GenitalFamily.Vagina or GenitalFamily.Anus => "RJW_StatEntry_FluidAmount_wetness",
					_ => "RJW_StatEntry_FluidAmount"
				};

				yield return new StatDrawEntry(genitalStatsCat, fluidAmountKey.Translate(), FluidAmount.ToString("F2"), "", statOrder--);
			}



			if (!Def.partTags.NullOrEmpty())
			{
				var tags = Def.partTags;
				yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartTags".Translate(), tags.ToCommaList(), "", statOrder--);
			}
			else
			{
				yield return new StatDrawEntry(genitalStatsCat, "RJW_StatEntry_PartTags".Translate(), "None", "", statOrder--);
			}

			// 归属信息
			if (!previousOwner.NullOrEmpty())
			{
				yield return new StatDrawEntry(ownerCategory, "RJW_StatEntry_PreviousOwner".Translate(), previousOwner, "", 1);
			}
			if (!originalOwnerRace.NullOrEmpty())
			{
				yield return new StatDrawEntry(ownerCategory, "RJW_StatEntry_OriginalOwnerRace".Translate(), originalOwnerRace, "", 2);
			}
		}

	}
}
