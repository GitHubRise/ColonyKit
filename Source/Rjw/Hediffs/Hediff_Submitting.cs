#nullable disable

﻿using Verse;

//小人"躺下并顺从"按钮的状态工作类
namespace rjw
{
	public class Hediff_Submitting: HediffWithComps
	{
		public override bool ShouldRemove {
			get
			{
				Pawn daddy = pawn.CarriedBy;
				if (daddy != null && daddy.Faction == pawn.Faction)
				{
					return true;
				}
				else
					return base.ShouldRemove;
			}
		}
	}
}
