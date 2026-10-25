#nullable disable

﻿using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace rjw
{
	public static class DebugActions
	{
		[DebugAction("RJW", "Force Size Updates", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void ToggleForceUpdate()
		{
			HediffComp_GrowsWithOwner.ForceUpdate = !HediffComp_GrowsWithOwner.ForceUpdate;

			string status = HediffComp_GrowsWithOwner.ForceUpdate
				? "<color=green>Size Updated</color>"
				: "<color=red>disabled</color>";

			Messages.Message($"Debug {status}", MessageTypeDefOf.TaskCompletion);
		}

		[DebugAction("RJW", "Reset CanLovinTick", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ResetCanLovinTick(Pawn pawn)
		{
			if (!pawn.Dead) // 不必在已死亡的小人身上费心，这无关紧要，而且微粒生成器会抛出错误。
			{
				int currentTime = Find.TickManager.TicksGame;
				if (pawn.mindState.canLovinTick >= currentTime)
				{
					MoteMaker.ThrowText(pawn.TrueCenter(), pawn.Map, "canLovinTick was " + pawn.mindState.canLovinTick + ", curTick: " + currentTime + ", Tick Reset");
					pawn.mindState.canLovinTick = 0;
				}
				else
				{
					MoteMaker.ThrowText(pawn.TrueCenter(), pawn.Map, "Next Lovin Tick already passed");
				}
			}
		}

		[DebugAction("RJW", "Reset All Sex Cooldowns", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ResetSexCooldowns(Pawn pawn)
		{
			if (!pawn.Dead)
			{ 
				int currentTime = Find.TickManager.TicksGame;
				if (pawn.mindState.canLovinTick >= currentTime)
				{
					pawn.mindState.canLovinTick = 0;
				}
				Hediff cooldown = null;
				pawn.health.hediffSet.TryGetHediff(HediffDef.Named("Hediff_RapeEnemyCD"), out cooldown);
				if (cooldown != null)
				{
					pawn.health.RemoveHediff(cooldown);
				}

			
				MoteMaker.ThrowText(pawn.TrueCenter(), pawn.Map, "Cooldowns reset.");
			}
		}

		[DebugAction("RJW", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void RunFindPartner(Pawn pawn)
		{
			bool state = RJWSettings.DebugLogJoinInBed;
			RJWSettings.DebugLogJoinInBed = true;

			try
			{
				CasualSex_Helper.FindPartner(pawn);
			}
			catch (Exception e)
			{
				ModLog.Error($"Exception in CasualSex_Helper.FindPartner:\n{e}");
			}

			RJWSettings.DebugLogJoinInBed = state;
		}
	}
}
