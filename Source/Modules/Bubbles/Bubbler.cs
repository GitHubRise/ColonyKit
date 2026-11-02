using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 气泡的收集与绘制：监听 <see cref="PlayLog"/>，把社交互动转成挂在发起者身上的气泡。
	/// </summary>
	/// <remarks>
	/// 生命周期完全跟随游戏：<see cref="Add"/> 在 <c>Verse.PlayLog.Add</c> 的 Postfix 里调用，
	/// <see cref="Draw"/> 在 <c>MapInterface.MapInterfaceOnGUI_BeforeMainTabs</c> 里调用，
	/// 读档/换图/关闭模块时由 <see cref="BubblesModule"/> 调 <see cref="Clear"/>。
	/// 气泡不进存档，重启游戏就没了。
	/// </remarks>
	internal static class Bubbler
	{
		/// <summary>气泡相对小人标签的纵向偏移，和原 Mod 保持一致。</summary>
		private const float LabelPositionOffset = -0.6f;

		private static readonly Dictionary<Pawn, List<Bubble>> Bubbles =
			new Dictionary<Pawn, List<Bubble>>();

		/// <summary>记录一条互动；不是社交互动、或当前条件不满足时直接忽略。</summary>
		public static void Add(LogEntry entry)
		{
			try
			{
				BubblesSettings settings = BubblesSettingsUtility.Current;
				if (entry == null || !BubblesFeature.Active || !CanRender(settings))
				{
					return;
				}

				GetActors(entry, out Pawn initiator, out Pawn recipient);
				if (initiator == null || initiator.Map == null || initiator.Map != Find.CurrentMap)
				{
					return;
				}

				bool playerInitiated = initiator.Faction != null && initiator.Faction.IsPlayer;

				// 非玩家发起的互动（或没有阵营的动物）先过听觉判定，再由「显示其他派系」决定。
				if (!playerInitiated && !CanBeHeard(initiator, settings))
				{
					return;
				}

				if (!settings.doNonPlayer && !playerInitiated)
				{
					return;
				}

				if (!settings.doAnimals && (IsAnimal(initiator) || IsAnimal(recipient)))
				{
					return;
				}

				if (!settings.doDrafted && (IsDrafted(initiator) || IsDrafted(recipient)))
				{
					return;
				}

				if (!Bubbles.ContainsKey(initiator))
				{
					Bubbles[initiator] = new List<Bubble>();
				}

				Bubbles[initiator].Add(new Bubble(initiator, entry));
			}
			catch (Exception exception)
			{
				BubblesFeature.Fail(exception);
			}
		}

		/// <summary>每帧在地图 UI 上画所有气泡；由 MapInterface 补丁调用。</summary>
		public static void Draw()
		{
			if (!BubblesFeature.Active || Bubbles.Count == 0)
			{
				return;
			}

			BubblesSettings settings = BubblesSettingsUtility.Current;
			if (settings == null || !CanRender(settings))
			{
				return;
			}

			try
			{
				float altitude = GetAltitude();
				if (altitude <= 0f || altitude > settings.altitudeMax)
				{
					return;
				}

				float scale = settings.altitudeBase / altitude;
				if (scale > settings.scaleMax)
				{
					scale = settings.scaleMax;
				}

				Pawn selected = Find.Selector?.SingleSelectedObject as Pawn;
				Pawn[] pawns = Bubbles
					.Keys
					.OrderBy(pawn => pawn == selected)
					.ThenBy(pawn => pawn.Position.y)
					.ToArray();

				foreach (Pawn pawn in pawns)
				{
					DrawBubble(pawn, pawn == selected, scale, settings);
				}
			}
			catch (Exception exception)
			{
				BubblesFeature.Fail(exception);
			}
		}

		/// <summary>清空全部气泡（读档、换图、关闭功能时调用）。</summary>
		public static void Clear()
		{
			Bubbles.Clear();
		}

		/// <summary>『显示彩色文字』切换后重算所有气泡的文本缓存。</summary>
		public static void Rebuild()
		{
			foreach (List<Bubble> list in Bubbles.Values)
			{
				foreach (Bubble bubble in list)
				{
					bubble.Rebuild();
				}
			}
		}

		private static void DrawBubble(Pawn pawn, bool isSelected, float scale, BubblesSettings settings)
		{
			Map map = Find.CurrentMap;
			if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map != map || map == null
				|| map.fogGrid == null || map.fogGrid.IsFogged(pawn.Position))
			{
				// 小人离图/死亡后不再绘制；顺手把它的气泡清掉，避免字典越积越大。
				Bubbles.Remove(pawn);
				return;
			}

			if (!Bubbles.TryGetValue(pawn, out List<Bubble> list) || list.Count == 0)
			{
				Bubbles.Remove(pawn);
				return;
			}

			Vector2 position = GenMapUI.LabelDrawPosFor(pawn, LabelPositionOffset);
			Rot4 direction = BubblesFeature.Direction(settings);
			Vector2 axis = direction.AsVector2;
			float offset = settings.offsetStart;
			int count = 0;

			foreach (Bubble bubble in list.OrderByDescending(item => item.Entry.Tick).ToArray())
			{
				if (count > settings.pawnMax)
				{
					break;
				}

				Vector2 point = new Vector2(position.x + offset * axis.x, position.y + offset * axis.y);
				if (!bubble.Draw(point, isSelected, scale, settings))
				{
					list.Remove(bubble);
				}

				offset += (direction.IsHorizontal ? bubble.Width : bubble.Height) + settings.offsetSpacing;
				count++;
			}

			if (list.Count == 0)
			{
				Bubbles.Remove(pawn);
			}
		}

		private static bool CanRender(BubblesSettings settings)
		{
			if (settings == null || !settings.enabled || !settings.activated)
			{
				return false;
			}

			try
			{
				WorldRenderMode mode = WorldRendererUtility.CurrentWorldRenderMode;
				if (mode != WorldRenderMode.None && mode != WorldRenderMode.Background)
				{
					// 大地图/选点界面不画。
					return false;
				}
			}
			catch
			{
				return false;
			}

			if (settings.autoHideSpeed <= BubblesSettings.AutoHideSpeedOff)
			{
				return true;
			}

			return Find.TickManager == null
				|| (int)Find.TickManager.CurTimeSpeed < settings.autoHideSpeed;
		}

		private static bool CanBeHeard(Pawn pawn, BubblesSettings settings)
		{
			if (!settings.hearingCheck)
			{
				return true;
			}

			Map map = pawn.Map;
			if (map == null || map.mapPawns == null)
			{
				return false;
			}

			foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
			{
				float hearing = colonist.health.capacities.GetLevel(PawnCapacityDefOf.Hearing);
				if (hearing <= 0f)
				{
					continue;
				}

				if (colonist.Position.InHorDistOf(pawn.Position, settings.hearingRange * hearing))
				{
					return true;
				}
			}

			return false;
		}

		private static void GetActors(LogEntry entry, out Pawn initiator, out Pawn recipient)
		{
			initiator = null;
			recipient = null;

			if (entry is PlayLogEntry_Interaction interaction)
			{
				initiator = BubblesReflection.PlayLogEntryInteractionInitiator?.GetValue(interaction) as Pawn;
				recipient = BubblesReflection.PlayLogEntryInteractionRecipient?.GetValue(interaction) as Pawn;
			}
			else if (entry is PlayLogEntry_InteractionSinglePawn single)
			{
				initiator = BubblesReflection.PlayLogEntryInteractionSinglePawnInitiator?.GetValue(single) as Pawn;
			}
		}

		private static bool IsAnimal(Pawn pawn)
		{
			return pawn != null && pawn.RaceProps != null && pawn.RaceProps.Animal;
		}

		private static bool IsDrafted(Pawn pawn)
		{
			return pawn != null && pawn.drafter != null && pawn.drafter.Drafted;
		}

		private static float GetAltitude()
		{
			if (Find.CameraDriver == null || BubblesReflection.CameraDriverRootSize == null)
			{
				return 0f;
			}

			float altitude = Mathf.Max(1f, (float)BubblesReflection.CameraDriverRootSize.GetValue(Find.CameraDriver));
			BubblesCompat.Apply(ref altitude);
			return altitude;
		}
	}
}
