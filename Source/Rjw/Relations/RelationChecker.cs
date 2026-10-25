#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace rjw
{
	/// <summary>
	/// 检查亲缘关系；这是对依赖其他关系检查的关系检查所做的变通，因为原版的 inRelation 检查已被加上前缀。
	/// 
	/// 如果第一个小人是第二个小人的指定亲属，则返回 true
	/// 
	///  如果 "me" isRelationOf "other" 则返回 true
	/// </summary>
	/// 
	public static class RelationChecker
	{
		// 类似 getMother()，但不区分性别，并能取到同性的第二位亲本
		private static Pawn getParent(Pawn pawn, bool skipFirst)
		{
			if (pawn == null)
			{
				return null;
			}
			if (!pawn.RaceProps.IsFlesh)
			{
				return null;
			}
			if (pawn.relations == null)
			{
				return null;
			}
			foreach (DirectPawnRelation directPawnRelation in pawn.relations.DirectRelations)
			{
				if (directPawnRelation.def == PawnRelationDefOf.Parent)
				{
					if (!skipFirst)
					{
						return directPawnRelation.otherPawn;
					}
					skipFirst = false;
				}
			}
			return null;
		}

		// 并非必要，但能让关系读取更简单
		public static Pawn getParent(Pawn pawn)
		{
			return getParent(pawn, false);
		}

		public static Pawn getSecondParent(Pawn pawn)
		{
			return getParent(pawn, true);
		}

		// 检查所有 Parent 直接关系，忽略性别
		public static bool isChildOf(Pawn me, Pawn other)
		{
			if (me == null || other ==null || me == other)
			{
				return false;
			}
			if (!other.RaceProps.IsFlesh)
			{
				return false;
			}
			if (me.relations == null)
			{
				return false;
			}
			foreach (DirectPawnRelation directPawnRelation in me.relations.DirectRelations)
			{
				if (directPawnRelation.def == PawnRelationDefOf.Parent && directPawnRelation.otherPawn == other)
				{
					return true;
				}
			}
			return false;
		}

		public static bool isSiblingOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if (getParent(me) != null && getSecondParent(me) != null && (
				getParent(me) == getParent(other) && getSecondParent(me) == getSecondParent(other) || // 如果双方父母相同
				getParent(me) == getSecondParent(other) && getSecondParent(me) == getParent(other))) // 如果父母角色互换，我不知道这种关系该怎么命名，但没有这段逻辑他们就会被判定为亲属，这是错的
			{
				return true;
			}
			return false;
		}

		public static bool isHalfSiblingOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if (isSiblingOf(me, other))
			{
				return false;
			}
			return getParent(me) != null && (getParent(me) == getParent(other) || getParent(me) == getSecondParent(other)) ||
					getSecondParent(me) != null && (getSecondParent(me) == getParent(other) || getSecondParent(me) == getSecondParent(other));
		}

		public static bool isAnySiblingOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if (getParent(me) != null && (getParent(me) == getParent(other) || getParent(me) == getSecondParent(other)) ||
				getSecondParent(me) != null && (getSecondParent(me) == getParent(other) || getSecondParent(me) == getSecondParent(other)))
			{
				return true;
			}
			return false;
		}

		public static bool isGrandchildOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if ((getParent(me) != null && isChildOf(getParent(me), other)) || (getSecondParent(me) != null && isChildOf(getSecondParent(me), other)))
			{
				return true;
			}
			return false;
		}

		public static bool isGrandparentOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if (isGrandchildOf(other, me))
			{
				return true;
			}
			return false;
		}

		public static bool isNephewOrNieceOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if ((getParent(me) != null && (isAnySiblingOf(other, getParent(me)))) || (getSecondParent(me) != null && (isAnySiblingOf(other, getSecondParent(me)))))
			{
				return true;
			}
			return false;
		}

		public static bool isUncleOrAuntOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if (isNephewOrNieceOf(other, me))
			{
				return true;
			}
			return false;
		}

		public static bool isCousinOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}

			if ((getParent(other) != null && isNephewOrNieceOf(me, getParent(other))) || (getSecondParent(other) != null && isNephewOrNieceOf(me, getSecondParent(other))))
			{
				return true;
			}
			return false;
		}

		public static bool isGreatGrandparentOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}

			return isGreatGrandChildOf(other, me);
		}

		public static bool isGreatGrandChildOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}

			if ((getParent(me) != null && isGrandchildOf(getParent(me), other)) || (getSecondParent(me) != null && isGrandchildOf(getSecondParent(me), other)))
			{
				return true;
			}
			return false;
		}

		public static bool isGreatUncleOrAuntOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}

			return isGrandnephewOrGrandnieceOf(other, me);
		}

		public static bool isGrandnephewOrGrandnieceOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}

			if ((getParent(me) != null && isUncleOrAuntOf(other, getParent(me))) || (getSecondParent(me) != null && isUncleOrAuntOf(other, getSecondParent(me))))
			{
				return true;
			}
			return false;
		}

		public static bool isCousinOnceRemovedOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			if ((getParent(other) != null && isCousinOf(me, getParent(other))) || (getSecondParent(other) != null && isCousinOf(me, getSecondParent(other))))
			{
				return true;
			}
			if ((getParent(other) != null && isGrandnephewOrGrandnieceOf(me, getParent(other))) || (getSecondParent(other) != null && isGrandnephewOrGrandnieceOf(me, getSecondParent(other))))
			{
				return true;
			}
			return false;
		}

		public static bool isSecondCousinOf(Pawn me, Pawn other)
		{
			if (me == null || me == other)
			{
				return false;
			}
			Pawn parent = getParent(other);
			if (parent != null && ((getParent(parent) != null && isGrandnephewOrGrandnieceOf(me, getParent(parent))) || (getSecondParent(parent) != null && isGrandnephewOrGrandnieceOf(me, getSecondParent(parent)))))
			{
				return true;
			}

			Pawn secondParent = getSecondParent(other);
			if (secondParent != null && (getParent(secondParent) != null && isGrandnephewOrGrandnieceOf(me, getParent(secondParent)) || (getSecondParent(secondParent) != null && isGrandnephewOrGrandnieceOf(me, getSecondParent(secondParent)))))
			{
				return true;
			}
			return false;
		}
	}
}
