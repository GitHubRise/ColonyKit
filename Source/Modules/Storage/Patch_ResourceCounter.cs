using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 把立体库内部库存计入原版资源列表（底部资源读条）。
	/// 原版 ResourceCounter 只统计 SlotGroup；仓库内部 ThingOwner 不在其中。
	/// </summary>
	[HarmonyPatch(typeof(ResourceCounter), nameof(ResourceCounter.UpdateResourceCounts))]
	internal static class Patch_ResourceCounter
	{
		private static readonly AccessTools.FieldRef<ResourceCounter, Map> MapRef =
			AccessTools.FieldRefAccess<ResourceCounter, Map>("map");

		private static void Postfix(ResourceCounter __instance)
		{
			Map map = MapRef(__instance);
			if (map == null)
			{
				return;
			}

			foreach (Building_VerticalWarehouse warehouse in map.listerBuildings
				.AllBuildingsColonistOfClass<Building_VerticalWarehouse>())
			{
				foreach (Thing stored in warehouse.Container)
				{
					Thing inner = stored.GetInnerIfMinified();
					if (inner == null || !inner.def.CountAsResource || inner.IsNotFresh())
					{
						continue;
					}

					if (inner.SpawnedOrAnyParentSpawned && inner.PositionHeld.Fogged(inner.MapHeld))
					{
						continue;
					}

					__instance.AllCountedAmounts.TryGetValue(inner.def, out int count);
					__instance.AllCountedAmounts[inner.def] = count + inner.stackCount;
				}
			}
		}
	}
}
