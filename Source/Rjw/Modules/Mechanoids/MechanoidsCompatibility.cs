#nullable disable

﻿using System.Linq;
using Verse;

namespace rjw
{
	/// <summary>
	/// 辅助类：让机械族在模组配合下也具备性吸引力。
	/// </summary>
	public static class MechanoidsCompatibility
	{
		public static bool IsMechanoidSexy(Pawn pawn)
		{
			if (pawn == null) return false;
			//return pawn.GetCompRJW().orientation != Orientation.None;

			BodyPartRecord Part = Genital_Helper.get_BPR(pawn, xxx.mechgenitalsDef);
			if (Part == null) return false;
			//if (Part.def.conceptual) return false;
			return pawn.health.hediffSet.hediffs.Any((Hediff hed) => hed.Part == Part && hed is Hediff_ArtificialSexPart);
		}

		public static bool IsMechanoid(Pawn pawn)
		{
			return xxx.is_mechanoid(pawn);
		}

		//public static bool MechanoidPenisFertility(Pawn pawn)
		//{
		//	//仿生人只有使用远古科技部件才能生育
		//	BodyPartRecord Part = Genital_Helper.get_genitalsBPR(pawn);
		//	return (pawn.health.hediffSet.hediffs.Any((Hediff hed) =>
		//		(hed.Part == Part) &&
		//		(hed.def == Genital_Helper.archotech_penis)
		//		));
		//}

		//public static bool MechanoidVaginaFertility(Pawn pawn)
		//{
		//	//仿生人只有使用远古科技部件才能生育
		//	BodyPartRecord Part = Genital_Helper.get_genitalsBPR(pawn);
		//	return (pawn.health.hediffSet.hediffs.Any((Hediff hed) =>
		//		(hed.Part == Part) &&
		//		(hed.def == Genital_Helper.archotech_vagina)
		//		));
		//}
	}
}
