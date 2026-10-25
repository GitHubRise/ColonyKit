#nullable disable

﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Multiplayer.API;

namespace rjw
{
	/// <summary>
	/// 社交的 RMB 分类生成器
	/// </summary>
	static class RMB_Socialize
	{
		/// <summary>
		/// 创建社交选项
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

			FloatMenuOption opt = GenerateCategoryOption(pawn, target);
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
			if (!pawn.interactions.CanInteractNowWith(target))
			{
				return $"No socialize: cannot interact with {target.NameShortColored} now";
			}

			return true;
		}

		private static FloatMenuOption GenerateCategoryOption(Pawn pawn, Pawn target)
		{
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RJW_RMB_Social".Translate(target), delegate ()
			{
				FloatMenuUtility.MakeMenu(GenerateSocialOptions(pawn, target).Where(x => x.action != null), (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
			}, MenuOptionPriority.High), pawn, target);
		}

		/// <summary>
		/// 为社交生成子菜单选项
		/// </summary>
		/// <param name="pawn">发起者小人</param>
		/// <param name="target">目标小人</param>
		/// <returns>菜单选项列表，其中每一项代表一种可能的互动</returns>
		private static IEnumerable<FloatMenuOption> GenerateSocialOptions(Pawn pawn, LocalTargetInfo target)
		{
			FloatMenuOption option = new("RJW_RMB_Insult".Translate(), delegate ()
			{
				Socialize(pawn, target, InteractionDefOf.Insult);
			}, MenuOptionPriority.High);
			yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);

			if (!pawn.HostileTo(target.Pawn))
			{
				option = new FloatMenuOption("RJW_RMB_SocialFight".Translate(), delegate ()
				{
					SocializeFight(pawn, target);
				}, MenuOptionPriority.High);
				yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);

				option = new FloatMenuOption("RJW_RMB_Chat".Translate(), delegate ()
				{
					Socialize(pawn, target, InteractionDefOf.Chitchat);
				}, MenuOptionPriority.High);
				yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);

				// 太强了 +12 关系，在作弊菜单中启用
				if (RJWSettings.Allow_RMB_DeepTalk)
				{
					option = new FloatMenuOption("RJW_RMB_DeepTalk".Translate(), delegate ()
					{
						pawn.interactions.TryInteractWith(target.Pawn, InteractionDefOf.DeepTalk);
					}, MenuOptionPriority.High);
					yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);
				}

				if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, target.Pawn))
				{
					option = new FloatMenuOption("RJW_RMB_RomanceAttempt".Translate(), delegate ()
					{
						Socialize(pawn, target, InteractionDefOf.RomanceAttempt);
					}, MenuOptionPriority.High);
					yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);
				}

				if (pawn.relations.DirectRelationExists(PawnRelationDefOf.Lover, target.Pawn) || pawn.relations.DirectRelationExists(PawnRelationDefOf.Fiance, target.Pawn))
				{
					option = new FloatMenuOption("RJW_RMB_MarriageProposal".Translate(), delegate ()
					{
						Socialize(pawn, target, InteractionDefOf.MarriageProposal);
					}, MenuOptionPriority.High);
					yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);
				}
			}
		}

		//多人游戏同步操作

		/// <summary>
		/// 停止 <paramref name="pawn"/> 当前的工作并开始一场社交斗殴
		/// </summary>
		/// <param name="pawn">发起者</param>
		/// <param name="target">目标小人</param>
		[SyncMethod]
		static void SocializeFight(Pawn pawn, LocalTargetInfo target)
		{
			pawn.interactions.StartSocialFight(target.Pawn);
		}

		/// <summary>
		/// 停止 <paramref name="pawn"/> 当前的工作并开始一次互动
		/// </summary>
		/// <param name="pawn">发起者</param>
		/// <param name="target">目标小人</param>
		/// <param name="interaction">互动定义</param>
		[SyncMethod]
		static void Socialize(Pawn pawn, LocalTargetInfo target, InteractionDef interaction)
		{
			pawn.interactions.TryInteractWith(target.Pawn, interaction);
		}
	}
}
