#nullable disable

﻿using System;
using Verse;

namespace rjw.Modules.Shared.Extensions
{
	public static class BodyPartRecordExtension
	{
		/// <summary>
		/// 检查身体部位是否缺失。与原生 <see cref="HediffSet.PartIsMissing"/> 不同，
		/// 此方法会考虑人工替代部件。
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static bool IsMissingForPawn(this BodyPartRecord self, Pawn pawn)
		{
			if (pawn == null)
			{
				throw new ArgumentNullException(nameof(pawn));
			}
			if (self == null)
			{
				throw new ArgumentNullException(nameof(self));
			}

			HediffSet hediffSet = pawn.health.hediffSet;

			if (hediffSet.PartIsMissing(self))
			{
				// 部位可能因被人工部件替换而缺失。
				// 在原生游戏中，一个人工部件可以替换多个自然部件。
				// 例如，仿生臂实际上是肩部，而手臂、手和手指都缺失了
				return !hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(self);
			}

			return false;
		}
	}
}
