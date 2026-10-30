using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 取水账单的「维持 X 个」计数：只数满水桶（地图上 + 殖民者物品栏），
	/// 不把空桶/半桶算进去。
	/// </summary>
	public class RecipeWorkerCounter_FullBuckets : RecipeWorkerCounter
	{
		public override bool CanCountProducts(Bill_Production bill)
		{
			return true;
		}

		public override int CountProducts(Bill_Production bill)
		{
			return WaterContainerUtility.CountFullBuckets(bill?.Map);
		}

		public override string ProductsDescription(Bill_Production bill)
		{
			return "CK.Hygiene.WaterSpot.FullBuckets".Translate();
		}

		public override bool CanPossiblyStore(Bill_Production bill, ISlotGroup slotGroup)
		{
			return true;
		}
	}
}
