using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 右键小人菜单的统一注册表。外部只注册「条件 + 生成选项」，由
	/// <see cref="ColonyKitPawnMenuProvider"/> 交给原版 <c>FloatMenuMakerMap</c>。
	/// </summary>
	/// <remarks>
	/// 原版通过 <c>typeof(FloatMenuOptionProvider).AllSubclassesNonAbstract()</c> 自动发现 Provider，
	/// 所以这里不需要 Harmony；新增选项也不会改原版代码。
	/// </remarks>
	public static class PawnMenuService
	{
		private sealed class Entry
		{
			public string Id;
			public Func<Pawn, bool> Applies;
			public Func<Pawn, FloatMenuOption> Create;
			public int Priority;
		}

		private static readonly List<Entry> entries = new List<Entry>();

		static PawnMenuService()
		{
			Register(
				"demo.log-pawn",
				pawn => new FloatMenuOption(
					"ColonyKit.Menu.LogPawn".Translate(),
					() =>
					{
						ModLog.Info("Menu", $"menu: {pawn.LabelShort} ({pawn.def.defName})");
						NotificationService.ThrottledMessage(
							"menu.log-pawn",
							"ColonyKit.Notification.PawnLogged".Translate(pawn.LabelShort));
					}),
				pawn => pawn.IsColonist,
				priority: 100);

			Register(
				"demo.build-wall",
				pawn => new FloatMenuOption(
					"ColonyKit.Menu.BuildWall".Translate(),
					() =>
					{
						Map map = pawn.Map;
						if (map == null)
						{
							return;
						}

						IntVec3 cell = pawn.Position + IntVec3.East;
						if (!cell.InBounds(map))
						{
							NotificationService.Message(
								"ColonyKit.Build.OutOfBounds".Translate(),
								MessageTypeDefOf.RejectInput);
							return;
						}

						BuildControlService.PlaceBlueprint(
							ThingDefOf.Wall,
							cell,
							map,
							Rot4.North,
							ThingDefOf.Steel);
					}),
				pawn => pawn.IsColonist && pawn.Map != null,
				priority: 90);
		}

		public static int Count => entries.Count;

		/// <summary>
		/// 注册一个菜单项。同 Id 会覆盖；priority 大的排前面。
		/// </summary>
		public static void Register(
			string id,
			Func<Pawn, FloatMenuOption> create,
			Func<Pawn, bool> applies = null,
			int priority = 0)
		{
			if (id.NullOrEmpty() || create == null)
			{
				return;
			}

			entries.RemoveAll(entry => entry.Id == id);
			entries.Add(new Entry
			{
				Id = id,
				Create = create,
				Applies = applies,
				Priority = priority
			});
			entries.Sort((left, right) => right.Priority.CompareTo(left.Priority));
		}

		/// <summary>取消注册。</summary>
		public static bool Unregister(string id)
		{
			return entries.RemoveAll(entry => entry.Id == id) > 0;
		}

		/// <summary>按设置与条件生成当前小人的菜单项。</summary>
		public static IEnumerable<FloatMenuOption> GetOptions(Pawn pawn)
		{
			if (pawn == null
				|| !ModuleRegistry.IsEnabled("pawn-menu")
				|| !(ColonyKitMod.Settings?.general?.enablePawnMenu ?? true))
			{
				yield break;
			}

			foreach (Entry entry in entries.ToList())
			{
				if (entry.Applies != null)
				{
					bool applies;
					try
					{
						applies = entry.Applies(pawn);
					}
					catch (Exception exception)
					{
						ModLog.Error("Menu", $"pawn menu '{entry.Id}' applies failed", exception);
						continue;
					}
					if (!applies)
					{
						continue;
					}
				}

				FloatMenuOption option = null;
				try
				{
					option = entry.Create(pawn);
				}
				catch (Exception exception)
				{
					ModLog.Error("Menu", $"pawn menu '{entry.Id}' create failed", exception);
				}

				if (option != null)
				{
					yield return option;
				}
			}
		}
	}
}
