#nullable disable

﻿using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace rjw
{
	[HarmonyPatch(typeof(Pawn_ApparelTracker))]
	[HarmonyPatch("Wear")]
	internal static class PATCH_Pawn_ApparelTracker_Wear
	{
		// 阻止小人装备某件衣物，前提是该衣物与小人已穿着的某些上锁衣物冲突
		[HarmonyPrefix]
		private static bool prevent_wear_by_gear(Pawn_ApparelTracker __instance, ref Apparel newApparel)
		{
			var tra = __instance;
			// //--ModLog.Message ("Pawn_ApparelTracker.Wear called for " + newApparel.Label + " on " + tra.xxx.get_pawnname(pawn));
			foreach (var app in tra.WornApparel)
				if (app.has_lock() && (!ApparelUtility.CanWearTogether(newApparel.def, app.def, tra.pawn.RaceProps.body)))
				{
					if (!PawnUtility.ShouldSendNotificationAbout(tra.pawn)) return false;
					var text = "RJW_BG_Message_NoWear_BlockedBy".Translate(tra.pawn,newApparel.def.label,app.def.label);
					Messages.Message(text, tra.pawn, MessageTypeDefOf.NegativeEvent);
					return false;
				}
			return true;
		}

		// 穿戴装备时添加对应的状态
		[HarmonyPostfix]
		private static void on_wear(Pawn_ApparelTracker __instance, Apparel newApparel)
		{
			var tra = __instance;
			if ((newApparel.def is bondage_gear_def def) &&
				(newApparel.Wearer == tra.pawn))
			{ // 检查衣物是否真的被穿上
				def.soul.on_wear(tra.pawn, newApparel);
			}
		}
	}

	// 卸下装备时移除该状态
	[HarmonyPatch(typeof(Pawn_ApparelTracker))]
	[HarmonyPatch("Notify_ApparelRemoved")]
	internal static class PATCH_Pawn_ApparelTracker_Remove
	{
		[HarmonyPostfix]
		private static void on_remove(Pawn_ApparelTracker __instance, Apparel apparel)
		{
			if (apparel.def is bondage_gear_def def)
			{
				def.soul.on_remove(apparel, __instance.pawn);
			}
		}
	}

	// 修补 TryDrop 方法，使上锁衣物无法被 JobDriver_RemoveApparel 丢弃，也无法被剥除——
	// 无论是从尸体还是从倒地的小人身上。
	internal static class PATCH_Pawn_ApparelTracker_TryDrop
	{
		// 无法在特性中调用 MakeByRefType，因此需要这个方法。
		private static MethodInfo get_target()
		{
			return typeof(Pawn_ApparelTracker).GetMethod("TryDrop", new Type[] {
				typeof(Apparel),
				typeof(Apparel).MakeByRefType (),
				typeof(IntVec3),
				typeof(bool)
			});
		}

		// 不能直接把 [HarmonyTargetMethod] 放在 get_target 上，因此需要这个方法。顺带一提，我
		// 也不清楚 HarmonyTargetMethod 为什么不起作用。我确信自己都设置对了，所以这可能是
		// Harmony 自身的 bug，但我懒得细究。
		public static void apply(Harmony har)
		{
			var tar = get_target();
			var pat = typeof(PATCH_Pawn_ApparelTracker_TryDrop).GetMethod("prevent_locked_apparel_drop");
			har.Patch(tar, new HarmonyMethod(pat), null);
		}

		public static bool prevent_locked_apparel_drop(Pawn_ApparelTracker __instance, ref Apparel ap, ref bool __result)
		{
			if (!ap.has_lock())
				return true;
			else
			{
				__result = false;
				return false;
			}
		}
	}

	// 防止从随商队在外的小人身上移除上锁衣物
	// TODO：如果钥匙在商队物品栏里，则允许移除该衣物
	//[HarmonyPatch (typeof (WITab_Caravan_Gear))]
	//[HarmonyPatch ("TryRemoveFromCurrentWearer")]
	//static class PATCH_WITab_Caravan_Gear_TryRemoveFromCurrentWearer {
	//	[HarmonyPrefix]
	//	static bool prevent_locked_apparel_removal (WITab_Caravan_Gear __instance, Thing t, ref bool __result)
	//	{
	//		var app = t as Apparel;
	//		if ((app == null) || (! app.has_lock ()))
	//			return true;
	//		else {
	//			__result = false;
	//			return false;
	//		}
	//	}
	//}

	// 处理一种特殊情况：玩家用商队装备页签，给某个小人装备一件可上锁的衣物，而这个小人的
	// 身上已穿着同样的、已上锁的衣物；要确保该操作无效且不改变任何东西。这是为了
	// 修复一个 bug：尽管新衣物无法装备到小人身上，它却会以某种方式复制已装备衣物的全息烙印
	// 信息。
	[HarmonyPatch(typeof(WITab_Caravan_Gear))]
	[HarmonyPatch("TryEquipDraggedItem")]
	internal static class PATCH_WITab_Caravan_Gear_TryEquipDraggedItem
	{
		private static Thing get_dragged_item(WITab_Caravan_Gear tab)
		{
			return (Thing)typeof(WITab_Caravan_Gear).GetField("draggedItem", xxx.ins_public_or_no).GetValue(tab);
		}

		private static void set_dragged_item(WITab_Caravan_Gear tab, Thing t)
		{
			typeof(WITab_Caravan_Gear).GetField("draggedItem", xxx.ins_public_or_no).SetValue(tab, t);
		}

		[HarmonyPrefix]
		private static bool prevent_locked_apparel_conflict(WITab_Caravan_Gear __instance, ref Pawn p)
		{
			if (__instance == null) return true;
			if (get_dragged_item(__instance) is Apparel dragged_app && p.apparel != null && dragged_app.has_lock())
			{
				foreach (var equipped_app in p.apparel.WornApparel)
				{
					if (equipped_app.has_lock() && (!ApparelUtility.CanWearTogether(dragged_app.def, equipped_app.def, p.RaceProps.body)))
					{
						set_dragged_item(__instance, null);
						return false;
					}
				}
			}

			return true;
		}
	}
	/* GONE IN b19
	// Prevent locked apparel from being removed from a corpse
	// TODO: Find out when this method is actually called (probably doesn't matter but still)
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch("RemoveFromCorpseIfPossible")]
	internal static class PATCH_Dialog_FormCaravan_RemoveFromCorpseIfPossible
	{
		[HarmonyPrefix]
		private static bool prevent_locked_apparel_removal(Dialog_FormCaravan __instance, Thing thing)
		{
			var app = thing as Apparel;
			return (app == null) || (!app.has_lock());
		}
	}
	*/
	// 修补小人装备页签中的一个方法，以便玩家命令小人
	// 丢弃其已装备的上锁衣物时，给该小人一个特殊的工作驱动
	// TODO：寻找钥匙，如果小人能拿到，就给其一个新工作，让它前去取钥匙
	// 并用它解开衣物
	[HarmonyPatch(typeof(ITab_Pawn_Gear))]
	[HarmonyPatch("InterfaceDrop")]
	internal static class PATCH_ITab_Pawn_Gear_InterfaceDrop
	{
		private static Pawn SelPawnForGear(ITab_Pawn_Gear tab)
		{
			var pro = typeof(ITab_Pawn_Gear).GetProperty("SelPawnForGear", xxx.ins_public_or_no);
			return (Pawn)pro.GetValue(tab, null);
		}

		[HarmonyPrefix]
		private static bool drop_locked_apparel(ITab_Pawn_Gear __instance, Thing t)
		{
			var tab = __instance;
			var apparel = t as Apparel;
			if ((apparel == null) || (!apparel.has_lock()))
				return true;
			else
			{
				var p = SelPawnForGear(tab);
				if ((p.apparel != null) && (p.apparel.WornApparel.Contains(apparel)))
				{
					p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(xxx.struggle_in_BondageGear, apparel));
					return false;
				}
				else
					return true;
			}
		}
	}

	// 修补囚犯在牢房里寻找衣物的方法，使他们不会试图穿上
	// 与上锁衣物冲突的东西。用来阻止小人试图
	// 把上锁衣物优化掉的那个方法在这里不管用，因为囚犯不检查 GetSpecialApparelScoreOffset。
	[HarmonyPatch(typeof(JobGiver_PrisonerGetDressed))]
	[HarmonyPatch("FindGarmentCoveringPart")]
	internal static class PATCH_JobGiver_PrisonerGetDressed_FindGarmentCoveringPart
	{
		private static bool conflicts(Pawn_ApparelTracker tra, Apparel new_app)
		{
			foreach (var worn_app in tra.WornApparel)
				if (worn_app.has_lock() && (!ApparelUtility.CanWearTogether(worn_app.def, new_app.def, tra.pawn.RaceProps.body)))
					return true;

			return false;
		}

		[HarmonyPrefix]
		private static bool prevent_locked_apparel_conflict(JobGiver_PrisonerGetDressed __instance, Pawn pawn, BodyPartGroupDef bodyPartGroupDef, ref Apparel __result)
		{
			// 如果囚犯没有穿着上锁衣物，就直接运行原方法
			if (!pawn.is_wearing_locked_apparel())
				return true;

			// 否则复制原方法里的逻辑，只是多加一项检查
			// 用来过滤掉与已穿着的上锁衣物冲突的衣物。
			else
			{
				var room = pawn.GetRoom();
				if (room.IsPrisonCell)
					foreach (var c in room.Cells)
						foreach (var t in c.GetThingList(pawn.Map))
						{
							if ((t is Apparel app) &&
								app.def.apparel.bodyPartGroups.Contains(bodyPartGroupDef) &&
								pawn.CanReserve(app) &&
								ApparelUtility.HasPartsToWear(pawn, app.def) &&
								(!conflicts(pawn.apparel, app)))
							{
								__result = app;
								return false;
							}
						}
				__result = null;
				return false;
			}
		}
	}

	// 修补囚犯在牢房里寻找衣物的方法，使他们不会试图穿上
	// 与上锁衣物冲突的东西。用来阻止小人试图
	// 把上锁衣物优化掉的那个方法在这里不管用，因为囚犯不检查 GetSpecialApparelScoreOffset。
	[HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
	[HarmonyPatch("ApparelScoreRaw")]
	internal static class PATCH_JobGiver_OptimizeApparel_ApparelScoreRaw
	{
		[HarmonyPostfix]
		static void bondage_score_offset(Pawn pawn, Apparel ap, ref float __result, JobGiver_OptimizeApparel __instance)
		{
			if (ap.HasThingCategory(ThingCategoryDef.Named("Bondage")))
			{
				if (ap.Wearer == pawn)
					__result += 0f;
				else if(ap.AllComps.Any(x=> x.props is CompProperties_HoloCryptoStamped))
					__result -= 1e5f;
				else
					__result += 0f;

				//ModLog.Message("bondage_score_offset " + __result);

			}
		}
	}
}
