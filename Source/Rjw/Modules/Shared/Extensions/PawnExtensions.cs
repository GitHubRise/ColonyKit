#nullable disable

﻿using System;
using Verse;
using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

using rjw.Modules.Shared.Enums;

namespace rjw.Modules.Shared.Extensions
{
	public static class PawnExtensions
	{
		public static string GetName(this Pawn pawn)
		{
			if (pawn == null)
			{
				return "null";
			}

			if (String.IsNullOrWhiteSpace(pawn.Name?.ToStringFull) == false)
			{
				return pawn.Name.ToStringFull;
			}

			return pawn.def.defName;
		}

		public static PawnState GetPawnState(this Pawn pawn)
		{
			if (pawn.Dead)
			{
				return PawnState.Dead;
			}

			if (pawn.Downed)
			{
				if (pawn.health.capacities.CanBeAwake == false)
				{
					return PawnState.Unconscious;
				}

				return PawnState.Downed;
			}

			return PawnState.Healthy;
		}
	}
}
