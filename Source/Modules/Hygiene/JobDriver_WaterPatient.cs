using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 给病人/囚徒喂水：先把随身/地上的脸盆或水桶拿到取水点、水龙头、水井或水箱装满，
	/// 再走到病人身边，一勺一勺喂到不渴。病人全程不需要起身。
	/// </summary>
	public class JobDriver_WaterPatient : JobDriver
	{
		private const TargetIndex PatientInd = TargetIndex.A;

		private const TargetIndex ContainerInd = TargetIndex.B;

		private const TargetIndex SourceInd = TargetIndex.C;

		private const float FeedLitersPerTick = 0.02f;

		private ContaminationLevel waterQuality = ContaminationLevel.Treated;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterQuality, "waterQuality", ContaminationLevel.Treated);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(PatientInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			if (job.GetTarget(ContainerInd).HasThing
				&& !pawn.Reserve(job.GetTarget(ContainerInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(PatientInd);

			Toil gotoPatient = Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);
			if (job.GetTarget(SourceInd).IsValid)
			{
				Toil gotoSource = job.GetTarget(SourceInd).HasThing
					? Toils_Goto.GotoThing(SourceInd, PathEndMode.Touch)
					: Toils_Goto.GotoCell(SourceInd, PathEndMode.OnCell);
				yield return Toils_Jump.JumpIf(
					gotoSource, () => WaterContainerUtility.CarriedContainer(pawn, requireWater: true) != null);
				yield return Toils_Goto.GotoThing(ContainerInd, PathEndMode.ClosestTouch);
				yield return PickupContainer();
				yield return Toils_Jump.Jump(gotoSource);
				yield return gotoSource;
				yield return FillContainer();
			}
			else
			{
				yield return Toils_Jump.JumpIf(
					gotoPatient, () => WaterContainerUtility.CarriedContainer(pawn, requireWater: true) != null);
				yield return Toils_Goto.GotoThing(ContainerInd, PathEndMode.ClosestTouch);
				yield return PickupContainer();
				yield return Toils_Jump.Jump(gotoPatient);
			}
			yield return gotoPatient;
			yield return FeedPatient();
		}

		private Toil PickupContainer()
		{
			Toil pickup = ToilMaker.MakeToil("CK_WaterPatientPickup");
			pickup.initAction = () =>
			{
				Thing thing = job.GetTarget(ContainerInd).Thing;
				if (thing == null || thing.Destroyed || !thing.Spawned)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				thing.DeSpawn();
				if (!pawn.inventory.innerContainer.TryAdd(thing))
				{
					GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					EndJobWith(JobCondition.Incompletable);
				}
			};
			return pickup;
		}

		private Toil FillContainer()
		{
			Toil fill = ToilMaker.MakeToil("CK_WaterPatientFill");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater container = WaterContainerUtility.CarriedContainer(pawn, requireWater: true)
					?? WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
				LocalTargetInfo source = job.GetTarget(SourceInd);
				if (container == null || !source.IsValid)
				{
					ReadyForNextToil();
					return;
				}
				if (container.IsFull)
				{
					ReadyForNextToil();
					return;
				}

				bool fromWell = false;
				bool fromSurface = false;
				PlumbingNet net = null;
				float rate;
				ContaminationLevel quality = ContaminationLevel.Untreated;
				if (source.HasThing)
				{
					Thing thing = source.Thing;
					if (thing == null)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					fromWell = JobDriver_FillBasin.IsWell(thing);
					net = JobDriver_FillBasin.NetOf(thing);
					rate = JobDriver_FillBasin.FillRate(fromWell, JobDriver_FillBasin.IsPumped(thing));
				}
				else
				{
					IntVec3 waterCell = WaterSourceUtility.AdjacentWaterCell(pawn.Map, source.Cell);
					if (!waterCell.IsValid)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					fromSurface = true;
					rate = JobDriver_FillBasin.SurfaceFillRate;
					quality = WaterSourceUtility.SurfaceQuality(pawn.Map, waterCell);
				}

				float amount = Mathf.Min(rate * delta, container.Space);
				if (amount <= 0.0001f)
				{
					ReadyForNextToil();
					return;
				}
				if (fromWell)
				{
					container.AddWater(amount, ContaminationLevel.Untreated);
				}
				else if (fromSurface)
				{
					container.AddWater(amount, quality);
				}
				else
				{
					if (net == null || !net.PullWater(amount, out ContaminationLevel contam))
					{
						// 水箱被抽空：先把已有的水喂给病人，不要整趟白跑。
						ReadyForNextToil();
						return;
					}
					container.AddWater(amount, contam);
				}
				if (container.IsFull)
				{
					ReadyForNextToil();
				}
			};
			return fill;
		}

		private Toil FeedPatient()
		{
			Toil feed = ToilMaker.MakeToil("CK_WaterPatientFeed");
			feed.defaultCompleteMode = ToilCompleteMode.Never;
			feed.tickIntervalAction = delta =>
			{
				Pawn patient = job.GetTarget(PatientInd).Thing as Pawn;
				CompBasinWater container = WaterContainerUtility.CarriedContainer(pawn, requireWater: true);
				Need_Thirst thirst = patient?.needs?.TryGetNeed<Need_Thirst>();
				if (patient == null || patient.Dead || !patient.Spawned || container == null || thirst == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float taken = container.Drain(Mathf.Min(FeedLitersPerTick * delta, container.Water));
				if (taken <= 0.0001f)
				{
					ReadyForNextToil();
					return;
				}
				if (container.Contamination > waterQuality)
				{
					waterQuality = container.Contamination;
				}
				thirst.Drink(taken * MineralWaterUtility.ThirstPerLiter);
				if (thirst.CurLevel >= 0.999f || container.IsEmpty)
				{
					ReadyForNextToil();
				}
			};
			feed.AddFinishAction(() =>
			{
				Pawn patient = job.GetTarget(PatientInd).Thing as Pawn;
				if (patient != null && !patient.Dead)
				{
					SanitationUtil.ContaminationCheckDrinking(patient, waterQuality);
					HygieneUtility.AddDrinkSatiety(patient);
				}
			});
			return feed;
		}
	}
}
