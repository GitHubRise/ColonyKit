using RimWorld;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 只挑「可以安全放大数量」的物品：
	/// 尸体、掉落即毁、未完成品、武器/衣物/最小化建筑不动；
	/// 这些物品更适合以后由仓库按重量合并，而不是把 stackLimit 直接拉高。
	/// </summary>
	internal static class StorageStackRules
	{
		public static bool CanMultiply(ThingDef def)
		{
			if (def == null || def.category != ThingCategory.Item)
			{
				return false;
			}

			if (def.stackLimit <= 1 || def.IsCorpse || def.destroyOnDrop || def.isUnfinishedThing)
			{
				return false;
			}

			if (def.IsApparel || def.IsWeapon || def.isTechHediff)
			{
				return false;
			}

			if (def.thingClass != null && typeof(MinifiedThing).IsAssignableFrom(def.thingClass))
			{
				return false;
			}

			return def.IsStuff || !def.thingCategories.NullOrEmpty();
		}
	}
}
