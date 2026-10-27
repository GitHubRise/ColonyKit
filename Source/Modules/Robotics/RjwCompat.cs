using System;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>RJW 是可选的：只有程序集和核心类型存在时才启用兼容分支。</summary>
	internal static class RjwCompat
	{
		private static readonly bool loaded = Type.GetType("rjw.xxx, RJW") != null;

		public static bool Loaded => loaded;
	}
}
