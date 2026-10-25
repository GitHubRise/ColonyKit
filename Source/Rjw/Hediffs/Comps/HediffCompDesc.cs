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
	public class HediffCompDesc : HediffComp
	{

		/// <summary>
		/// 用于为父 Hediff 的信息卡提供数据而调用的函数，请用你自己的数据重写。
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			/*
			 * 格式为 
			 * - 归类到的分类
			 * - 行的标签
			 * - 行的数值
			 * - 右侧的大段描述字段，选中条目时显示。
			 * - 在分类中的位置，数字越大越靠前显示。
			 */
			//yield return new StatDrawEntry(StatCategoryDefOf.Ability, "StatLabel", "ValueString", "Long report text goes Here!", 1, null);
			yield break;
		}
	}
}
