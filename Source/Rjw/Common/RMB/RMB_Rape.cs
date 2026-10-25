#nullable disable

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System;
using rjw.Modules.Attraction;

namespace rjw.RMB
{
	/// <summary>
	/// 强暴的 RMB 分类生成器
	/// </summary>
	static class RMB_Rape
	{
		/// <summary>
		/// 创建强暴与反向强暴选项
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
				yield return new FloatMenuOption("RJW_RMB_Rape".Translate(target) + ": " + canCreateEntries.Reason, null);
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
			if (!RJWSettings.rape_enabled)
			{
				return "No rape: Disabled by the mod settings";
			}

			if (!xxx.can_rape(pawn, true))
			{
				return "No rape: Pawn can't rape";
			}

			if (!xxx.can_be_fucked(target))
			{
				return "No rape: Target can't have sex";
			}

			if (target.HostileTo(pawn) && !target.Downed)
			{
				return "No rape: Hostile must be downed";
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
				if (AttractionUtility.Evaluate(pawn, target, AttractionPurpose.ForRape) < 0.1f)
					return "RJW_RMB_ReasonUnappealingTarget".Translate();
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
				text = "RJW_RMB_Rape_Reverse".Translate(target);
			}
			else
			{
				text = "RJW_RMB_Rape".Translate(target);
			}

			Action action = null;

			if (target.HostileTo(pawn))
			{
				action = delegate ()
				{
					var validinteractions = RMB_Menu.GenerateNonSoloSexRoleOptions(pawn, target, xxx.RapeEnemy, true, reverse);
					if (validinteractions.Any())
						FloatMenuUtility.MakeMenu(validinteractions, (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
					else
					{
						var messageText = "RJW_RMB_NoValidInteractions".Translate(text);
						Messages.Message(messageText, MessageTypeDefOf.RejectInput, false);
					}
				};
			}
			else if (target.IsDesignatedComfort())
			{
				action = delegate ()
				{
					var validinteractions = RMB_Menu.GenerateNonSoloSexRoleOptions(pawn, target, xxx.RapeCP, true, reverse);
					if (validinteractions.Any())
						FloatMenuUtility.MakeMenu(validinteractions, (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
					else
					{
						var messageText = "RJW_RMB_NoValidInteractions".Translate(text);
						Messages.Message(messageText, MessageTypeDefOf.RejectInput, false);
					}
				};
			}
			else if (xxx.can_get_raped(target) && (xxx.get_vulnerability(target) >= xxx.get_vulnerability(pawn)))
			{
				action = delegate ()
				{
					var validinteractions = RMB_Menu.GenerateNonSoloSexRoleOptions(pawn, target, xxx.RapeRandom, true, reverse);
					if (validinteractions.Any())
						FloatMenuUtility.MakeMenu(validinteractions, (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
					else
					{
						var messageText = "RJW_RMB_NoValidInteractions".Translate(text);
						Messages.Message(messageText, MessageTypeDefOf.RejectInput, false);
					}
				};
			}
			else
			{
				if (RJWSettings.DevMode && !reverse)
				{
					return new FloatMenuOption("No rape: Target not hostile, not CP, can't be raped, or not vulnerable enough", null);
				}

				return null;
			}

			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action, MenuOptionPriority.High), pawn, target);
		}
	}
}
