#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace rjw
{
	public class Recipe_RemoveAnus : Recipe_RemovePart
	{
		protected override bool ValidFor(Pawn p)
		{
			return base.ValidFor(p) && Genital_Helper.has_anus(p) && !Genital_Helper.anus_blocked(p);
		}
	}
}
