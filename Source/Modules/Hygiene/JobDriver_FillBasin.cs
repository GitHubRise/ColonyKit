using System.Collections.Generic;
using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 用随身容器（脸盆/水桶）打水：容器在物品栏里就直接装，不在就去地上找一只（自动取用）。
	/// 水井抽地下水不需要水网有水；取水点/水箱从同网取水。同网有工作的水泵时出水快，
	/// 没有水泵时只能手动慢慢打（水井 0.05 L/tick，取水点 0.12 L/tick）。
	/// </summary>
	public class JobDriver_FillBasin : JobDriver
	{
		private const TargetIndex SourceInd = TargetIndex.A;

		private const TargetIndex ContainerInd = TargetIndex.B;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(SourceInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			if (WaterContainerUtility.CarriedContainer(pawn, requireWater: false) == null
				&& job.GetTarget(ContainerInd).HasThing)
			{
				return pawn.Reserve(job.GetTarget(ContainerInd), job, 1, -1, null, errorOnFailed);
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(SourceInd);

			Toil fill = ToilMaker.MakeToil("CK_FillBasin");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.FailOnDespawnedNullOrForbidden(SourceInd);
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater container = WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
				Thing source = job.GetTarget(SourceInd).Thing;
				if (container == null || source == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				bool fromWell = IsWell(source);
				bool pumped = IsPumped(source);
				float amount = Mathf.Min(FillRate(fromWell, pumped) * delta, container.Space);
				if (amount <= 0.0001f)
				{
					// 容器满了。
					ReadyForNextToil();
					return;
				}
				if (fromWell)
				{
					// 水井是地下水，不消耗水箱；水质按未处理算（和 DBH 的井水一致）。
					container.AddWater(amount, ContaminationLevel.Untreated);
				}
				else
				{
					PlumbingNet net = NetOf(source);
					if (net == null || !net.PullWater(amount, out ContaminationLevel contam))
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					container.AddWater(amount, contam);
				}
				if (container.IsFull)
				{
					ReadyForNextToil();
				}
			};
			fill.WithProgressBar(
				SourceInd,
				() =>
				{
					CompBasinWater container = WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
					return container == null || container.Props.Capacity <= 0f
						? 1f
						: container.Water / container.Props.Capacity;
				},
				interpolateBetweenActorAndTarget: true);

			Toil pickup = ToilMaker.MakeToil("CK_FillBasinPickup");
			pickup.initAction = () =>
			{
				Thing container = job.GetTarget(ContainerInd).Thing;
				if (container == null || container.Destroyed || !container.Spawned)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				container.DeSpawn();
				if (!pawn.inventory.innerContainer.TryAdd(container))
				{
					GenPlace.TryPlaceThing(container, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					EndJobWith(JobCondition.Incompletable);
				}
			};

			Toil gotoSource = Toils_Goto.GotoThing(SourceInd, PathEndMode.Touch);
			// 已经随身带着容器就跳过“去捡容器”，直接去水源。
			yield return Toils_Jump.JumpIf(gotoSource, () => WaterContainerUtility.CarriedContainer(pawn, requireWater: false) != null);
			yield return Toils_Goto.GotoThing(ContainerInd, PathEndMode.ClosestTouch);
			yield return pickup;
			yield return Toils_Jump.Jump(gotoSource);
			yield return gotoSource;
			yield return fill;
		}

		/// <summary>打水速度（升/tick）。水泵在工作时明显更快。</summary>
		internal static float FillRate(bool fromWell, bool pumped)
		{
			if (fromWell)
			{
				return pumped ? 0.30f : 0.05f;
			}
			return pumped ? 0.30f : 0.12f;
		}

		internal static bool IsWell(Thing source)
		{
			return (source as ThingWithComps)?.TryGetComp<CompWaterInlet>() != null;
		}

		internal static bool IsPumped(Thing source)
		{
			PlumbingNet net = NetOf(source);
			return net != null && net.WaterPumps.Any(p => p != null && p.WorkingNow);
		}

		internal static PlumbingNet NetOf(Thing source)
		{
			return (source as ThingWithComps)?.GetComp<CompPipe>()?.pipeNet;
		}

		/// <summary>物品栏里还能装水的容器（优先大容量）。</summary>
		internal static CompBasinWater CarriedBasin(Pawn pawn)
		{
			return WaterContainerUtility.CarriedContainer(pawn, requireWater: false);
		}

		/// <summary>物品栏里有水的容器（倒水/喝水用）。</summary>
		internal static CompBasinWater CarriedBasinWithWater(Pawn pawn)
		{
			return WaterContainerUtility.CarriedContainer(pawn, requireWater: true);
		}
	}
}
