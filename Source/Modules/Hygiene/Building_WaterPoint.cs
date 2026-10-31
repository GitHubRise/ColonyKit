using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 取水点：接在水网上的公共水龙头 + 150 L 小水箱。
	/// 殖民者可以直接在这里喝水、洗手，也能用水桶接水带走；
	/// 在检视页设置「取水次数」后，会自动派活拿水桶去同网水井或附近池塘/河边打水倒回来。
	/// 设置「制冰次数」后，室外温度 ≤ 0°C 时会把水箱里的水冻成冰块（每块 10 L）。
	/// 同网有工作的水泵时打水快，没有水泵只能手动慢慢打。
	/// </summary>
	public class Building_WaterPoint : Building_Basin
	{
		public const int MaxDraws = 99;

		public const int MaxIce = 99;

		/// <summary>低于这个水量就不值得再派一趟打水，避免半桶空跑。</summary>
		public const float MinDrawLiters = 20f;

		/// <summary>一块冰含多少升水。</summary>
		public const float IceWaterLiters = 10f;

		/// <summary>每多少 tick 冻出一块冰（室外 ≤ 0°C 时）。</summary>
		public const int IceProduceTicks = 1000;

		/// <summary>无电时每小时上水 25 升；通电时 200 升/小时。</summary>
		private const float UnpoweredIntakeLitersPerHour = 25f;

		private const float PoweredIntakeLitersPerHour = 200f;

		/// <summary>RimWorld 一天 60000 tick，按 24 小时算，一小时 2500 tick。</summary>
		private const int TicksPerHour = 2500;

		private const int PassiveIntakeInterval = 250;

		/// <summary>每 250 tick 的无电进水量：25 L/h × 250 / 2500 = 2.5 L。</summary>
		private const float PassiveIntakeLiters =
			UnpoweredIntakeLitersPerHour * PassiveIntakeInterval / TicksPerHour;

		private const float PoweredIntakeMultiplier =
			PoweredIntakeLitersPerHour / UnpoweredIntakeLitersPerHour;

		public int targetDraws;

		public int deliveredDraws;

		public int targetIce;

		public int madeIce;

		private CompWaterStorage storage;

		private int surfaceCheckTick = -9999;

		private bool surfaceNear;

		public CompWaterStorage Storage => storage ?? (storage = GetComp<CompWaterStorage>());

		public float StoredWater => Storage?.WaterStorage ?? 0f;

		public float Capacity => Storage?.Props.WaterStorageCap ?? 0f;

		public float Space => Storage?.space ?? 0f;

		public int RemainingDraws => Mathf.Max(0, targetDraws - deliveredDraws);

		public bool HasPendingDraws => RemainingDraws > 0;

		public int RemainingIce => Mathf.Max(0, targetIce - madeIce);

		public bool HasPendingIce => RemainingIce > 0;

		public bool Pumped => pipe?.pipeNet?.WaterPumps.Any(p => p != null && p.WorkingNow) == true;

		/// <summary>所在位置温度不高于 0°C 就能把水冻成冰（冷库/冬天室外都行）。</summary>
		public bool CanFreeze => Map != null && Position.GetTemperature(Map) <= 0f;

		/// <summary>附近（40 格内）有没有可站人的地表水；检视页每 250 tick 缓存一次。</summary>
		public bool HasSurfaceWaterNear
		{
			get
			{
				int now = Find.TickManager?.TicksGame ?? 0;
				if (surfaceCheckTick < 0 || now - surfaceCheckTick >= 250)
				{
					surfaceCheckTick = now;
					surfaceNear = Map != null
						&& WaterSourceUtility.FindSurfaceWaterStandCell(Map, Position, 40f).IsValid;
				}
				return surfaceNear;
			}
		}

		public override bool CanSetGender => false;

		public override bool ShowFixtureGizmos => false;

		public override PathEndMode InteractionPathMode => PathEndMode.Touch;

		/// <summary>水龙头是否建在水面上；水面上时作为同网水泵的地表进水口。</summary>
		public bool OnWater => Map != null && Position.GetTerrain(Map).IsWater;

		/// <summary>水龙头是否通电；通电时进水是无电的 8 倍。</summary>
		public bool PoweredIntake => powerComp != null && powerComp.PowerOn;

		/// <summary>当前进水速度（升/小时），供检视页显示具体数值。</summary>
		public float IntakeLitersPerHour =>
			PoweredIntake ? PoweredIntakeLitersPerHour : UnpoweredIntakeLitersPerHour;

		/// <summary>没电也能喝/洗手（水箱里的水仍可用），只是进水慢。</summary>
		public override bool PowerRequiredToUse => false;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		public override float waterUsed => 2f * HygieneSettings.Current.waterUseMultiplier;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			storage = GetComp<CompWaterStorage>();
			if (storage != null)
			{
				// 断电时水箱仍可存取，电力只决定进水速度。
				storage.IgnorePower = true;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref targetDraws, "targetDraws", 0);
			Scribe_Values.Look(ref deliveredDraws, "deliveredDraws", 0);
			Scribe_Values.Look(ref targetIce, "targetIce", 0);
			Scribe_Values.Look(ref madeIce, "madeIce", 0);
		}

		protected override void Tick()
		{
			base.Tick();
			if (Map == null)
			{
				return;
			}

			// 无电慢速重力进水；通电时 ×8。水源可以是水面，或同网水井/其他水上进水口。
			if (this.IsHashIntervalTick(PassiveIntakeInterval)
				&& StoredWater < Capacity
				&& HasNetworkWaterSource())
			{
				CompWaterStorage intakeTank = Storage;
				if (intakeTank != null)
				{
					float amount = PassiveIntakeLiters * (PoweredIntake ? PoweredIntakeMultiplier : 1f);
					float added = Mathf.Min(amount, intakeTank.space);
					if (added > 0f)
					{
						if (intakeTank.WaterStorage <= 0.01f)
						{
							intakeTank.WaterQuality = ContaminationLevel.Untreated;
						}
						intakeTank.WaterStorage += added;
					}
				}
			}

			if (!HasPendingIce
				|| !CanFreeze
				|| StoredWater < IceWaterLiters
				|| !this.IsHashIntervalTick(IceProduceTicks))
			{
				return;
			}
			CompWaterStorage tank = Storage;
			ThingDef iceDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_IceBlock");
			if (tank == null || iceDef == null)
			{
				return;
			}
			Thing ice = ThingMaker.MakeThing(iceDef);
			if (!GenPlace.TryPlaceThing(ice, Position, Map, ThingPlaceMode.Near))
			{
				ice.Destroy(DestroyMode.Vanish);
				return;
			}
			tank.WaterStorage = Mathf.Max(0f, tank.WaterStorage - IceWaterLiters);
			madeIce++;
		}

		private enum IntakeSource
		{
			None,
			OnWater,
			Well,
			Tank,
			Surface
		}

		/// <summary>
		/// 无电进水的水源：建在水面上、同网水井、同网其他水箱存水，或附近 40 格内的地表水。
		/// 不把水龙头自己的水箱算作水源，避免「自己有一点水就无限自灌」。
		/// </summary>
		private IntakeSource CurrentIntakeSource()
		{
			if (OnWater)
			{
				return IntakeSource.OnWater;
			}
			PlumbingNet net = pipe?.pipeNet;
			if (net != null)
			{
				foreach (CompWaterInlet well in net.WaterWells)
				{
					if (well != null && well.parent != this && well.GroundWaterCapacity > 0.0001f)
					{
						return IntakeSource.Well;
					}
				}
				foreach (CompWaterStorage tank in net.WaterTowers)
				{
					if (tank != null
						&& tank.parent != this
						&& tank.WorkingNow
						&& tank.WaterStorage > 1f)
					{
						return IntakeSource.Tank;
					}
				}
			}
			if (HasSurfaceWaterNear)
			{
				return IntakeSource.Surface;
			}
			return IntakeSource.None;
		}

		private bool HasNetworkWaterSource()
		{
			return CurrentIntakeSource() != IntakeSource.None;
		}

		/// <summary>检视页/水龙头页显示当前进水来源，避免玩家只看到速度却不知道为什么不进水。</summary>
		public string IntakeSourceLabel
		{
			get
			{
				switch (CurrentIntakeSource())
				{
					case IntakeSource.OnWater:
						return "CK.Hygiene.WaterPoint.SourceOnWater".Translate();
					case IntakeSource.Well:
						return "CK.Hygiene.WaterPoint.SourceWell".Translate();
					case IntakeSource.Tank:
						return "CK.Hygiene.WaterPoint.SourceTank".Translate();
					case IntakeSource.Surface:
						return "CK.Hygiene.WaterPoint.SourceSurface".Translate();
					default:
						return "CK.Hygiene.WaterPoint.SourceNone".Translate();
				}
			}
		}

		public void SetDrawOrder(int count)
		{
			count = Mathf.Clamp(count, 0, MaxDraws);
			if (count != targetDraws)
			{
				targetDraws = count;
				deliveredDraws = 0;
			}
			if (deliveredDraws > targetDraws)
			{
				deliveredDraws = targetDraws;
			}
		}

		public void CancelDrawOrder()
		{
			targetDraws = 0;
			deliveredDraws = 0;
		}

		public void NotifyBucketDelivered()
		{
			if (deliveredDraws < targetDraws)
			{
				deliveredDraws++;
			}
		}

		public void SetIceOrder(int count)
		{
			count = Mathf.Clamp(count, 0, MaxIce);
			if (count != targetIce)
			{
				targetIce = count;
				madeIce = 0;
			}
			if (madeIce > targetIce)
			{
				madeIce = targetIce;
			}
		}

		public void CancelIceOrder()
		{
			targetIce = 0;
			madeIce = 0;
		}

		/// <summary>把一桶水倒进水箱；返回实际倒进去的升数。</summary>
		public float AddWater(float liters, ContaminationLevel quality)
		{
			CompWaterStorage tank = Storage;
			if (tank == null || liters <= 0f)
			{
				return 0f;
			}
			float added = Mathf.Min(liters, tank.space);
			if (added <= 0.0001f)
			{
				return 0f;
			}
			if (tank.WaterStorage <= 0.01f || quality > tank.WaterQuality)
			{
				tank.WaterQuality = quality;
			}
			tank.WaterStorage += added;
			return added;
		}

		/// <summary>同一张水网里最近的可达水井；没有就返回 null。</summary>
		public Thing FindWellForDraw()
		{
			PlumbingNet net = pipe?.pipeNet;
			if (net == null)
			{
				return null;
			}
			Thing best = null;
			float bestDistance = float.MaxValue;
			foreach (CompWaterInlet well in net.WaterWells)
			{
				Thing thing = well?.parent;
				if (thing == null
					|| thing == this
					|| !thing.Spawned
					|| thing.Map != Map
					|| well.GroundWaterCapacity <= 0f
					|| thing.IsForbidden(Faction.OfPlayer))
				{
					continue;
				}
				float distance = thing.Position.DistanceTo(Position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = thing;
				}
			}
			return best;
		}

		/// <summary>
		/// 取水次数用的水源：同网水井优先；没有水井就找附近小池塘/河边的可站格。
		/// 返回 Thing 表示水井，返回 Cell 表示地表水的可站格；没有则 Invalid。
		/// </summary>
		public LocalTargetInfo FindDrawSource(Pawn pawn, out bool surface)
		{
			surface = false;
			Thing well = FindWellForDraw();
			// 有井但小人够不着时继续找地表水，不要因为一口远处的井放弃就近的池塘。
			if (well != null
				&& (pawn == null
					|| pawn.CanReserveAndReach(well, PathEndMode.Touch, pawn.NormalMaxDanger())))
			{
				return new LocalTargetInfo(well);
			}
			IntVec3 stand = WaterSourceUtility.FindSurfaceWaterStandCell(pawn, Position, 40f);
			if (stand.IsValid)
			{
				surface = true;
				return new LocalTargetInfo(stand);
			}
			return LocalTargetInfo.Invalid;
		}

		/// <summary>取水点不消耗热水：喝水和洗手都直接从水网取常温水。</summary>
		public override bool TryUseWater(out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Treated;
			if (!HasWaterSupply)
			{
				return false;
			}
			float amount = waterUsedPerTick * HygieneSettings.Current.waterUseMultiplier;
			if (!Working(amount).Accepted)
			{
				return false;
			}
			return pipe.pipeNet.PullWater(amount, out contam);
		}

		/// <summary>
		/// 带净水器的水龙头可以直接装 1 L 纯净水；没有净水器/没水时不能装。
		/// </summary>
		public bool CanBottlePurifiedWater(Pawn pawn)
		{
			PlumbingNet net = pipe?.pipeNet;
			return net != null
				&& net.HasFilter
				&& net.WaterStorage >= 1f
				&& HasWaterSupply
				&& pawn != null
				&& pawn.CanReserveAndReach(this, InteractionPathMode, pawn.NormalMaxDanger());
		}

		/// <summary>从水网抽 1 L 已过滤水，返回水质（正常情况下必为 Treated）。</summary>
		public bool TryDrawPurifiedBottle(out ContaminationLevel quality)
		{
			quality = ContaminationLevel.Treated;
			PlumbingNet net = pipe?.pipeNet;
			if (net == null || !net.HasFilter || !HasWaterSupply)
			{
				return false;
			}
			return net.PullWater(1f, out quality);
		}

		/// <summary>
		/// 水龙头可以直接装 1 L 矿泉水：水网里有至少 1 升水、水龙头出得了水、小人够得着。
		/// 和取水点一样不需要水桶/脸盆，只是这里要真的从水网里抽走 1 升。
		/// </summary>
		public bool CanBottleMineralWater(Pawn pawn)
		{
			PlumbingNet net = pipe?.pipeNet;
			return net != null
				&& net.WaterStorage >= 1f
				&& HasWaterSupply
				&& pawn != null
				&& pawn.CanReserveAndReach(this, InteractionPathMode, pawn.NormalMaxDanger());
		}

		/// <summary>从水网抽 1 L 常温水装瓶，返回抽到的水质（未处理/已污染都照实记录）。</summary>
		public bool TryDrawMineralBottle(out ContaminationLevel quality)
		{
			quality = ContaminationLevel.Untreated;
			PlumbingNet net = pipe?.pipeNet;
			if (net == null || !HasWaterSupply)
			{
				return false;
			}
			return net.PullWater(1f, out quality);
		}

		public override string GetInspectString()
		{
			StringBuilder builder = new StringBuilder(base.GetInspectString());
			void Append(string line)
			{
				if (builder.Length > 0)
				{
					builder.AppendLine();
				}
				builder.Append(line);
			}

			Append(PoweredIntake
				? "CK.Hygiene.WaterPoint.FlowFast".Translate(IntakeLitersPerHour.ToString("F0"))
				: "CK.Hygiene.WaterPoint.FlowSlow".Translate(IntakeLitersPerHour.ToString("F0")));
			Append(IntakeSourceLabel);
			CompSurfaceWaterInlet inlet = GetComp<CompSurfaceWaterInlet>();
			if (inlet != null && inlet.GroundWaterCapacity > 0.0001f)
			{
				Append("CK.Hygiene.WaterPoint.SurfaceInlet".Translate(
					inlet.GroundWaterCapacity.ToString("F0")));
			}
			if (targetDraws > 0)
			{
				Append("CK.Hygiene.WaterPoint.DrawProgress".Translate(deliveredDraws, targetDraws));
				Thing well = FindWellForDraw();
				Append(well != null
					? "CK.Hygiene.WaterPoint.WellReady".Translate()
					: HasSurfaceWaterNear
						? "CK.Hygiene.WaterPoint.SurfaceReady".Translate()
						: "CK.Hygiene.WaterPoint.NoSource".Translate());
			}
			if (targetIce > 0)
			{
				Append("CK.Hygiene.WaterPoint.IceProgress".Translate(madeIce, targetIce));
				Append(CanFreeze
					? "CK.Hygiene.WaterPoint.IceReady".Translate()
					: "CK.Hygiene.WaterPoint.IceWarm".Translate());
			}
			return builder.ToString().TrimEndNewlines();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.WaterPoint.SetDraws".Translate(targetDraws),
				defaultDesc = "CK.Hygiene.WaterPoint.SetDrawsDesc".Translate(),
				action = () => Find.WindowStack.Add(new Dialog_Slider(
					(int value) => "CK.Hygiene.WaterPoint.SetDrawsSlider".Translate(value),
					0,
					MaxDraws,
					SetDrawOrder,
					targetDraws))
			};
			if (targetDraws > 0)
			{
				yield return new Command_Action
				{
					defaultLabel = "CK.Hygiene.WaterPoint.CancelDraws".Translate(),
					defaultDesc = "CK.Hygiene.WaterPoint.CancelDrawsDesc".Translate(),
					action = CancelDrawOrder
				};
			}
			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.WaterPoint.SetIce".Translate(targetIce),
				defaultDesc = "CK.Hygiene.WaterPoint.SetIceDesc".Translate(),
				action = () => Find.WindowStack.Add(new Dialog_Slider(
					(int value) => "CK.Hygiene.WaterPoint.SetIceSlider".Translate(value),
					0,
					MaxIce,
					SetIceOrder,
					targetIce))
			};
			if (targetIce > 0)
			{
				yield return new Command_Action
				{
					defaultLabel = "CK.Hygiene.WaterPoint.CancelIce".Translate(),
					defaultDesc = "CK.Hygiene.WaterPoint.CancelIceDesc".Translate(),
					action = CancelIceOrder
				};
			}
		}
	}
}
