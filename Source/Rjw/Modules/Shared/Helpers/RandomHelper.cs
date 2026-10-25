#nullable disable

﻿using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using rjw.Modules.Shared;
using UnityEngine;
using Verse;

namespace rjw
{
	public static class RandomHelper
	{
		/// <remarks>这并非万无一失</remarks>
		public static TType WeightedRandom<TType>(IList<Weighted<TType>> weights)
		{
			if (weights == null || weights.Any() == false || weights.Where(e => e.Weight < 0).Any())
			{
				return default(TType);
			}

			Weighted<TType> result;

			if (weights.TryRandomElementByWeight(e => e.Weight, out result) == true)
			{
				return result.Element;
			}

			return weights.RandomElement().Element;
		}
	}
}
