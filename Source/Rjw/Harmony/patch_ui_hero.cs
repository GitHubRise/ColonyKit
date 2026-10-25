#nullable enable

using Verse;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace rjw
{
	/// <summary>
	/// 为英雄模式修补界面
	/// - 对非自有英雄禁用小人控制
	/// - 对非自有英雄禁用装备管理
	/// 硬核模式：
	/// - 对非英雄禁用装备管理
	/// - 对非英雄禁用小人右键菜单
	/// - 移除非英雄的征召控件
	/// </summary>

	//禁用强制工作（右键工作分配器）
	[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ShouldGenerateFloatMenuForPawn))]
	[StaticConstructorOnStartup]
	static class Patch_Disable_FloatMenuMakerMap
	{
		[HarmonyPostfix]
		static void NonHero_disable_controls(ref AcceptanceReport __result, Pawn pawn)
		{
			if (!__result.Accepted)
			{
				return;
			}

			if (!RJWSettings.RPG_hero_control)
			{
				return;
			}

			if (pawn.IsDesignatedHero() && !pawn.IsHeroOwner())
			{
				__result = "ForHeroRefuse1Desc".Translate();   //不是英雄所有者，禁用菜单
				return;
			}

			if (!pawn.IsDesignatedHero() && RJWSettings.RPG_hero_control_HC)
			{
				if (pawn.Drafted && pawn.CanChangeDesignationPrisoner() && pawn.CanChangeDesignationColonist())
				{
					//允许控制已征召的小人，但这受下面的 disable_Gizmos 补丁限制
				}
				else
				{
					__result = "ForHeroRefuseHCDesc".Translate(); //不是英雄，禁用菜单
				}
			}
		}
	}

	//TODO：禁用装备管理
	/*
	//disable equipment management
	[HarmonyPatch(typeof(ITab_Pawn_Gear), "CanControl")]
	static class disable_equipment_management
	{
		[HarmonyPostfix]
		static bool this_is_postfix(ref bool __result, Pawn selPawnForGear)
		{
			Pawn pawn = selPawnForGear;

			if (RJWSettings.RPG_hero_control)
			{
				if ((pawn.IsDesignatedHero() && !pawn.IsHeroOwner()))	//not hero owner, disable drafting
				{
					__result = false;   //not hero owner, disable menu
				}
				else if (!pawn.IsDesignatedHero() && RJWSettings.RPG_hero_control_HC)   //not hero, disable drafting
				{
					if (false)
					{
						//add some filter for bots and stuff? if there is such stuff
						//so it can be drafted and controlled for fighting
					}
					else
					{
						__result = false; //not hero, disable menu
					}
				}
			}
			return true;
		}
	}
	*/

	//TODO：允许共享控制非殖民者（机器人等）？
	//禁用命令按钮
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	[StaticConstructorOnStartup]
	static class disable_Gizmos
	{
		[HarmonyPostfix]
		[HarmonyPriority(100)]
		static IEnumerable<Gizmo> NonHero_disable_gizmos(IEnumerable<Gizmo> __result, Pawn __instance)
		{
			
			Pawn pawn = __instance;
			string disablementReason = string.Empty; 

			if (RJWSettings.RPG_hero_control)
			{
				if ((pawn.IsDesignatedHero() && !pawn.IsHeroOwner()))	//不是英雄所有者，禁用征召
				{
					disablementReason = "ForHeroRefuse1Desc";
				}
				else if (!pawn.IsDesignatedHero() && RJWSettings.RPG_hero_control_HC)   //不是英雄，禁用征召
				{
					//无权为非囚犯英雄／其他玩家更改 designation
					if (pawn.CanChangeDesignationPrisoner() && pawn.CanChangeDesignationColonist()
							&& (pawn.kindDef.race.defName.Contains("AIRobot")
							|| (pawn.kindDef.race.defName.Contains("Droid") && !pawn.kindDef.race.defName.Contains("AndDroid"))
							|| pawn.kindDef.race.defName.Contains("RPP_Bot")
							))
					//if (false) 
					{
						//要不要给机器人之类加些过滤？如果有这种东西的话
						//这样它就能被征召并操控去战斗
					}
					else
					{
						disablementReason = "ForHeroRefuseHCDesc";
					}
				}
			}

			foreach (var gizmo in __result)
			{
				if ( disablementReason.NullOrEmpty() || (!(gizmo is Command)) )  //我们不过滤非命令类按钮，比如护盾条或心灵熵
				{
					yield return gizmo;
				}

				//ModLog.Message("Gizmo for " + xxx.get_pawnname(__instance) + " type: " + gizmo.GetType()+ ": " + gizmo);

				if (!disablementReason.NullOrEmpty())
				{
					if (gizmo is Verse.Command_VerbTarget) { //武器图标 
						gizmo.Disable(disablementReason.Translate());
						yield return gizmo; 
					}
				}
				//其他所有命令按钮都被丢弃
				
			}	
		}
	}
}
