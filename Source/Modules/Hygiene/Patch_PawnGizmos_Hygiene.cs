using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>手动「去洗澡」入口：即使自动 ThinkTree 未命中也能测试/使用。</summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
	internal static class Patch_PawnGizmos_Hygiene
	{
		private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
		{
			foreach (Gizmo gizmo in __result)
			{
				yield return gizmo;
			}

			if (!__instance.IsColonistPlayerControlled
				|| !ModuleRegistry.IsEnabled("hygiene")
				|| __instance.Map == null)
			{
				yield break;
			}

			NeedDef needDef = DefDatabase<NeedDef>.GetNamedSilentFail("CK_Hygiene");
			if (needDef == null)
			{
				yield break;
			}
			if (__instance.needs?.TryGetNeed<Need_Hygiene>() == null)
			{
				Need need = (Need)Activator.CreateInstance(needDef.needClass, __instance);
				need.def = needDef;
				__instance.needs.AllNeeds.Add(need);
				need.SetInitialLevel();
			}

			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.TakeShower".Translate(),
				defaultDesc = "CK.Hygiene.TakeShowerDesc".Translate(),
				action = () =>
				{
					Building_Shower shower = FindShower(__instance);
					if (shower == null)
					{
						Messages.Message("CK.Hygiene.NoShower".Translate(), MessageTypeDefOf.RejectInput, false);
						return;
					}
					JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeShower");
					if (jobDef != null)
					{
						__instance.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef, shower), JobTag.Misc);
					}
				}
			};
		}

		private static Building_Shower FindShower(Pawn pawn)
		{
			Building_Shower best = null;
			float bestDistance = float.MaxValue;
			foreach (Building_Shower shower in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Shower>())
			{
				if (!shower.CanUse(pawn) || shower.IsForbidden(pawn))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(shower.Position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = shower;
				}
			}
			return best;
		}
	}
}
