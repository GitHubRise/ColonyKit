#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	public class Recipe_ClaimChild :  RecipeWorker
	{
		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
			if (pawn != null)//我不知道它是怎么工作的，但 Recipe_ShutDown : RecipeWorker 在不适用时并不返回值
			{
				//ModLog.Message("RJW Claim child check on " + pawn);
				if (xxx.is_human(pawn) && !pawn.IsColonist)
				{
					if ( (pawn.ageTracker.CurLifeStageIndex < 2))//猜测目前硬编码为人类 4 个标准生命阶段中的 `baby` 和 `toddler`
					{
						BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
						if (brain != null)
						{
							//ModLog.Message("RJW Claim child is applicable");
							yield return brain;
						}
					}
				}
			}
		}

		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			if (pawn == null)
			{
				ModLog.Error("Error applying medical recipe, pawn is null");
				return;
			}
			pawn.SetFaction(Faction.OfPlayer);
			//我们可以这样做 
			//pawn.SetFaction(billDoer.Faction);
			//但那没有用，因为 GetPartsToApplyOn 本来就不支持派系，而且所有配方都硬编码为玩家。
		}
	}
}
