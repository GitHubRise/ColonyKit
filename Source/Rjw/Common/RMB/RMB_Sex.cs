#nullable disable

﻿using RimWorld;
using rjw.Modules.Attraction;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rjw.RMB
{
	/// <summary>
	/// “正常”性行为的 RMB 分类生成器
	/// </summary>
	static class RMB_Sex
	{
		/// <summary>
		/// 创建性行为与反向性行为选项
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
				yield return new FloatMenuOption("RJW_RMB_Sex".Translate(target) + ": " + canCreateEntries.Reason, null);
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
			if (target.Downed)
			{
				return "No sex: Target downed";
			}

			if (target.HostileTo(pawn))
			{
				return "No sex: Target hostile";
			}

			if (!xxx.can_be_fucked(target) && !xxx.can_fuck(target))
			{
				return "No sex: Target can't have sex";
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
			int opinionOf;

			if (!pawn.IsDesignatedHero() && !pawn.IsHeroOwner())
			{
				opinionOf = pawn.relations.OpinionOf(target);
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

			opinionOf = target.relations.OpinionOf(pawn);
			if (opinionOf < RJWHookupSettings.MinimumRelationshipToHookup)
			{
				if (!(opinionOf > 0 && xxx.is_nympho(target)))
				{
					return "RJW_RMB_ReasonLowOpinionOfPawn".Translate();
				}
			}

			if (AttractionUtility.Evaluate(target, pawn, AttractionPurpose.ForFucking) < 0.1f)
			{
				return "RJW_RMB_ReasonUnappealingPawn".Translate();
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
				text = "RJW_RMB_Sex_Reverse".Translate(target);
			}
			else
			{
				text = "RJW_RMB_Sex".Translate(target);
			}

			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate ()
			{
				JobDef job = null;
				if (target.InBed())
					job = xxx.casual_sex;
				else
					job = xxx.quick_sex;

				var validinteractions = RMB_Menu.GenerateNonSoloSexRoleOptions(pawn, target, job, false, reverse);
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
