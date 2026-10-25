#nullable disable

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	public class Recipe_AddMultiPart : Recipe_InstallPart
	{
		// 记录故事，但不移除已有的部位
		protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			if (billDoer != null)
			{
				TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
			}
		}

		protected override bool CanApplyOnPart(Pawn pawn, BodyPartRecord record)
		{
			return base.CanApplyOnPart(pawn, record) && !pawn.health.hediffSet.PartIsMissing(record);
		}
	}
}
