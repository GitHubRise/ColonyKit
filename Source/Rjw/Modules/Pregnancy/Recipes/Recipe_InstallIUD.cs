#nullable disable

﻿using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw
{
	/// <summary>
	/// IUD - 防止怀孕
	/// </summary>
	public class Recipe_InstallIUD : Recipe_InstallImplantToExistParts
	{
		public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
		{
			if (RJWPregnancySettings.UseVanillaPregnancy)
			{
				return false;
			}
			return base.AvailableOnNow(thing, part);
		}

		// 我们把它注释掉吧。最糟能怎样？
		// public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		// {
		// 	if (!xxx.is_female(pawn))
		// 	{
		// 		return Enumerable.Empty<BodyPartRecord>();
		// 	}
		// 	return base.GetPartsToApplyOn(pawn, recipe);
		// }
	}
}
