using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人当前任务的显示名。原版 JobDef 基本只写了 reportString，没有 label，
	/// 直接显示 def.label 会得到 "HaulToCell" / "CK_NannyCleanFilth" 这类内部名；
	/// 这里统一换成玩家看得懂的任务名，并附上目标物品/小人。
	/// </summary>
	internal static class NannyBotJobUtility
	{
		public static string TaskText(Pawn pawn)
		{
			Job job = pawn?.CurJob;
			if (job?.def == null)
			{
				return "CK.Robotics.Task.Idle".Translate();
			}

			string label = Label(job.def);
			string target = TargetLabel(job);
			return target.NullOrEmpty()
				? label
				: "CK.Robotics.Task.WithTarget".Translate(label, target);
		}

		public static string Label(JobDef def)
		{
			if (def == null)
			{
				return "CK.Robotics.Task.Idle".Translate();
			}

			string key = KeyFor(def);
			if (!key.NullOrEmpty())
			{
				return key.Translate();
			}

			// 其他 Mod 的 JobDef 通常带 label；只有和 defName 不同才用它，
			// 避免把 "HaulToCell" 这种内部名当成任务名显示。
			if (!def.label.NullOrEmpty() && def.label != def.defName)
			{
				return def.label.CapitalizeFirst();
			}
			return "CK.Robotics.Task.Other".Translate();
		}

		private static string KeyFor(JobDef def)
		{
			switch (def.defName)
			{
				case "CK_NannyCleanFilth":
					return "CK.Robotics.Task.CleanFilth";
				case "CK_NannyCharge":
					return "CK.Robotics.Task.Charge";
				case "CK_NannyUnblock":
					return "CK.Robotics.Task.Unblock";
				case "CK_EmptySepticTank":
					return "CK.Robotics.Task.EmptySepticTank";
				case "CK_RepairNannyBot":
					return "CK.Robotics.Task.Repair";
				case "HaulToCell":
					return "CK.Robotics.Task.HaulToCell";
				case "HaulToContainer":
					return "CK.Robotics.Task.HaulToContainer";
				case "BeatFire":
					return "CK.Robotics.Task.BeatFire";
				case "ExtinguishSelf":
					return "CK.Robotics.Task.ExtinguishSelf";
				case "Flee":
					return "CK.Robotics.Task.Flee";
				case "DoBill":
					return "CK.Robotics.Task.DoBill";
				case "Refuel":
				case "RefuelAtomic":
					return "CK.Robotics.Task.Refuel";
				case "FeedPatient":
					return "CK.Robotics.Task.FeedPatient";
				case "Rescue":
					return "CK.Robotics.Task.Rescue";
				case "RopeToPen":
				case "RopeRoamerToUnenclosedPen":
					return "CK.Robotics.Task.RopeToPen";
				case "RopeRoamerToHitchingPost":
					return "CK.Robotics.Task.RopeToHitchingPost";
				case "Unrope":
					return "CK.Robotics.Task.Unrope";
				case "Goto":
					return "CK.Robotics.Task.Goto";
				case "GotoWander":
					return "CK.Robotics.Task.Wander";
				case "GotoSafeTemperature":
					return "CK.Robotics.Task.Goto";
				case "Wait":
				case "Wait_MaintainPosture":
				case "Wait_Asleep":
				case "Wait_AsleepDormancy":
				case "Wait_Downed":
				case "Wait_SafeTemperature":
				case "Wait_Wander":
				case "Wait_Combat":
					return "CK.Robotics.Task.Wait";
				case "Wander":
					return "CK.Robotics.Task.Wander";
				case "Ingest":
					return "CK.Robotics.Task.Ingest";
				case "Clean":
					return "CK.Robotics.Task.Clean";
				default:
					return null;
			}
		}

		private static string TargetLabel(Job job)
		{
			if (!CanShowTarget(job.def))
			{
				return null;
			}

			Thing thing = job.targetA.HasThing
				? job.targetA.Thing
				: (job.targetB.HasThing ? job.targetB.Thing : null);
			if (thing == null || thing.Destroyed)
			{
				return null;
			}
			return thing.LabelShort;
		}

		private static bool CanShowTarget(JobDef def)
		{
			switch (def.defName)
			{
				case "Goto":
				case "Wait":
				case "Wait_Combat":
				case "Wander":
				case "CK_NannyCharge":
					return false;
				default:
					return true;
			}
		}
	}
}
