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
	/// 这个派生类只是实现了 `SpecialDisplayStats` 方法的一个版本，它会检查所附的任何
	/// 组件是否有类似的升级派生类，并把它们的信息追加到状态的信息卡片（那个小 `i` 图标）。
	/// </summary>
	public class HediffWithCompDesc : HediffWithComps
	{
		
		/// <summary>
		/// 为状态信息卡片提供数据的函数。若你要重写，建议在自己的代码中复制前三行。
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			foreach (StatDrawEntry statDrawEntry in base.SpecialDisplayStats(req))
			{
				yield return statDrawEntry;
			}

			//遍历该状态提供的所有组件，若它们是描述派生类，则检查其属性行。
			if (this.comps != null)
			{
				foreach (HediffComp curComp in this.comps)
				{
					//HediffCompDesc tmp = new HediffCompDesc() ;
					if (curComp.GetType().IsSubclassOf(typeof(HediffCompDesc)))
					{
						HediffCompDesc tmp = (HediffCompDesc)curComp;
						foreach (StatDrawEntry statDrawEntry in tmp.SpecialDisplayStats(req))
						{
							yield return statDrawEntry;
						}
					}
				}
			}

			yield break;
		}
	}
}
