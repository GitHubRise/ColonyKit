#nullable disable

﻿using RimWorld;
using Verse;
using static RimWorld.CompCableConnection;

namespace rjw
{
	public class RitualSpectatorFilter_CanDoLoving : RitualSpectatorFilter
	{
		public override bool Allowed(Pawn pawn)
		{
			return xxx.can_do_loving(pawn);
		}
	}
}
