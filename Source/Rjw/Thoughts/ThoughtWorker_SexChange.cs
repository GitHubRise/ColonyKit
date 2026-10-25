#nullable disable

﻿using RimWorld;
using Verse;

namespace rjw
{
	//RW 的这套想法系统烂得离谱。它需要为每个状态单独提供想法处理器。
	public abstract class ThoughtWorker_SexChange : ThoughtWorker
	{
		public virtual HediffDef hediff_served { get; }
		protected override ThoughtState CurrentStateInternal(Pawn pawn)
		{
			//ModLog.Message(" "+this.GetType() + " is called for " + pawn +" and hediff" + hediff_served);
			Hediff denial = pawn.health.hediffSet.GetFirstHediffOfDef(hediff_served);
			//ModLog.Message("Hediff of the class is null " + (hediff_served == null));
			if (denial != null && denial.CurStageIndex!=0)
			{
				//ModLog.Message("Current denial level is  " + denial.CurStageIndex );
				return ThoughtState.ActiveAtStage(denial.CurStageIndex-1);
			}
			return ThoughtState.Inactive;
		}
	}
	public class ThoughtWorker_MtT : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.m2t; } }
	}
	public class ThoughtWorker_MtF:ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.m2f; }  }
	}
	public class ThoughtWorker_MtH : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.m2h; } }
	}
	public class ThoughtWorker_FtT : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.f2t; } }
	}
	public class ThoughtWorker_FtM : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.f2m; } }
	}
	public class ThoughtWorker_FtH : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.f2h; } }
	}
	public class ThoughtWorker_HtT : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.h2t; } }
	}
	public class ThoughtWorker_HtM : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.h2m; } }
	}
	public class ThoughtWorker_HtF : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.h2f; } }
	}
	public class ThoughtWorker_TtH : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.t2h; } }
	}
	public class ThoughtWorker_TtM : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.t2m; } }
	}
	public class ThoughtWorker_TtF : ThoughtWorker_SexChange
	{
		public override HediffDef hediff_served { get { return GenderHelper.t2f; } }
	}
}
