using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 原版组建远行队时，Pawns 页签只创建这些 section：
	///   殖民者 / 奴隶 / 囚犯 / 可捕获 / 动物 / Biotech 机械主控单位 / 亚人实体。
	///
	/// Mk-V 虽然已经能通过 AllSendablePawns 和 CaravanUtility.IsOwner 进入
	/// transferables，但它不属于上面任何一类，所以列表里没有任何 section
	/// 显示它，看起来就像“远行队里没有 Mk-V”。
	///
	/// 这里只追加一个「机器人」section，把玩家阵营的 Mk-V 显示出来；
	/// 不改原版其他 section，也不影响 Mk-I ~ Mk-IV 或动物。
	/// </summary>
	[HarmonyPatch(typeof(CaravanUIUtility), nameof(CaravanUIUtility.AddPawnsSections))]
	internal static class Patch_MkVCaravanSection
	{
		private static void Postfix(TransferableOneWayWidget widget,
			List<TransferableOneWay> transferables)
		{
			if (widget == null || transferables == null)
			{
				return;
			}
			// 只从 Pawn 类 transferable 中挑 Mk-V，避免影响物品/远行物资。
			List<TransferableOneWay> robots = transferables
				.Where(t => t?.ThingDef?.category == ThingCategory.Pawn
					&& t.AnyThing is Pawn pawn
					&& NannyBotUtility.IsMkV(pawn)
					&& pawn.Faction == Faction.OfPlayer)
				.ToList();
			if (robots.Count == 0)
			{
				return;
			}
			// AddSection 是原版 widget 的公开入口；追加后仍按原版排序/搜索/计数。
			widget.AddSection("CK.Robotics.Caravan.RobotsSection".Translate(), robots);
		}
	}
}
