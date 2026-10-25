#nullable disable

﻿using RimWorld;
using System.Collections.Generic;
using Verse;
using rjw.Modules.Interactions;

namespace rjw.RMB
{
	/// <summary>
	/// 自慰的 RMB 分类生成器。
	///
	/// 与双人选项不同，自慰菜单有 3 层：
	/// Masturbate -> Masturbate on bed
	///               Masturbate at (tile) -> Interaction 1
	///               Masturbate here         Interaction 2
	///                                       etc.
	/// </summary>
	static class RMB_Masturbate
	{
		/// <summary>
		/// 创建自慰选项
		/// </summary>
		/// <param name="pawn">选中的小人</param>
		public static IEnumerable<FloatMenuOption> GetOptions(Pawn pawn)
		{
			FloatMenuOption opt = GenerateCategoryOption(pawn);
			if (opt != null)
				yield return opt;
		}

		/// <summary>
		/// 生成一个 FloatMenuOption
		/// </summary>
		/// <returns>分类层级项，点击后打开子菜单</returns>
		private static FloatMenuOption GenerateCategoryOption(Pawn pawn)
		{
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RJW_RMB_Masturbate".Translate(), delegate ()
			{
				FloatMenuUtility.MakeMenu(GenerateSoloSexRoleOptions(pawn), (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
			}, MenuOptionPriority.High), pawn, pawn);
		}

		/// <summary>
		/// 为单人小人生成子菜单选项
		/// </summary>
		/// <param name="pawn">发起者小人</param>
		/// <returns>菜单选项列表，其中每一项代表一种可能的自慰类型</returns>
		private static IEnumerable<FloatMenuOption> GenerateSoloSexRoleOptions(Pawn pawn)
		{
			FloatMenuOption option = new("RJW_RMB_Masturbate_Bed".Translate(), delegate ()
			{
				Find.Targeter.BeginTargeting(TargetParemetersMasturbationChairOrBed(), (LocalTargetInfo targetThing) =>
				{
					FloatMenuUtility.MakeMenu(GenerateSoloSexPoseOptions(pawn, targetThing), (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
				});
			}, MenuOptionPriority.High);
			yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, pawn);

			option = new FloatMenuOption("RJW_RMB_Masturbate_At".Translate(), delegate ()
			{
				Find.Targeter.BeginTargeting(TargetParemetersMasturbationLoc(), (LocalTargetInfo targetLoc) =>
				{
					FloatMenuUtility.MakeMenu(GenerateSoloSexPoseOptions(pawn, targetLoc), (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
				});
			}, MenuOptionPriority.High);
			yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, pawn);

			option = new FloatMenuOption("RJW_RMB_Masturbate_Here".Translate(), delegate ()
			{
				FloatMenuUtility.MakeMenu(GenerateSoloSexPoseOptions(pawn, pawn.Position), (FloatMenuOption opt) => opt.Label, (FloatMenuOption opt) => opt.action);
			}, MenuOptionPriority.High);
			yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, pawn);
		}

		private static TargetingParameters TargetParemetersMasturbationChairOrBed()
		{
			return new TargetingParameters()
			{
				canTargetBuildings = true,
				mapObjectTargetsMustBeAutoAttackable = false,
				validator = (TargetInfo target) =>
				{
					if (!target.HasThing)
						return false;
					if (target.Thing is not Building building)
						return false;
					if (building.def.building.isSittable)
						return true;
					if (building is Building_Bed)
						return true;
					return false;
				}
			};
		}

		public static TargetingParameters TargetParemetersMasturbationLoc()
		{
			return new TargetingParameters()
			{
				canTargetLocations = true,
				mapObjectTargetsMustBeAutoAttackable = false,
				validator = (TargetInfo target) =>
				{
					if (!target.HasThing)
						return true;
					return false;
				}
			};
		}

		/// <summary>
		/// 为单人小人生成子子菜单选项
		/// </summary>
		/// <param name="pawn">发起者小人</param>
		/// <param name="target">自慰地点</param>
		/// <returns>菜单选项列表，其中每一项代表一种可能的自慰互动</returns>
		public static IEnumerable<FloatMenuOption> GenerateSoloSexPoseOptions(Pawn pawn, LocalTargetInfo target)
		{
			bool foundInteraction = false;

			foreach (InteractionDef d in SexUtility.SexInteractions)
			{
				var interaction = new SexInteraction(d);
				if (interaction.Extension.rjwSextype != xxx.rjwSextype.Masturbation.ToStringSafe())
					continue;

				var props = new SexProps(pawn, pawn) { interaction = interaction };
				var resolved = SexInteractionHelper.ResolveInteraction(props);
				if (resolved == null)
					continue;

				string label = interaction.Extension.RMBLabel.CapitalizeFirst();
				if (RJWSettings.DevMode)
					label += $" ( defName: {d.defName} )";

				FloatMenuOption option = new FloatMenuOption(label, delegate ()
				{
					RMB_Menu.HaveSex(pawn, xxx.Masturbate, target, d, resolved);
				}, MenuOptionPriority.High);
				yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);
				foundInteraction = true;
			}

			if (RJWSettings.DevMode && !foundInteraction)
			{
				yield return new FloatMenuOption("No interactions found", null);
			}
		}
	}
}
