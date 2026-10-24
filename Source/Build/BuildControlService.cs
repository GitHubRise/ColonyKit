using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>建筑建造规则：返回 false 时用 Reason 作为拒绝原因。</summary>
	public sealed class BuildRule
	{
		public string Id;
		public Func<BuildableDef, IntVec3, Map, Rot4, ThingDef, bool> Allow;
		public Func<BuildableDef, IntVec3, Map, Rot4, ThingDef, string> Reason;
	}

	/// <summary>
	/// 建筑建造的统一控制：规则注册、放置校验、程序化放蓝图。
	/// </summary>
	/// <remarks>
	/// - <see cref="CanPlace"/> 走原版 <c>GenConstruct.CanPlaceBlueprintAt</c>，规则由
	///   <see cref="Patch_GenConstruct_CanPlaceBlueprintAt"/> 的 Postfix 追加；
	/// - <see cref="PlaceBlueprint"/> 先校验再调 <c>GenConstruct.PlaceBlueprintForBuild</c>，
	///   失败只发通知，不抛异常；
	/// - 规则只影响「放置蓝图」阶段，不改变施工/材料逻辑。
	/// </remarks>
	public static class BuildControlService
	{
		private static readonly List<BuildRule> rules = new List<BuildRule>();

		public static int RuleCount => rules.Count;

		/// <summary>注册规则；同 Id 覆盖。allow 抛异常时按拒绝处理并记日志。</summary>
		public static void RegisterRule(
			string id,
			Func<BuildableDef, IntVec3, Map, Rot4, ThingDef, bool> allow,
			Func<BuildableDef, IntVec3, Map, Rot4, ThingDef, string> reason = null)
		{
			if (id.NullOrEmpty() || allow == null)
			{
				return;
			}

			rules.RemoveAll(rule => rule.Id == id);
			rules.Add(new BuildRule
			{
				Id = id,
				Allow = allow,
				Reason = reason
			});
		}

		public static bool UnregisterRule(string id)
		{
			return rules.RemoveAll(rule => rule.Id == id) > 0;
		}

		public static void ClearRules()
		{
			rules.Clear();
		}

		/// <summary>原版放置校验 + 自定义规则（规则由 Postfix 追加，这里只调用原版）。</summary>
		public static AcceptanceReport CanPlace(
			BuildableDef def,
			IntVec3 center,
			Map map,
			Rot4 rot,
			ThingDef stuff = null)
		{
			return GenConstruct.CanPlaceBlueprintAt(def, center, rot, map, stuffDef: stuff);
		}

		/// <summary>校验并放置玩家蓝图；失败发 RejectInput 通知并返回 null。</summary>
		public static Blueprint_Build PlaceBlueprint(
			BuildableDef def,
			IntVec3 center,
			Map map,
			Rot4 rot,
			ThingDef stuff = null)
		{
			if (def == null || map == null)
			{
				return null;
			}

			if (!ModuleRegistry.IsEnabled("build-control"))
			{
				NotificationService.Message(
					"ColonyKit.Build.ModuleDisabled".Translate(),
					MessageTypeDefOf.RejectInput);
				return null;
			}

			AcceptanceReport report = CanPlace(def, center, map, rot, stuff);
			if (!report.Accepted)
			{
				NotificationService.Message(
					report.Reason.NullOrEmpty()
						? "ColonyKit.Build.Rejected".Translate()
						: report.Reason,
					MessageTypeDefOf.RejectInput);
				return null;
			}

			return GenConstruct.PlaceBlueprintForBuild(def, center, map, rot, Faction.OfPlayer, stuff);
		}

		/// <summary>供 Harmony Postfix 调用：在原版结果之上应用规则。</summary>
		internal static AcceptanceReport ApplyRules(
			BuildableDef def,
			IntVec3 center,
			Map map,
			Rot4 rot,
			ThingDef stuff,
			AcceptanceReport current)
		{
			if (!current.Accepted || rules.Count == 0 || !ModuleRegistry.IsEnabled("build-control"))
			{
				return current;
			}

			foreach (BuildRule rule in rules)
			{
				bool allowed;
				try
				{
					allowed = rule.Allow(def, center, map, rot, stuff);
				}
				catch (Exception exception)
				{
					ModLog.Error("Build", $"rule '{rule.Id}' failed", exception);
					continue;
				}

				if (allowed)
				{
					continue;
				}

				string reason = null;
				try
				{
					reason = rule.Reason?.Invoke(def, center, map, rot, stuff);
				}
				catch (Exception exception)
				{
					ModLog.Error("Build", $"rule '{rule.Id}' reason failed", exception);
				}

				return new AcceptanceReport(reason.NullOrEmpty()
					? "ColonyKit.Build.Rejected".Translate()
					: reason);
			}

			return current;
		}
	}
}
