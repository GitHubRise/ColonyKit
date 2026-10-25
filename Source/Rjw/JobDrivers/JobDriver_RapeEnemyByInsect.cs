#nullable disable

using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	internal class JobDriver_RapeEnemyByInsect : JobDriver_RapeEnemy
	{
		public override bool CanUseThisJobForPawn(Pawn rapist)
		{
			var curJobDef = rapist.CurJob?.def;
			if (rapist.CurJob != null &&
				curJobDef != JobDefOf.LayDown &&
				curJobDef != JobDefOf.Wait_Wander &&
				curJobDef != JobDefOf.GotoWander)
				return false; ;

			return xxx.is_insect(rapist);
		}

		public override float GetFuckability(Pawn rapist, Pawn target)
		{
			//雌性向所有人产卵。
			//if (rapist.gender == Gender.Female) //Genital_Helper.has_ovipositorF(rapist);
			//{
			//	//仅在目标尚无卵时强暴
			//	//if ((from x in target.health.hediffSet.GetHediffs<Hediff_InsectEgg>() where (x.IsParent(rapist)) select x).Count() > 0)
			//	{
			//		return 1f;
			//	}
			//}
			////雄性强暴所有人。
			////使已植入目标体内的卵受精。
			//else //Genital_Helper.has_ovipositorM(rapist);
			//{
			//	//仅在能受精时强暴目标
			//	//if ((from x in target.health.hediffSet.GetHediffs<Hediff_InsectEgg>() where (x.IsParent(rapist) && !x.fertilized) select x).Count() > 0)
			//	if ((from x in target.health.hediffSet.GetHediffs<Hediff_InsectEgg>() where x.IsParent(rapist) select x).Count() > 0)
			//	{
			//		return 1f;
			//	}
			//}
			return 1f;
		}
	}
}
