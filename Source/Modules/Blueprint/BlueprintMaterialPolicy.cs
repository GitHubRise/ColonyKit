using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>一条材料替换记录：原材质 → 实际使用的材质。</summary>
	public sealed class BlueprintSubstitution
	{
		public ThingDef source;
		public ThingDef target;
	}

	/// <summary>
	/// 蓝图材料选择：把「捕获时的材质」翻译成「现在能用的材质」。
	/// </summary>
	/// <remarks>
	/// 逐个建筑调用，结果只和 <see cref="ThingDef"/> + 策略 + 地图库存有关，不写保存数据。
	/// 候选材质列表按建筑 Def 缓存，避免每次鼠标移动都扫描全部 ThingDef。
	/// </remarks>
	public static class BlueprintMaterialPolicy
	{
		private static readonly Dictionary<ThingDef, List<ThingDef>> candidateCache =
			new Dictionary<ThingDef, List<ThingDef>>();

		/// <summary>清空候选缓存；Def 重新加载/换局时调用。</summary>
		public static void ResetCache()
		{
			candidateCache.Clear();
		}

		/// <summary>
		/// 为 <paramref name="def"/> 选一种材料；<paramref name="def"/> 不需要材料时返回 null。
		/// </summary>
		public static ThingDef Resolve(
			ThingDef def,
			string sourceStuffName,
			BlueprintMaterialMode mode,
			Map map)
		{
			if (def == null || !def.MadeFromStuff)
			{
				return null;
			}

			ThingDef source = sourceStuffName.NullOrEmpty()
				? null
				: DefDatabase<ThingDef>.GetNamedSilentFail(sourceStuffName);

			bool sourceUsable = IsUsable(source, def);

			switch (mode)
			{
				case BlueprintMaterialMode.PreferSource:
					if (sourceUsable)
					{
						return source;
					}
					return PickCheapest(def, map, requireStock: true)
						?? PickCheapest(def, map, requireStock: false)
						?? (sourceUsable ? source : null);

				case BlueprintMaterialMode.Available:
					if (sourceUsable && CountOnMap(map, source) > 0)
					{
						return source;
					}
					return PickCheapest(def, map, requireStock: true)
						?? PickCheapest(def, map, requireStock: false)
						?? (sourceUsable ? source : null);

				case BlueprintMaterialMode.Cheapest:
					return PickCheapest(def, map, requireStock: false)
						?? (sourceUsable ? source : null);

				default:
					return sourceUsable ? source : PickCheapest(def, map, requireStock: false);
			}
		}

		/// <summary>该材质能否用于这个建筑（材质类别匹配）。</summary>
		public static bool IsUsable(ThingDef stuff, ThingDef def)
		{
			return stuff != null
				&& stuff.IsStuff
				&& stuff.stuffProps != null
				&& def != null
				&& stuff.stuffProps.CanMake(def);
		}

		/// <summary>地图上该材质的库存（不含已预约/在途）。</summary>
		public static int CountOnMap(Map map, ThingDef thingDef)
		{
			if (map?.resourceCounter == null || thingDef == null)
			{
				return 0;
			}
			return map.resourceCounter.GetCount(thingDef);
		}

		private static ThingDef PickCheapest(ThingDef def, Map map, bool requireStock)
		{
			List<ThingDef> candidates = Candidates(def);
			if (candidates.Count == 0)
			{
				return null;
			}

			IEnumerable<ThingDef> pool = candidates;
			if (requireStock)
			{
				int need = System.Math.Max(1, def.CostStuffCount);
				pool = candidates.Where(stuff => CountOnMap(map, stuff) >= need);
				if (!pool.Any())
				{
					// 库存不足时不要直接放弃：退到「有任何库存」的材料，至少能先开工。
					pool = candidates.Where(stuff => CountOnMap(map, stuff) > 0);
				}
			}

			return pool
				.OrderBy(stuff => stuff.BaseMarketValue)
				.ThenByDescending(stuff => stuff.stuffProps.commonality)
				.ThenBy(stuff => stuff.defName)
				.FirstOrDefault();
		}

		private static List<ThingDef> Candidates(ThingDef def)
		{
			if (candidateCache.TryGetValue(def, out List<ThingDef> cached))
			{
				return cached;
			}

			List<ThingDef> all = DefDatabase<ThingDef>.AllDefs
				.Where(stuff => IsUsable(stuff, def))
				.ToList();

			// 原版「推荐材料」（木头/钢铁/石块等）优先；没有推荐项时才允许皮革/布料等。
			List<ThingDef> suggested = all
				.Where(stuff => stuff.stuffProps.canSuggestUseDefaultStuff)
				.ToList();
			if (suggested.Count > 0)
			{
				all = suggested;
			}

			candidateCache[def] = all;
			return all;
		}
	}
}
