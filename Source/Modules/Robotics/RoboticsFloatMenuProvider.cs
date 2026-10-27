using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>机器人右键菜单：选中机器人后右键充电桩/自己，或选中殖民者后右键受损机器人。</summary>
	public sealed class RoboticsFloatMenuProvider : FloatMenuOptionProvider
	{
		protected override bool Drafted => true;

		protected override bool Undrafted => true;

		protected override bool Multiselect => false;

		protected override bool MechanoidCanDo => true;

		protected override bool CanSelfTarget => true;

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
		{
			Pawn selected = context.FirstSelectedPawn;
			if (selected == null)
			{
				yield break;
			}

			if (NannyBotUtility.IsMkV(selected)
				&& clickedThing is ThingWithComps weapon
				&& weapon.def.IsWeapon
				&& !weapon.IsForbidden(selected)
				&& selected.CanReserve(weapon)
				&& EquipmentUtility.CanEquip(weapon, selected))
			{
				yield return new FloatMenuOption(
					"CK.Robotics.Menu.EquipWeapon".Translate(weapon.LabelShort),
					() => OrderEquip(selected, weapon));
			}

			if (!NannyBotUtility.IsNannyBot(selected)
				|| !(clickedThing is Building building)
				|| building.def != NannyBotDefOf.CK_NannyBotStation)
			{
				yield break;
			}

			yield return new FloatMenuOption(
				"CK.Robotics.Menu.ChargeAtStation".Translate(),
				() => OrderCharge(selected, building));
		}

		public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
		{
			Pawn selected = context.FirstSelectedPawn;
			if (selected == null || clickedPawn == null)
			{
				yield break;
			}

			if (RjwCompat.Loaded && NannyBotUtility.IsMkV(clickedPawn) && selected != clickedPawn)
			{
				foreach (FloatMenuOption option in GetRjwSexOptions(selected, clickedPawn))
				{
					yield return option;
				}
			}

			if (selected == clickedPawn && NannyBotUtility.IsNannyBot(selected))
			{
				Building station = NannyBotUtility.FindStation(selected, false);
				yield return new FloatMenuOption(
					"CK.Robotics.Menu.ChargeAtStation".Translate(),
					() => OrderCharge(selected, station));

				CompMkVCombat combat = selected.TryGetComp<CompMkVCombat>();
				if (combat?.HomeBay != null)
				{
					yield return new FloatMenuOption(
						"CK.Robotics.Menu.ReturnToBay".Translate(),
						() => OrderReturn(selected, combat.HomeBay));
				}
				yield break;
			}

			CompNannyBot robotComp = clickedPawn.TryGetComp<CompNannyBot>();
			if (selected.IsColonist && robotComp != null
				&& robotComp.Durability < robotComp.MaxDurability - 0.5f)
			{
				yield return new FloatMenuOption(
					"CK.Robotics.Menu.RepairRobot".Translate(clickedPawn.LabelShort),
					() => OrderRepair(selected, clickedPawn));
			}
		}

		private static IEnumerable<FloatMenuOption> GetRjwSexOptions(Pawn initiator, Pawn target)
		{
			IEnumerable<FloatMenuOption> options = null;
			try
			{
				Type rmbSex = Type.GetType("rjw.RMB.RMB_Sex, RJW");
				MethodInfo getOptions = rmbSex?.GetMethod(
					"GetOptions",
					BindingFlags.Public | BindingFlags.Static);
				options = getOptions?.Invoke(null, new object[] { initiator, target }) as IEnumerable<FloatMenuOption>;
			}
			catch (Exception exception)
			{
				Log.Warning($"[ColonyKit] RJW sex menu failed: {exception.Message}");
			}
			if (options == null)
			{
				yield break;
			}
			foreach (FloatMenuOption option in options)
			{
				yield return option;
			}
		}

		private static void OrderEquip(Pawn robot, ThingWithComps weapon)
		{
			if (robot == null || weapon == null || !weapon.Spawned)
			{
				return;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Equip, weapon);
			robot.jobs.TryTakeOrderedJob(job);
		}

		private static void OrderCharge(Pawn robot, Building station)
		{
			if (robot == null || station == null)
			{
				Messages.Message("CK.Robotics.Menu.NoStation".Translate(),
					MessageTypeDefOf.RejectInput, false);
				return;
			}
			if (robot.Drafted && robot.drafter != null)
			{
				robot.drafter.Drafted = false;
			}
			Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCharge, station);
			robot.jobs.TryTakeOrderedJob(job);
		}

		private static void OrderReturn(Pawn robot, Building bay)
		{
			if (robot == null || bay == null || !bay.Spawned)
			{
				return;
			}
			if (robot.Drafted && robot.drafter != null)
			{
				robot.drafter.Drafted = false;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Goto, bay.InteractionCell);
			job.locomotionUrgency = LocomotionUrgency.Jog;
			robot.jobs.TryTakeOrderedJob(job);
		}

		private static void OrderRepair(Pawn colonist, Pawn robot)
		{
			if (colonist == null || robot == null)
			{
				return;
			}
			Job job = JobMaker.MakeJob(NannyBotDefOf.CK_RepairNannyBot, robot);
			colonist.jobs.TryTakeOrderedJob(job);
		}
	}
}
