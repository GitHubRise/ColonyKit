#nullable disable

using RimWorld;
using Verse;

namespace rjw
{
	internal class JobDriver_RapeEnemyByHumanlike : JobDriver_RapeEnemy
	{
		public override bool CanUseThisJobForPawn(Pawn rapist)
		{
			var curJobDef = rapist.CurJob?.def;
			if (rapist.CurJob != null &&
				curJobDef != JobDefOf.LayDown &&
				curJobDef != JobDefOf.Wait_Wander &&
				curJobDef != JobDefOf.GotoWander)
				return false;

			return xxx.is_human(rapist);
		}
	}
}
