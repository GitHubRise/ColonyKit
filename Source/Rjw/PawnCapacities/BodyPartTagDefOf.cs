#nullable disable

﻿using Verse;

namespace rjw
{
	/// <summary>
	/// 查找并返回 XML 中定义的 BodyPartTagDef
	/// </summary>
	public static class BodyPartTagDefOf {
		public static BodyPartTagDef RJW_Fertility
		{
			get
			{
				if (a == null) a = (BodyPartTagDef)GenDefDatabase.GetDef(typeof(BodyPartTagDef), "RJW_Fertility");
				return a;
			}
		}
		private static BodyPartTagDef a;
	}
	
}
