#nullable disable

﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rjw
{
	/// <summary>
	/// 比较特质是否相等，但忽略强制标志
	/// </summary>
	class TraitComparer : IEqualityComparer<Trait>
	{
		bool ignoreForced;
		bool ignoreDegree;

		public TraitComparer(bool ignoreDegree = false, bool ignoreForced = true)
		{
			this.ignoreDegree = ignoreDegree;
			this.ignoreForced = ignoreForced;
		}

		public bool Equals(Trait x, Trait y)
		{
			return
				x.def == y.def &&
				(ignoreDegree || (x.Degree == y.Degree)) &&
				(ignoreForced || (x.ScenForced == y.ScenForced));
		}

		public int GetHashCode(Trait obj)
		{
			return
				(obj.def.GetHashCode() << 5) +
				(ignoreDegree ? 0 : obj.Degree) +
				((ignoreForced || obj.ScenForced) ? 0 : 0x10);
		}
	}
}
