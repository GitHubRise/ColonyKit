#nullable disable

﻿using RimWorld;
using rjw.Modules.Attraction;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace rjw.RMB
{
	/// <summary>
	/// 兽交的 RMB 分类生成器
	/// </summary>
	static class RMB_Bestiality
	{
		/// <summary>
		/// 创建兽交与反向兽交选项
		/// </summary>
		/// <param name="pawn">选中的小人</param>
		/// <param name="target">右键点击的目标</param>
		public static IEnumerable<FloatMenuOption> GetOptions(Pawn pawn, Pawn target)
		{
			AcceptanceReport canCreateEntries = DoBasicChecks(pawn, target);

			if (!canCreateEntries)
			{
				if (RJWSettings.DevMode && !canCreateEntries.Reason.NullOrEmpty())
					yield return new FloatMenuOption(canCreateEntries.Reason, null);

				yield break;
			}

			canCreateEntries = DoChecks(pawn, target);

			if (!canCreateEntries)
			{
				yield return new FloatMenuOption("RJW_RMB_Bestiality".Translate(target) + ": " + canCreateEntries.Reason, null);
				yield break;
			}

			FloatMenuOption opt = GenerateCategoryOption(pawn, target);
			if (opt != null)
				yield return opt;

			opt = GenerateCategoryOption(pawn, target, true);
			if (opt != null)
				yield return opt;
		}

		/// <summary>
		/// 检查那些对玩家显而易见或不会因特定小人而改变的事项。
		/// </summary>
		/// <returns>
		/// AcceptanceReport。原因字符串未翻译，只应在开发者模式中显示
		/// </returns>
		private static AcceptanceReport DoBasicChecks(Pawn pawn, Pawn target)
		{
			if (!RJWSettings.bestiality_enabled)
			{
				return "No bestiality: Disabled by the mod settings";
			}

			if (target.Downed)
			{
				return "No bestiality: Target is downed";
			}

			if (target.HostileTo(pawn))
			{
				return "No bestiality: Target is hostile";
			}

			if (!xxx.can_be_fucked(target) && !xxx.can_fuck(target))
			{
				return "No bestiality: Target can't have sex";
			}

			if (target.Faction != pawn.Faction)
			{
				return "No bestiality: Target must belong to pawn's faction";
			}

			return true;
		}

		/// <summary>
		/// 检查那些会因特定小人而改变的事项。
		/// </summary>
		/// <returns>
		/// AcceptanceReport。原因为已翻译的字符串，且不应为 null。
		/// </returns>
		private static AcceptanceReport DoChecks(Pawn pawn, Pawn target)
		{
			if (!pawn.IsDesignatedHero() && !pawn.IsHeroOwner())
			{
				int opinionOf = pawn.relations.OpinionOf(target);
				if (opinionOf < RJWHookupSettings.MinimumRelationshipToHookup)
				{
					if (!(opinionOf > 0 && xxx.is_nympho(pawn)))
					{
						return "RJW_RMB_ReasonLowOpinionOfTarget".Translate();
					}
				}
				if (AttractionUtility.Evaluate(pawn, target, AttractionPurpose.ForFucking) < 0.1f)
				{
					return "RJW_RMB_ReasonUnappealingTarget".Translate();
				}
			}

			if ((pawn.ownership.OwnedBed == null) && (target.ownership.OwnedBed == null))
			{
				return "RJW_RMB_ReasonNeedBed".Translate();
			}

			if ((!pawn.CanReach(target.ownership.OwnedBed, PathEndMode.OnCell, Danger.Some)) && (pawn.ownership.OwnedBed == null))
			{
				return "RJW_RMB_ReasonCantReachBed".Translate();
			}

			if  ((!target.CanReach(pawn.ownership.OwnedBed, PathEndMode.OnCell, Danger.Some)) && (target.ownership.OwnedBed == null))
			{
				return "RJW_RMB_ReasonCantReachBed".Translate();
			}

			return true;
		}

		/// <summary>
		/// 生成一个 FloatMenuOption
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="target"></param>
		/// <param name="reverse"></param>
		/// <returns>分类层级项，点击后打开子菜单</returns>
		private static FloatMenuOption GenerateCategoryOption(Pawn pawn, Pawn target, bool reverse = false)
		{
			string text = null;

			if (reverse)
			{
				text = "RJW_RMB_Bestiality_Reverse".Translate(target);
			}
			else
			{
				text = "RJW_RMB_Bestiality".Translate(target);
			}

			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate ()
			{
				var validinteractions = RMB_Menu.GenerateNonSoloSexRoleOptions(pawn, target, xxx.bestialityForFemale, false, reverse);
				if (validinteractions.Any())
					FloatMenuUtility.MakeMenu(validinteractions, (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
				else
				{
					var messageText = "RJW_RMB_NoValidInteractions".Translate(text);
					Messages.Message(messageText, MessageTypeDefOf.RejectInput, false);
				}
			}, MenuOptionPriority.High), pawn, target);
		}
	}
}
