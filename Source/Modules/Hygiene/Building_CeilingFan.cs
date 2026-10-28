using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 吊扇（DBH Building_CeilingFan 的移植）：吊在天花板上，房间超过 22℃ 才开始转，
	/// 按环境温度把热量「抽走」；带一盏灯。扇叶按 FanRotation 实时旋转。
	/// </summary>
	public class Building_CeilingFan : Building
	{
		public float FanRotation;

		public float FanSpeed;

		private float targetSpeed;

		private float tempFactor;

		private CompPowerTrader powerComp;

		private CompRefuelable refuelableComp;

		private CompBreakdownable breakdownableComp;

		public bool ShouldPushHeatNow
		{
			get
			{
				if (AmbientTemperature <= 22f || !SpawnedOrAnyParentSpawned)
				{
					return false;
				}
				if (!FlickUtility.WantsToBeOn(this))
				{
					return false;
				}
				if (powerComp != null && !powerComp.PowerOn)
				{
					return false;
				}
				if (refuelableComp != null && !refuelableComp.HasFuel)
				{
					return false;
				}
				return breakdownableComp == null || !breakdownableComp.BrokenDown;
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = GetComp<CompPowerTrader>();
			refuelableComp = GetComp<CompRefuelable>();
			breakdownableComp = GetComp<CompBreakdownable>();
			FanRotation = Rand.Range(0f, 360f);
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			// 只画旋转中的扇叶：直接给 Graphic 一个额外的旋转角。
			Graphic.Draw(DrawPos, Rotation, this, FanRotation);
		}

		protected override void Tick()
		{
			FanRotation += FanSpeed;
			if (FanRotation > 360f)
			{
				FanRotation = 0f;
			}
			FanSpeed = Mathf.Lerp(FanSpeed, targetSpeed, 0.08f);

			if (!this.IsHashIntervalTick(250))
			{
				return;
			}
			if (powerComp != null)
			{
				powerComp.PowerOutput = -15f;
			}
			bool running = false;
			float heatPushed = 0f;
			if (ShouldPushHeatNow)
			{
				tempFactor = Mathf.Clamp01(Mathf.InverseLerp(10f, 40f, AmbientTemperature));
				float energyLimit = -11f * tempFactor * 4.1666665f;
				heatPushed = GenTemperature.ControlTemperatureTempChange(Position, Map, energyLimit, 22f);
				running = !Mathf.Approximately(heatPushed, 0f);
			}
			if (running)
			{
				targetSpeed = 2f + 3f * tempFactor;
				Room room = Position.GetRoom(Map);
				if (room != null)
				{
					room.Temperature += heatPushed;
				}
				if (powerComp != null)
				{
					// 1.6 的 CompProperties_Power.basePowerConsumption 是私有的，这里用 Def 里的 50W。
					powerComp.PowerOutput = -50f;
				}
			}
			else
			{
				targetSpeed = 0f;
			}
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			string line = ShouldPushHeatNow
				? "CK.Hygiene.FanCooling".Translate()
				: "CK.Hygiene.FanIdle".Translate();
			return text.NullOrEmpty() ? line : text + "\n" + line;
		}
	}
}
