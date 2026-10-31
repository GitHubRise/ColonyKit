using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_SaunaHeater : CompProperties_HeatPusher
	{
		/// <summary>可选的点火特效（DBH 用电/柴两套贴图）；不配就不画。</summary>
		public GraphicData effects;

		public CompProperties_SaunaHeater()
		{
			compClass = typeof(Comp_SaunaHeater);
		}
	}

	/// <summary>
	/// DBH Comp_SaunaHeater 的移植：只对「桑拿房」生效的加热器。
	/// 房间角色不是桑拿房时不加热、检视会提示；被使用后冒蒸汽并推热。
	/// 和 DBH 的差异：发光用原版 CompGlower（DBH 用 ProperGlowerComp），蒸汽粒子没搬。
	/// </summary>
	public class Comp_SaunaHeater : ThingComp
	{
		public int steamTicks;

		public CompRefuelable fuelComp;

		public CompPowerTrader powerComp;

		public CompBreakdownable breakdownableComp;

		public bool saunaRoom = true;

		public CompProperties_SaunaHeater Props => (CompProperties_SaunaHeater)props;

		public bool ShouldPushHeatNow
		{
			get
			{
				if (!parent.SpawnedOrAnyParentSpawned)
				{
					return false;
				}
				float ambient = parent.AmbientTemperature;
				if (ambient < Props.heatPushMaxTemperature
					&& ambient > Props.heatPushMinTemperature
					&& steamTicks > 0
					&& FlickUtility.WantsToBeOn(parent)
					&& (powerComp == null || powerComp.PowerOn)
					&& (fuelComp == null || fuelComp.HasFuel))
				{
					return breakdownableComp == null || !breakdownableComp.BrokenDown;
				}
				return false;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			fuelComp = parent.GetComp<CompRefuelable>();
			powerComp = parent.GetComp<CompPowerTrader>();
			breakdownableComp = parent.GetComp<CompBreakdownable>();
		}

		public override string CompInspectStringExtra()
		{
			if (!saunaRoom)
			{
				return "CK.Hygiene.RequiresSaunaRoom".Translate();
			}
			return base.CompInspectStringExtra();
		}

		public void UsedNow()
		{
			fuelComp?.Notify_UsedThisTick();
			steamTicks = 60;
		}

		public override void CompTick()
		{
			if (parent.IsHashIntervalTick(60))
			{
				Room room = parent.GetRoom();
				saunaRoom = room != null && room.Role != null && room.Role.defName == "CK_SaunaRoom";
				if (ShouldPushHeatNow)
				{
					if (powerComp != null)
					{
						powerComp.PowerOutput = -1000f;
					}
					GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, Props.heatPerSecond);
				}
				else if (powerComp != null)
				{
					powerComp.PowerOutput = -100f;
				}
			}
			if (steamTicks > -1)
			{
				steamTicks--;
			}
			// 被使用后的 60 tick 内每 10 tick 冒一团桑拿蒸汽（DBH 同款：房间内随机取点）。
			if (steamTicks > 0 && parent.Spawned && parent.IsHashIntervalTick(10))
			{
				ThrowSteamFleck();
			}
		}

		private void ThrowSteamFleck()
		{
			Room room = parent.GetRoom();
			if (room == null)
			{
				return;
			}
			IntVec3 cell = room.Cells.RandomElement();
			if (!cell.IsValid || cell.DistanceTo(parent.Position) > 10f)
			{
				return;
			}
			FleckDef steam = DefDatabase<FleckDef>.GetNamedSilentFail("CK_FleckSteamRoom");
			if (steam == null)
			{
				return;
			}
			Vector3 loc = cell.ToVector3Shifted();
			loc += new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			loc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			if (!loc.ShouldSpawnMotesAt(parent.Map))
			{
				return;
			}
			FleckCreationData data = FleckMaker.GetDataStatic(loc, parent.Map, steam, Rand.Range(2f, 3f));
			data.velocityAngle = Rand.Bool ? 90f : -90f;
			data.velocitySpeed = 0.032f;
			parent.Map.flecks.CreateFleck(data);
		}
	}
}
