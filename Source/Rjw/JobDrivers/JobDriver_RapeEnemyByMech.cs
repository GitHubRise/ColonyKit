#nullable disable

﻿using RimWorld;
using Verse;

namespace rjw
{
	internal class JobDriver_RapeEnemyByMech : JobDriver_RapeEnemy
	{
		public override bool CanUseThisJobForPawn(Pawn rapist)
		{
			var curJobDef = rapist.CurJob?.def;
			if (rapist.CurJob != null &&
				curJobDef != JobDefOf.LayDown &&
				curJobDef != JobDefOf.Wait_Wander &&
				curJobDef != JobDefOf.GotoWander)
				return false;

			return xxx.is_mechanoid(rapist);
		}

		public override float GetFuckability(Pawn rapist, Pawn target)
		{
			//把东西植入人形生物体内。
			if (xxx.is_human(target))
				return 1f;
			else
				return 0f;
		}

	}
}
