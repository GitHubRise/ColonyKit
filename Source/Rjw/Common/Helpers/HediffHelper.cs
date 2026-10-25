#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace rjw
{
	public static class HediffHelper
	{
		/// <summary>
		/// 使用自定义的状态类创建状态
		/// </summary>
		/// <typeparam name="T">状态类。应为状态定义所指定的类或该类的子类</typeparam>
		/// <param name="def">hediffDef</param>
		/// <param name="pawn">新建状态的所有者</param>
		/// <param name="partRecord">状态分配到的身体部位</param>
		/// <returns>新建的状态</returns>
		public static T MakeHediff<T>(HediffDef def, Pawn pawn, BodyPartRecord partRecord = null) where T : Hediff
		{
			if (!def.hediffClass.IsAssignableFrom(typeof(T)))
			{
				throw new InvalidOperationException($"trying to create hediff with incompatible class: {typeof(T).Name} is not a {def.hediffClass.Name} or its subclass");
			}

			T hediff = (T)Activator.CreateInstance(typeof(T));
			hediff.def = def;
			hediff.pawn = pawn;
			hediff.Part = partRecord;
			hediff.loadID = Find.UniqueIDsManager.GetNextHediffID();
			hediff.PostMake();

			return hediff;
		}

		/// <summary>
		/// 用数据填充已有的状态类
		/// </summary>
		/// <typeparam name="T">状态类。应为状态定义所指定的类或该类的子类</typeparam>
		/// <param name="def">hediffDef</param>
		/// <param name="hediff">空的状态实例</param>
		/// <param name="pawn">新建状态的所有者</param>
		/// <param name="partRecord">状态分配到的身体部位</param>
		/// <returns>新建的状态</returns>
		public static T FillHediff<T>(HediffDef def, T hediff, Pawn pawn, BodyPartRecord partRecord = null) where T : Hediff
		{
			if (!def.hediffClass.IsAssignableFrom(typeof(T)))
			{
				throw new InvalidOperationException($"trying to create hediff with incompatible class: {typeof(T).Name} is not a {def.hediffClass.Name} or its subclass");
			}

			hediff.def = def;
			hediff.pawn = pawn;
			hediff.Part = partRecord;
			hediff.loadID = Find.UniqueIDsManager.GetNextHediffID();
			hediff.PostMake();

			return hediff;
		}

		public static bool IsNaturalSexPart(this HediffDef def)
		{
			return def is HediffDef_SexPart && typeof(Hediff_NaturalSexPart).IsAssignableFrom(def.hediffClass);
		}

		public static bool IsArtificialSexPart(this HediffDef def)
		{
			return def is HediffDef_SexPart && typeof(Hediff_ArtificialSexPart).IsAssignableFrom(def.hediffClass);
		}
	}
}
