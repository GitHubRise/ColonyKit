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
	/// 配种的 RMB 分类生成器
	/// </summary>
	static class RMB_RapeAnimal
	{
		/// <summary>
		/// 创建配种与反向配种选项
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
				yield return new FloatMenuOption("RJW_RMB_RapeAnimal".Translate(target) + ": " + canCreateEntries.Reason, null);
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
				return "No breeding: Rape is disabled by the mod settings";
			}

			if (!RJWSettings.bestiality_enabled)
			{
				return "No breeding: Bestiality is disabled by the mod settings";
			}

			if (!xxx.can_rape(pawn, true))
			{
				return "No breeding: Pawn can't rape";
			}

			if (!xxx.can_be_fucked(target))
			{
				return "No breeding: Target can't have sex";
			}

			if (target.HostileTo(pawn) && !target.Downed)
			{
				return "No breeding: Hostile must be downed";
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
				if (AttractionUtility.Evaluate(pawn, target) < 0.1f)
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
				text = "RJW_RMB_RapeAnimal_Reverse".Translate(target);
			}
			else
			{
				text = "RJW_RMB_RapeAnimal".Translate(target);
			}

			Action action = null;

			if (target.HostileTo(pawn))
			{
				// 需要测试
				// 在从 RMB_Rape 中分离出动物部分之前，理论上可能会走到这里。
				// 但 RapeEnemy 工作驱动能正常工作吗？
				// 或者倒地的动物会失去敌对状态？
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
			else if (!xxx.can_fuck(pawn) && !xxx.can_be_fucked(pawn))
			{
				if (RJWSettings.DevMode && !reverse)
				{
					return new FloatMenuOption("No breeding: Pawn can't fuck", null);
				}

				return null;
			}
			else
			{
				action = delegate ()
				{
					var validinteractions = RMB_Menu.GenerateNonSoloSexRoleOptions(pawn, target, xxx.bestiality, true, reverse);
					if (validinteractions.Any())
						FloatMenuUtility.MakeMenu(validinteractions, (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
					else
					{
						var messageText = "RJW_RMB_NoValidInteractions".Translate(text);
						Messages.Message(messageText, MessageTypeDefOf.RejectInput, false);
					}
				};
			}

			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action, MenuOptionPriority.High), pawn, target);
		}
	}
}
