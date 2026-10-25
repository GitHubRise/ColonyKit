#nullable disable

using System.Text;
using Verse;
using RimWorld;
using Multiplayer.API;
using System.Collections.Generic;
using rjw.Modules.Shared.Logs;
using System.Runtime.InteropServices;
using System.Linq;
using System;

namespace rjw
{
	/// <summary>
	/// 用于物体的组件
	/// </summary>
	public class CompThingBodyPart : ThingComp
	{
		/// <summary>
		/// 用于 rjw 物体部件的组件。
		/// </summary>

		public HediffDef_SexPart hediffDef;

		public float? depth;
		public float? maxGirth;
		public float? length;
		public float? girth;
		public float? volume;
		public float? size;				// 用于标签，以及在没有其他尺寸可用时作为回退

		public float originalOwnerSize;
		public string originalOwnerRace;      //第一任拥有者的种族
		public string previousOwner;          //呃

		public SexFluidDef fluid;
		public float fluidAmount;		// 液体的绝对量，用于显示
		public float? partFluidMultiplier;	// 液体量的乘数，每个部件随机

		public bool initialised;

		public CompProperties_ThingBodyPart Props => (CompProperties_ThingBodyPart)props;

		/// <summary>
		/// 标签中的物体/部件尺寸
		/// </summary>
		
		private string _label = "";
		public override string TransformLabel(string label)
		{
			if (!initialised)
				Init();

			if (_label.NullOrEmpty())
			{
				var humanSize = CalculateSize(1.0f);
				_label = $"{label} ({hediffDef.GetStandardSizeLabel(humanSize)})";
			}

			return _label;
		}

		public override string CompInspectStringExtra()
		{
			return InspectStringLines().ToLineList();
		}

		private IEnumerable<string> InspectStringLines()
		{
			if (length.HasValue)
			{
				yield return "RJW_PartInfo_length".Translate(length.Value.ToString("F1"));
			}
			if (girth.HasValue)
			{
				yield return "RJW_PartInfo_girth".Translate(girth.Value.ToString("F1"));
			}
			if (depth.HasValue && hediffDef.genitalFamily != GenitalFamily.FemaleOvipositor)
			{
				yield return "RJW_PartInfo_depth".Translate(depth.Value.ToString("F1"));
			}
			if (maxGirth.HasValue && hediffDef.genitalFamily != GenitalFamily.FemaleOvipositor)
			{
				yield return "RJW_PartInfo_maxGirth".Translate(maxGirth.Value.ToString("F1"));
			}

			if (volume.HasValue)
			{
				if (PartSizeCalculator.Inversion.TryInvertBreastVolume(hediffDef, volume.Value, 1.0f, out float _, out BreastSize breastSize))
				{
					yield return "RJW_PartInfo_braCup".Translate(breastSize.GetCupSize());
					yield return "RJW_PartInfo_weight".Translate(breastSize.weight.ToString("F3"));
				}
			}

			if (fluid != null)
			{
				yield return "RJW_PartInfo_fluidTypeFluidAmountHeading".Translate(fluid.label, fluidAmount.ToString("F0")).CapitalizeFirst();
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Defs.Look(ref hediffDef, "hediffDef");
			Scribe_Defs.Look(ref fluid, "fluid");
			Scribe_Values.Look(ref size, "size");
			Scribe_Values.Look(ref originalOwnerSize, "originalOwnerSize");
			Scribe_Values.Look(ref originalOwnerRace, "originalOwnerRace");
			Scribe_Values.Look(ref previousOwner, "previousOwner");
			Scribe_Values.Look(ref fluidAmount, "fluidAmount");
			Scribe_Values.Look(ref partFluidMultiplier, "partFluidMultiplier");
			Scribe_Values.Look(ref initialised, "initialised", defaultValue: true);
			Scribe_Values.Look(ref length, "length");
			Scribe_Values.Look(ref depth, "depth");
			Scribe_Values.Look(ref maxGirth, "maxGirth");
			Scribe_Values.Look(ref girth, "girth");
			Scribe_Values.Look(ref volume, "volume");
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
		{
			// 绝对是个糟糕的主意，但这个检查原本位于描述重写方法中，作为某种保险措施，
			// 所以暂时放在这里，以免出问题
			// TODO: 确保它可以被移除
			if (!initialised)
			{
				Init();
			}

			// 半随意选定。如果属性排序看起来不对劲就改这些值。
			int statOrder = StatDisplayOrder.Thing_BodyPartEfficiency + 10;
			var implantCategory = StatCategoryDefOf.Implant;
			var ownerCategory = RJWStatCategoryDefOf.OriginalPartOwner;

			//yield return new StatDrawEntry(category, "RJW_StatEntry_PartSize".Translate(), size.ToString("F2"), "", statOrder++);

			if (fluid != null)
			{
				yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_Fluid".Translate(), fluid.LabelCap, "", statOrder++);
			}

			if (fluidAmount != 0)
			{
				string fluidAmountKey = hediffDef.genitalFamily switch
				{
					GenitalFamily.Penis or GenitalFamily.MaleOvipositor => "RJW_StatEntry_FluidAmount_ejaculation",
					GenitalFamily.Vagina or GenitalFamily.Anus => "RJW_StatEntry_FluidAmount_wetness",
					_ => "RJW_StatEntry_FluidAmount"
				};

				yield return new StatDrawEntry(implantCategory, fluidAmountKey.Translate(), fluidAmount.ToString("F2"), "", statOrder++);
			}

			if (length.HasValue)
			{
				yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartLength".Translate(), length.Value.ToString("F2"), "", statOrder++);
			}

			if (girth.HasValue)
			{
				yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartGirth".Translate(), girth.Value.ToString("F2"), "", statOrder++);
			}

			if (depth.HasValue)
			{
				yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartDepth".Translate(), depth.Value.ToString("F2"), "", statOrder++);
			}

			if (maxGirth.HasValue)
			{
				yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartMaxGirth".Translate(), maxGirth.Value.ToString("F2"), "", statOrder++);
			}

			if (volume.HasValue)
			{
				if (PartSizeCalculator.Inversion.TryInvertBreastVolume(hediffDef, volume.Value, 1.0f, out float _, out BreastSize breastSize))
				{
					yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartCupSize".Translate(), breastSize.GetCupSize(), "", statOrder++);
					yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartWeight".Translate(), breastSize.weight.ToString("F3"), "", statOrder++);
				}
			}


			if (!hediffDef.partTags.NullOrEmpty())
			{
				var tags = hediffDef.partTags;
				yield return new StatDrawEntry(implantCategory, "RJW_StatEntry_PartTags".Translate(), tags.ToCommaList(), "", statOrder++);
			}

			// 所有权信息
			if (!previousOwner.NullOrEmpty())
			{
				yield return new StatDrawEntry(ownerCategory, "RJW_StatEntry_PreviousOwner".Translate(), previousOwner, "", 1);
			}
			if (!originalOwnerRace.NullOrEmpty())
			{
				yield return new StatDrawEntry(ownerCategory, "RJW_StatEntry_OriginalOwnerRace".Translate(), originalOwnerRace, "", 2);
			}
			if (originalOwnerSize > 0f)
			{
				yield return new StatDrawEntry(ownerCategory, "RJW_StatEntry_OriginalOwnerSize".Translate(), originalOwnerSize.ToString("F1"), "", 3);
			}
		}

		public override void PostPostMake()
		{
			base.PostPostMake();
			Init();
		}

		/// <summary>
		/// 填充组件数据
		/// </summary>
		[SyncMethod]
		private void Init(Pawn pawn = null)
		{
			// TODO: 也许该让它与 ThingDef xml 解耦，改为随机设置或继承自被移除的部件
			hediffDef = Props.hediffDef;

			bool originalOwnerIsKnown = true;

			if (pawn == null)
			{
				InitFromDef(hediffDef);
				initialised = true;
			}
			else
			{
				Hediff hd = HediffMaker.MakeHediff(hediffDef, pawn);
	
				HediffComp_SexPart hediffComp = hd.TryGetComp<HediffComp_SexPart>();
				if (hediffComp != null)
				{
					InitFromComp(hediffComp, originalOwnerIsKnown);
				}
			}
		}

		public bool HasSize() {
			return length.HasValue || girth.HasValue || depth.HasValue || maxGirth.HasValue || volume.HasValue || size.HasValue;
		}

		public float CalculateSize(float newBodySize)
		{
			// 尝试反推我们的测量值，以获得适合新体型的严重度
			if(length.HasValue)
			{
				if(PartSizeCalculator.Inversion.TryInvertLength(hediffDef, length.Value, newBodySize, out float newSize))
				{
					return newSize;
				}
			}
			if(girth.HasValue)
			{
				if(PartSizeCalculator.Inversion.TryInvertGirth(hediffDef, girth.Value, newBodySize, out float newSize))
				{
					return newSize;
				}
			}
			if(depth.HasValue)
			{
				if(PartSizeCalculator.Inversion.TryInvertLength(hediffDef, depth.Value, newBodySize, out float newSize))
				{
					return newSize;
				}
			}
			if(maxGirth.HasValue)
			{
				if(PartSizeCalculator.Inversion.TryInvertGirth(hediffDef, maxGirth.Value, newBodySize, out float newSize))
				{
					return newSize;
				}
			}
			if(volume.HasValue)
			{
				if(PartSizeCalculator.Inversion.TryInvertBreastVolume(hediffDef, volume.Value, newBodySize, out float newSize, out BreastSize _))
				{
					return newSize;
				}
			}

			if(size.HasValue)
			{
				// 若无其他可用信息则回退到尺寸
				return size.Value;
			}

			// 确实什么都没有，随机尺寸
			return hediffDef.GetRandomSize();
		}


		public void InitFromComp(HediffComp_SexPart hediffComp, bool recordOriginalOwner = true)
		{
			hediffDef = hediffComp.Def;
			fluid = hediffComp.Fluid;
			fluidAmount = hediffComp.FluidAmount;
			partFluidMultiplier = hediffComp.partFluidMultiplier;
			size = hediffComp.GetSeverity();

			if (!hediffDef.genitalTags.NullOrEmpty())
			{
				if (hediffDef.genitalTags.Contains(GenitalTag.CanPenetrate))
				{
					if (PartSizeCalculator.TryGetLength(hediffComp.parent, out float length))
					{
						this.length = length;
					}
					if (PartSizeCalculator.TryGetGirth(hediffComp.parent, out float girth))
					{
						this.girth = girth;
					}
				}
				if (hediffDef.genitalTags.Contains(GenitalTag.CanBePenetrated))
				{
					if (PartSizeCalculator.TryGetLength(hediffComp.parent, out float depth))
					{
						this.depth = depth;
					}
					if (PartSizeCalculator.TryGetGirth(hediffComp.parent, out float maxGirth))
					{
						this.maxGirth = maxGirth;
					}
				}
			}
			if (PartSizeCalculator.TryGetBreastSize(hediffComp.parent, out BreastSize breastSize))
			{
				this.volume = breastSize.volume;
			}

			if (recordOriginalOwner)
			{
				originalOwnerSize = hediffComp.originalOwnerSize;
				previousOwner = hediffComp.Pawn?.LabelNoCount;
				if (hediffComp.Pawn.IsHumanLike() || hediffComp.Pawn.Name != null)
				{
					originalOwnerRace = hediffComp.Pawn?.kindDef.race.LabelCap;
				}
			}
			initialised = true;

		}

		[SyncMethod]
		private void InitFromDef(HediffDef_SexPart hediffDef)
		{
			this.hediffDef = hediffDef;
			var size = hediffDef.GetRandomSize();
			var bodySize = 1.0f;

			fluid = hediffDef.fluid;
			partFluidMultiplier = hediffDef.GetRandomPartFluidMultiplier();
			var totalFluidMultiplier = hediffDef.GetFluidMultiplier(size, partFluidMultiplier.Value, bodySize);
			fluidAmount = hediffDef.GetFluidAmount(totalFluidMultiplier);

			if (!hediffDef.genitalTags.NullOrEmpty())
			{
				if (hediffDef.genitalTags.Contains(GenitalTag.CanPenetrate))
				{
					if (PartSizeCalculator.Internal.TryGetLength(hediffDef, size, bodySize, out float length))
					{
						this.length = length;
					}
					if (PartSizeCalculator.Internal.TryGetGirth(hediffDef, size, bodySize, out float girth))
					{
						this.girth = girth;
					}
				}
				if (hediffDef.genitalTags.Contains(GenitalTag.CanBePenetrated))
				{
					if (PartSizeCalculator.Internal.TryGetLength(hediffDef, size, bodySize, out float depth))
					{
						this.depth = depth;
					}
					if (PartSizeCalculator.Internal.TryGetGirth(hediffDef, size, bodySize, out float maxGirth))
					{
						this.maxGirth = maxGirth;
					}
				}
			}
			if (PartSizeCalculator.Internal.TryGetBreastSize(hediffDef, size, bodySize, out BreastSize breastSize))
			{
				this.volume = breastSize.volume;
			}

			this.size = size;
		}
	}
}
