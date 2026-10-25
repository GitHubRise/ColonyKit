#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	public class Recipe_RemoveBreasts : Recipe_RemovePart
	{
		protected override bool ValidFor(Pawn p)
		{
			return base.ValidFor(p) && Genital_Helper.has_breasts(p) && !Genital_Helper.breasts_blocked(p);
		}
	}
}
