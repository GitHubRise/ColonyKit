#nullable disable

﻿using System.Collections.Generic;
using Verse;

namespace rjw
{
	/// <summary>
	/// 只是把数据从 XML 传递到代码的最简单形式
	/// </summary>
	public class StringListDef : Def
	{
		public List<string> strings = new List<string>(); 
	}
}
