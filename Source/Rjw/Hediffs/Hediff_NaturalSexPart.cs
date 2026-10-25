#nullable disable

using Verse;
using RimWorld;

namespace rjw
{
	public class Hediff_NaturalSexPart : HediffWithCompDesc, ISexPartHediff
	{
		public override bool ShouldRemove => false;

		public HediffDef_SexPart Def => def as HediffDef_SexPart;

		public HediffWithComps AsHediff => this;

		public override float Severity { 
			get => base.Severity;
			set
			{
				var prevSeverity = base.Severity;
				base.Severity = value;
				ISexPartHediffExtensions.DetectExternalSeverityChange(this, prevSeverity, value);
			}
		}

		public override string LabelBase
		{
			get
			{
				/*
				 * make patch to make/save capmods?
				if (CapMods.Count < 5)
				{
					PawnCapacityModifier pawnCapacityModifier = new PawnCapacityModifier();
					pawnCapacityModifier.capacity = PawnCapacityDefOf.Moving;
					pawnCapacityModifier.offset += 0.5f;
					CapMods.Add(pawnCapacityModifier);
				}
				*/

				//名称/种类
				return def.label;
			}
		}

		/// <summary>
		/// 在健康标签页中堆叠状态？
		/// </summary>
		public override int UIGroupKey
		{
			get
			{
				return loadID;
			}
		}

		/// <summary>
		/// 不要将相同的 rjw 部件合并为一个
		/// </summary>
		public override bool TryMergeWith(Hediff other)
		{
			return false;
		}
	}
}
