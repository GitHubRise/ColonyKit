using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人躺到医疗床上休养：耐久按设置页「医疗床恢复速率」恢复。
	/// 起身条件都写在这一处，保证任何一条断了都能回到常规思考树，不会卡在床上：
	/// 耐久满了、床被拆/被禁用/被别人占了或订了、电量低到要回桩、附近有敌人。
	/// </summary>
	public class JobDriver_RobotRestInBed : JobDriver
	{
		private const TargetIndex BedInd = TargetIndex.A;

		private const float NearbyThreatRadius = 25f;

		private Building_Bed Bed => job.GetTarget(BedInd).Thing as Building_Bed;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Building_Bed bed = Bed;
			return bed != null && pawn.Reserve(bed, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(BedInd);
			this.FailOn(() => pawn.Drafted || pawn.Dead);
			// 床没了/被禁用/被别人占用或预定，就地结束，机器人回去干别的。
			this.FailOn(() => !NannyBotUtility.CanRestInBed(pawn, Bed, true, false));

			// 已经被放到床位上了（例如读完档就躺在床上）就不用再走一趟。
			if (!AtSleepingSlot())
			{
				Toil goToBed = ToilMaker.MakeToil("CK_RobotRestGoToBed");
				goToBed.initAction = () =>
				{
					Building_Bed bed = Bed;
					if (bed == null)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					IntVec3 slot = RestUtility.GetBedSleepingSlotPosFor(pawn, bed);
					if (pawn.Position == slot)
					{
						pawn.jobs.curDriver.ReadyForNextToil();
						return;
					}
					pawn.pather.StartPath(slot, PathEndMode.OnCell);
					if (!pawn.pather.Moving && pawn.Position != slot)
					{
						// 走不过去（被墙/禁用挡住）就别硬躺，结束任务回去干别的。
						EndJobWith(JobCondition.Incompletable);
					}
				};
				goToBed.defaultCompleteMode = ToilCompleteMode.PatherArrival;
				yield return goToBed;
			}

			Toil rest = ToilMaker.MakeToil("CK_RobotRestLie");
			rest.defaultCompleteMode = ToilCompleteMode.Never;
			rest.initAction = () =>
			{
				pawn.pather?.StopDead();
				pawn.jobs.posture = PawnPosture.LayingInBed;
				PortraitsCache.SetDirty(pawn);
			};
			rest.tickIntervalAction = delta =>
			{
				CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
				if (comp == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (comp.Durability >= comp.MaxDurability - 0.01f)
				{
					EndJobWith(JobCondition.Succeeded);
					return;
				}
				if (NeedsToGetUp(pawn))
				{
					EndJobWith(JobCondition.InterruptForced);
					return;
				}
				// 有人倒下时把病床让出来（医院床位紧张时不跟伤者抢）。
				if (pawn.IsHashIntervalTick(250) && AnyColonistDowned(pawn))
				{
					EndJobWith(JobCondition.InterruptForced);
					return;
				}
				comp.Repair(NannyBotUtility.Settings.robotBedRestRate * delta / 60000f);
			};
			rest.AddFinishAction(() =>
			{
				if (pawn.jobs != null)
				{
					pawn.jobs.posture = PawnPosture.Standing;
					PortraitsCache.SetDirty(pawn);
				}
			});
			yield return rest;
		}

		private bool AtSleepingSlot()
		{
			Building_Bed bed = Bed;
			return bed != null && pawn.Position == RestUtility.GetBedSleepingSlotPosFor(pawn, bed);
		}

		/// <summary>地图上有殖民者倒地（可能正等着送病床）时，机器人先让床。</summary>
		private static bool AnyColonistDowned(Pawn robot)
		{
			Map map = robot?.Map;
			if (map == null)
			{
				return false;
			}
			foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
			{
				if (colonist != robot && colonist.Downed && !colonist.Dead)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>电量见底要回桩、附近有敌人、口渴/憋不住要去处理——先起身。</summary>
		private static bool NeedsToGetUp(Pawn pawn)
		{
			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (energy != null
				&& energy.CurLevelPercentage <= NannyBotUtility.Settings.chargeThreshold)
			{
				return true;
			}
			if (NannyBotUtility.Settings.dangerAvoidance
				&& NannyBotUtility.HasNearbyThreat(pawn, NearbyThreatRadius))
			{
				return true;
			}
			Need thirst = pawn.needs?.TryGetNeed<ColonyKit.Modules.Hygiene.Need_Thirst>();
			if (thirst != null && thirst.CurLevelPercentage < 0.2f)
			{
				return true;
			}
			Need bladder = pawn.needs?.TryGetNeed<ColonyKit.Modules.Hygiene.Need_Bladder>();
			if (bladder != null && bladder.CurLevelPercentage < 0.2f)
			{
				return true;
			}
			return false;
		}
	}
}
