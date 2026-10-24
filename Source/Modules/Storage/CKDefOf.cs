using RimWorld;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>立体库用到的 Def 引用；由 DefOf 机制在 Def 加载后绑定。</summary>
	[DefOf]
	public static class CKDefOf
	{
		public static JobDef CK_WithdrawFromWarehouse;

		static CKDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(CKDefOf));
		}
	}
}
