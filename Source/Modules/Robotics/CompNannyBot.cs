using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	public class CompProperties_NannyBot : CompProperties
	{
		public CompProperties_NannyBot()
		{
			compClass = typeof(CompNannyBot);
		}
	}

	/// <summary>
	/// 保姆机器人的状态与指令：三项家务开关、母充电桩、充电按钮。
	/// 机器人是玩家阵营的机械族 Pawn，但没有 Work 页，默认自动找活。
	/// </summary>
	public class CompNannyBot : ThingComp
	{
		public bool sweep = true;
		public bool scrub = true;
		public bool haul = true;

		/// <summary>组装它的充电桩；桩被拆掉后会自动改用最近的桩。</summary>
		public Building homeStation;

		private Pawn Pawn => parent as Pawn;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref sweep, "ck_robotics_sweep", true);
			Scribe_Values.Look(ref scrub, "ck_robotics_scrub", true);
			Scribe_Values.Look(ref haul, "ck_robotics_haul", true);
			Scribe_References.Look(ref homeStation, "ck_robotics_homeStation");
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead || !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				yield break;
			}

			yield return Toggle(
				"CK.Robotics.Toggle.Sweep.Label".Translate(),
				"CK.Robotics.Toggle.Sweep.Desc".Translate(),
				"UI/Commands/CK_Sweep",
				() => sweep,
				() => sweep = !sweep);

			yield return Toggle(
				"CK.Robotics.Toggle.Scrub.Label".Translate(),
				"CK.Robotics.Toggle.Scrub.Desc".Translate(),
				"UI/Commands/CK_Scrub",
				() => scrub,
				() => scrub = !scrub);

			yield return Toggle(
				"CK.Robotics.Toggle.Haul.Label".Translate(),
				"CK.Robotics.Toggle.Haul.Desc".Translate(),
				"UI/Commands/CK_Haul",
				() => haul,
				() => haul = !haul);

			Building station = NannyBotUtility.FindStation(pawn, true);
			Command_Action charge = new Command_Action
			{
				defaultLabel = "CK.Robotics.Command.Charge".Translate(),
				defaultDesc = "CK.Robotics.Command.Charge.Desc".Translate(),
				icon = Icon("UI/Commands/CK_Charge"),
				action = () =>
				{
					Building target = NannyBotUtility.FindStation(pawn, true);
					if (target == null)
					{
						Messages.Message(
							"CK.Robotics.NoStation".Translate(),
							MessageTypeDefOf.RejectInput,
							false);
						return;
					}
					Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCharge, target);
					pawn.jobs.TryTakeOrderedJob(job);
				}
			};
			if (station == null)
			{
				charge.Disabled = true;
				charge.disabledReason = "CK.Robotics.NoStation".Translate();
			}
			yield return charge;
		}

		public override string CompInspectStringExtra()
		{
			Pawn pawn = Pawn;
			if (pawn == null || pawn.Dead)
			{
				return null;
			}

			List<string> lines = new List<string>();
			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (energy != null)
			{
				lines.Add("CK.Robotics.Inspect.Energy".Translate(energy.CurLevelPercentage.ToStringPercent()));
			}
			lines.Add("CK.Robotics.Inspect.Tasks".Translate(
				State(sweep),
				State(scrub),
				State(haul)));

			if (!sweep && !scrub && !haul)
			{
				lines.Add("CK.Robotics.Inspect.Idle".Translate());
			}
			if (pawn.Downed)
			{
				lines.Add("CK.Robotics.Inspect.Downed".Translate());
			}
			return string.Join("\n", lines);
		}

		private static string State(bool enabled)
		{
			return enabled
				? "CK.Robotics.On".Translate()
				: "CK.Robotics.Off".Translate();
		}

		private static Command_Toggle Toggle(
			string label,
			string description,
			string iconPath,
			System.Func<bool> active,
			System.Action toggle)
		{
			return new Command_Toggle
			{
				defaultLabel = label,
				defaultDesc = description,
				icon = Icon(iconPath),
				isActive = active,
				toggleAction = toggle
			};
		}

		private static Texture2D Icon(string path)
		{
			return ContentFinder<Texture2D>.Get(path, false) ?? TexCommand.GatherSpotActive;
		}
	}
}
