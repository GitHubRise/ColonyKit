#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	public class Recipe_RemoveGenitals : Recipe_RemovePart
	{
		protected override bool ValidFor(Pawn p)
		{
			return base.ValidFor(p) && Genital_Helper.has_genitals(p) && !Genital_Helper.genitals_blocked(p);
		}
	}
}
