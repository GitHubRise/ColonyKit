using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	public class CompProperties_NannyBotStation : CompProperties
	{
		/// <summary>待机功耗（W）；充电时切到 <see cref="chargePower"/>。</summary>
		public float idlePower = 50f;

		/// <summary>充电功耗（W）；默认 1000W，可在设置页 100–10000W 调整。</summary>
		public float chargePower = 1000f;

		/// <summary>机器人损毁后，重新组装的冷却 tick（默认 1 天）。</summary>
		public int rebuildCooldownTicks = 60000;

		public CompProperties_NannyBotStation()
		{
			compClass = typeof(CompNannyBotStation);
		}
	}

	/// <summary>
	/// 保姆机器人充电桩：建成后自动组装一台机器人，之后负责充电；
	/// 机器人损毁后可在冷却结束时重新组装。
	/// </summary>
	public class CompNannyBotStation : ThingComp
	{
		public Pawn robot;

		private int nextBuildTick;
		private bool spawnPending;
		private bool charging;
		private CompPowerTrader power;
		private CompGlower glower;

		private CompProperties_NannyBotStation Props =>
			(CompProperties_NannyBotStation)props;

		private bool HasRobot => robot != null && !robot.Dead && !robot.Destroyed && robot.Spawned;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.TryGetComp<CompPowerTrader>();
			glower = parent.TryGetComp<CompGlower>();
			if (!respawningAfterLoad)
			{
				spawnPending = true;
			}
			ApplyPower();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_References.Look(ref robot, "ck_robotics_stationRobot");
			Scribe_Values.Look(ref nextBuildTick, "ck_robotics_nextBuildTick", 0);
		}

		public override void CompTick()
		{
			if (!parent.IsHashIntervalTick(15))
			{
				return;
			}

			if (spawnPending)
			{
				spawnPending = false;
				if (!HasRobot)
				{
					TrySpawnRobot(false);
				}
			}

			if (robot != null && !HasRobot)
			{
				NotifyRobotLost();
			}

			if (charging && !IsRobotChargingHere())
			{
				SetCharging(false);
			}
			else
			{
				ApplyPower();
			}
			glower?.UpdateLit(parent.Map);
		}

		private void NotifyRobotLost()
		{
			robot = null;
			SetCharging(false);
			int now = Find.TickManager.TicksGame;
			if (nextBuildTick < now)
			{
				nextBuildTick = now + Props.rebuildCooldownTicks;
			}
		}

		public bool IsCharging => charging;

		public void SetCharging(bool value)
		{
			if (charging == value)
			{
				ApplyPower();
				glower?.UpdateLit(parent.Map);
				return;
			}
			charging = value;
			ApplyPower();
			glower?.UpdateLit(parent.Map);
		}

		private bool IsRobotChargingHere()
		{
			return robot != null
				&& robot.Spawned
				&& robot.CurJob != null
				&& robot.CurJob.def == NannyBotDefOf.CK_NannyCharge
				&& robot.Position.DistanceTo(parent.InteractionCell) <= 2f;
		}

		private void ApplyPower()
		{
			if (power == null)
			{
				return;
			}
			RoboticsSettings settings = NannyBotUtility.Settings;
			float chargePower = NannyBotUtility.IsMkV(robot)
				? settings.mkvChargePower
				: settings.chargePower;
			float desired = -(charging ? chargePower : Props.idlePower);
			if (!Mathf.Approximately(power.PowerOutput, desired))
			{
				power.PowerOutput = desired;
			}
		}

		public bool TrySpawnRobot(bool manual = true)
		{
			if (!ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return false;
			}
			if (parent.Map == null || !parent.Spawned)
			{
				return false;
			}
			if (HasRobot)
			{
				if (manual)
				{
					Messages.Message(
						"CK.Robotics.Station.HasRobot".Translate(),
						MessageTypeDefOf.RejectInput,
						false);
				}
				return false;
			}

			int now = Find.TickManager.TicksGame;
			if (manual && now < nextBuildTick)
			{
				Messages.Message(
					"CK.Robotics.Station.Cooldown".Translate(
						(nextBuildTick - now).ToStringTicksToPeriod()),
					MessageTypeDefOf.RejectInput,
					false);
				return false;
			}

			PawnKindDef kind = NannyBotDefOf.CK_RobotNannyKind;
			if (kind == null)
			{
				ModLog.Error("Robotics", "CK_RobotNannyKind missing; cannot assemble a nanny robot.");
				return false;
			}

			Pawn pawn = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer);
			if (pawn == null)
			{
				ModLog.Error("Robotics", $"PawnGenerator returned null for {kind.defName}.");
				return false;
			}

			IntVec3 cell = CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, 3);
			if (!cell.IsValid || !cell.InBounds(parent.Map))
			{
				cell = parent.InteractionCell;
			}

			int number = 1;
			foreach (Pawn other in parent.Map.mapPawns.AllPawnsSpawned)
			{
				if (NannyBotUtility.IsNannyBot(other))
				{
					number++;
				}
			}

			pawn.Name = new NameSingle(
				"CK.Robotics.RobotName".Translate(number).ToString(),
				true);
			if (pawn.playerSettings == null)
			{
				pawn.playerSettings = new Pawn_PlayerSettings(pawn);
			}

			GenSpawn.Spawn(pawn, cell, parent.Map, Rot4.South);
			CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
			if (comp != null)
			{
				comp.homeStation = parent as Building;
			}

			robot = pawn;
			nextBuildTick = now + Props.rebuildCooldownTicks;
			Messages.Message(
				"CK.Robotics.Station.Assembled".Translate(pawn.LabelShort),
				pawn,
				MessageTypeDefOf.PositiveEvent);
			return true;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (!ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId) || !parent.Spawned)
			{
				yield break;
			}

			Command_Action assemble = new Command_Action
			{
				defaultLabel = "CK.Robotics.Station.Assemble".Translate(),
				defaultDesc = "CK.Robotics.Station.Assemble.Desc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/CK_Assemble", false)
					?? TexCommand.Install,
				action = () => TrySpawnRobot(true)
			};

			if (HasRobot)
			{
				assemble.Disabled = true;
				assemble.disabledReason = "CK.Robotics.Station.HasRobot".Translate();
			}
			else
			{
				int now = Find.TickManager.TicksGame;
				if (now < nextBuildTick)
				{
					assemble.Disabled = true;
					assemble.disabledReason = "CK.Robotics.Station.Cooldown".Translate(
						(nextBuildTick - now).ToStringTicksToPeriod());
				}
			}
			yield return assemble;

			if (HasRobot)
			{
				yield return new Command_Action
				{
					defaultLabel = "CK.Robotics.Station.Locate".Translate(),
					defaultDesc = "CK.Robotics.Station.Locate.Desc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/CK_Goto", false)
						?? TexCommand.SelectCarriedPawn,
					action = () => CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(robot))
				};

				yield return new Command_Action
				{
					defaultLabel = "CK.Robotics.Station.Recall".Translate(),
					defaultDesc = "CK.Robotics.Station.Recall.Desc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/CK_Recall", false)
						?? TexCommand.SelectCarriedPawn,
					action = () =>
					{
						Job job = JobMaker.MakeJob(JobDefOf.Goto, parent.InteractionCell);
						job.locomotionUrgency = LocomotionUrgency.Jog;
						robot.jobs.TryTakeOrderedJob(job);
					}
				};
			}
		}

		public override string CompInspectStringExtra()
		{
			List<string> lines = new List<string>();
			if (power == null || !power.PowerOn)
			{
				lines.Add("CK.Robotics.Station.NoPower".Translate());
			}

			if (HasRobot)
			{
				lines.Add("CK.Robotics.Station.RobotOnline".Translate());
			}
			else
			{
				int now = Find.TickManager.TicksGame;
				if (now < nextBuildTick)
				{
					lines.Add("CK.Robotics.Station.RebuildIn".Translate(
						(nextBuildTick - now).ToStringTicksToPeriod()));
				}
				else
				{
					lines.Add("CK.Robotics.Station.Ready".Translate());
				}
			}

			return lines.Count == 0 ? null : string.Join("\n", lines);
		}
	}
}
