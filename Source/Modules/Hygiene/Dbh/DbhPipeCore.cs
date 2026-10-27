using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public enum PipeType
	{
		Sewage,
		Air,
		Heating,
		Water
	}

	public class CompProperties_Pipe : CompProperties
	{
		public PipeType mode;
		public bool stuffed;
		public bool vertPipe;

		public CompProperties_Pipe()
		{
			compClass = typeof(CompPipe);
		}
	}

	/// <summary>DBH CompPipe 的首层移植：只保留水网所需的注册、开关与管网引用。</summary>
	public class CompPipe : ThingComp
	{
		public MapComponent_Hygiene MapComp;
		public PlumbingNet pipeNetRef;
		public CompFlickable flicker;
		public CompPowerTrader power;

		public int GridID { get; set; } = -1;

		public PipeType mode => Props.mode;

		public CompProperties_Pipe Props => (CompProperties_Pipe)props;

		public PlumbingNet pipeNet
		{
			get
			{
				// 开关关闭的洁具/管件不应继续读取旧水网；否则拆管或关阀后，
				// 旧的 pipeNetRef 仍可能指向已废弃的网络。
				if (closed)
				{
					return null;
				}
				HygienePipeMapComp mapComp = MapComp?.PipeComp;
				if (mapComp == null)
				{
					return pipeNetRef;
				}
				if (pipeNetRef == null || !mapComp.PipeNets.Contains(pipeNetRef))
				{
					mapComp.RegenPipeGrids();
				}
				return pipeNetRef;
			}
		}

		public bool closed => flicker != null && !flicker.SwitchIsOn;

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			// 选中任意水网物件（管、泵、井、热水器、洁具）时，把全图水箱水位条和热水罐水温条一起亮出来。
			MapComp?.PipeComp?.RequestTankBars();
			MapComp?.PipeComp?.RequestHeaterBars();
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			flicker = parent.GetComp<CompFlickable>();
			power = parent.GetComp<CompPowerTrader>();
			MapComp = parent.Map?.GetComponent<MapComponent_Hygiene>();
			MapComp?.PipeComp.RegisterPipe(this, respawningAfterLoad);
		}

		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			map.GetComponent<MapComponent_Hygiene>()?.PipeComp.DeregisterPipe(this);
			base.PostDeSpawn(map, mode);
		}
	}

	public class MapComponent_Hygiene : MapComponent
	{
		private HygienePipeMapComp pipeComp;

		public MapComponent_Hygiene(Map map) : base(map)
		{
		}

		public HygienePipeMapComp PipeComp
		{
			get
			{
				if (pipeComp == null)
				{
					pipeComp = map.GetComponent<HygienePipeMapComp>();
				}
				return pipeComp;
			}
		}
	}

	public class HygienePipeMapComp : MapComponent
	{
		public List<CompPipe> cachedPipes = new List<CompPipe>();
		public List<PlumbingNet> PipeNets = new List<PlumbingNet>();
		public int masterID = 1;

		/// <summary>选中水网物件或正在放水网建筑时为 true，下一次 OnGUI 把水箱水位条标记出来。</summary>
		public bool MarkTowersForDraw;

		/// <summary>同上，热水罐/热水器的红色水温条。</summary>
		public bool MarkHeatingForDraw;

		private bool dirty = true;

		public HygienePipeMapComp(Map map) : base(map)
		{
		}

		public void RequestTankBars()
		{
			MarkTowersForDraw = true;
		}

		public void RequestHeaterBars()
		{
			MarkHeatingForDraw = true;
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			RegenPipeGrids();
		}

		public override void MapComponentOnGUI()
		{
			if (PipeDesignators.IsPlacing(PipeType.Water))
			{
				MarkTowersForDraw = true;
				MarkHeatingForDraw = true;
			}
			if (MarkTowersForDraw)
			{
				MarkTowersForDraw = false;
				foreach (PlumbingNet net in PipeNets)
				{
					foreach (CompWaterStorage tower in net.WaterTowers)
					{
						if (tower != null)
						{
							tower.DrawOverlay = true;
						}
					}
				}
			}
			if (MarkHeatingForDraw)
			{
				MarkHeatingForDraw = false;
				foreach (PlumbingNet net in PipeNets)
				{
					foreach (CompHeatStore heatStore in net.HeatStores)
					{
						if (heatStore != null)
						{
							heatStore.DrawOverlay = true;
						}
					}
				}
			}
		}

		public override void MapComponentTick()
		{
			if (dirty)
			{
				RegenPipeGrids();
			}
			foreach (PlumbingNet net in PipeNets)
			{
				net.Tick();
			}
		}

		public void RegisterPipe(CompPipe pipe, bool respawningAfterLoad)
		{
			if (!cachedPipes.Contains(pipe))
			{
				cachedPipes.Add(pipe);
			}
			dirty = true;
		}

		public void DeregisterPipe(CompPipe pipe)
		{
			cachedPipes.Remove(pipe);
			dirty = true;
		}

		public void RegenPipeGrids()
		{
			dirty = false;
			PipeNets.Clear();
			var assigned = new HashSet<CompPipe>();
			foreach (CompPipe start in cachedPipes.Where(p => p != null && !p.closed))
			{
				if (!assigned.Add(start))
				{
					continue;
				}
				var net = new PlumbingNet
				{
					MapComp = map.GetComponent<MapComponent_Hygiene>(),
					PipeComp = this,
					NetID = masterID++,
					NetType = (int)start.mode
				};
				var queue = new Queue<CompPipe>();
				queue.Enqueue(start);
				while (queue.Count > 0)
				{
					CompPipe pipe = queue.Dequeue();
					pipe.GridID = net.NetID;
					pipe.pipeNetRef = net;
					if (pipe.parent != null)
					{
						net.PipedThings.Add(pipe.parent);
					}
					foreach (CompPipe other in cachedPipes)
					{
						if (other == null || other.closed || other.mode != start.mode || assigned.Contains(other))
						{
							continue;
						}
						if (Touches(pipe.parent, other.parent))
						{
							assigned.Add(other);
							queue.Enqueue(other);
						}
					}
				}
				net.InitNet();
				PipeNets.Add(net);
			}
		}

		private static bool Touches(Thing a, Thing b)
		{
			if (a == null || b == null || a.Map != b.Map)
			{
				return false;
			}
			// 只认四邻相接：原来用 DistanceTo <= 1.5，斜对角（√2）也算连通，
			// 于是污水管能绕过墙角接到另一段管子上，和 Graphic_Linked 的
			// 四向连管贴图对不上，水/污水网还会跨墙串在一起。
			foreach (IntVec3 cell in a.OccupiedRect())
			{
				foreach (IntVec3 other in b.OccupiedRect())
				{
					if (Mathf.Abs(cell.x - other.x) + Mathf.Abs(cell.z - other.z) == 1)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	/// <summary>DBH PlumbingNet 的水路子集，先保留 API 与取/推水语义。</summary>
	public class PlumbingNet
	{
		public int NetID;
		public int NetType;
		public MapComponent_Hygiene MapComp;
		public HygienePipeMapComp PipeComp;
		public HashSet<ThingWithComps> PipedThings = new HashSet<ThingWithComps>();
		public List<CompWaterStorage> WaterTowers = new List<CompWaterStorage>();
		public List<CompWaterPumpingStation> WaterPumps = new List<CompWaterPumpingStation>();
		public List<CompWaterInlet> WaterWells = new List<CompWaterInlet>();
		public List<CompWaterHeater> Heaters = new List<CompWaterHeater>();
		public List<CompHeatStore> HeatStores = new List<CompHeatStore>();
		public List<CompHeatStore> HotWaterTanks = new List<CompHeatStore>();
		public List<CompSewageHandler> Sewers = new List<CompSewageHandler>();
		public List<CompPipe> Pipes = new List<CompPipe>();
		public float WaterCap;
		public float WaterPumpedPerTick;
		public float PumpingCapacitySum;
		public float GroundWaterCapacitySum;
		public float BoilerCapacitySum;
		public float HeatStoreCapacitySum;
		public float HeatingCap;
		public bool HasFilter => false;

		public float WaterStorage => WaterTowers.Where(t => t.WorkingNow).Sum(t => t.WaterStorage);

		public bool HasHotWater => HotWaterTanks.Any(t => t.GetStoreCapacity > 0f && t.HeaterTemp > 0f);

		/// <summary>按 DBH 原版把热量平均分摊到所有还有热水的罐；阀门关闭的罐不参与。</summary>
		public bool PullHotWater(float heatPerTick)
		{
			float draw = heatPerTick * ColonyKit.Modules.Hygiene.HygieneSettings.Current.hotWaterUseMultiplier;
			List<CompHeatStore> tanks = HotWaterTanks
				.Where(t => t.GetStoreCapacity > 0f && t.HeaterTemp > 0f)
				.ToList();
			if (tanks.Count == 0)
			{
				return false;
			}
			float each = draw / tanks.Count;
			for (int i = 0; i < tanks.Count; i++)
			{
				tanks[i].HeaterTemp = Mathf.Max(0f, tanks[i].HeaterTemp - each);
			}
			return true;
		}

		public bool PushSewage(float amount)
		{
			if (amount <= 0f)
			{
				return false;
			}
			List<CompSewageHandler> open = Sewers.Where(s => s != null && !s.Blocked).ToList();
			if (open.Count == 0)
			{
				return false;
			}
			// 按 DBH 的规则选最高优先级的一档，再平均分摊；这里不能再加
			// “当前量 + 本次量 <= 容量”的过滤：那会让化粪池永远到不了容量，
			// CompSewageHandler.CompTickRare 里的 Blocked/溢出分支永远不触发，
			// 溢出的污水也被悄悄丢掉。允许超出容量，由 TickRare 标 Blocked 并溢出。
			int maxPrio = open.Max(s => s.Props.priority);
			List<CompSewageHandler> top = open.Where(s => s.Props.priority == maxPrio).ToList();
			float each = amount / top.Count;
			for (int i = 0; i < top.Count; i++)
			{
				top[i].sewageBuffer += each;
			}
			return true;
		}

		public void InitNet()
		{
			Pipes.Clear();
			WaterTowers.Clear();
			WaterPumps.Clear();
			WaterWells.Clear();
			Heaters.Clear();
			HeatStores.Clear();
			HotWaterTanks.Clear();
			Sewers.Clear();
			foreach (ThingWithComps thing in PipedThings)
			{
				Pipes.AddRange(thing.GetComps<CompPipe>());
				CompWaterStorage tank = thing.TryGetComp<CompWaterStorage>();
				if (tank != null)
				{
					WaterTowers.Add(tank);
				}
				CompWaterPumpingStation pump = thing.TryGetComp<CompWaterPumpingStation>();
				if (pump != null)
				{
					WaterPumps.Add(pump);
				}
				CompWaterInlet inlet = thing.TryGetComp<CompWaterInlet>();
				if (inlet != null)
				{
					WaterWells.Add(inlet);
				}
				CompWaterHeater heater = thing.TryGetComp<CompWaterHeater>();
				if (heater != null)
				{
					Heaters.Add(heater);
				}
				CompHeatStore heatStore = thing.TryGetComp<CompHeatStore>();
				if (heatStore != null)
				{
					HeatStores.Add(heatStore);
					if (heatStore.ForRunningWater)
					{
						HotWaterTanks.Add(heatStore);
					}
				}
				CompSewageHandler sewer = thing.TryGetComp<CompSewageHandler>();
				if (sewer != null)
				{
					Sewers.Add(sewer);
				}
			}
		}

		public bool PullWater(float amount, out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Treated;
			if (amount <= 0f || WaterTowers.Count == 0)
			{
				return false;
			}
			if (WaterStorage < amount)
			{
				return false;
			}
			float remaining = amount;
			foreach (CompWaterStorage tank in WaterTowers.Where(t => t.WorkingNow))
			{
				if (tank.WaterQuality > contam)
				{
					contam = tank.WaterQuality;
				}
				float take = Mathf.Min(tank.WaterStorage, remaining);
				tank.WaterStorage -= take;
				remaining -= take;
				if (remaining <= 0f)
				{
					break;
				}
			}
			return true;
		}

		public float PushWater(float amount)
		{
			float remaining = amount;
			foreach (CompWaterStorage tank in WaterTowers.Where(t => t.WorkingNow && t.space > 0f))
			{
				float put = Mathf.Min(tank.space, remaining);
				tank.WaterStorage += put;
				remaining -= put;
				if (remaining <= 0f)
				{
					break;
				}
			}
			return remaining;
		}

		public void Tick()
		{
			WaterCap = 0f;
			foreach (CompWaterStorage tank in WaterTowers)
			{
				if (tank.WorkingNow)
				{
					WaterCap += tank.Props.WaterStorageCap;
				}
			}
			BoilerCapacitySum = Heaters.Sum(h => h.Capacity);
			HeatStoreCapacitySum = HeatStores.Sum(h => h.GetStoreCapacity);
			HeatingCap = HeatStoreCapacitySum > 0f
				? Mathf.Clamp01(BoilerCapacitySum / HeatStoreCapacitySum)
				: 0f;
			PumpingCapacitySum = WaterPumps.Sum(p => p.Capacity);
			// A pump needs a well: ground water is the hard limit, and no well means no water.
			GroundWaterCapacitySum = WaterWells.Sum(w => w.GroundWaterCapacity);
			WaterPumpedPerTick = GroundWaterCapacitySum > 0f
				? Mathf.Min(PumpingCapacitySum, GroundWaterCapacitySum) / 60000f
				: 0f;

			// Push once per network, not once per tank; otherwise every extra tank multiplies output.
			if (WaterPumpedPerTick > 0f && WaterTowers.Count > 0)
			{
				PushWater(WaterPumpedPerTick);
			}
		}
	}
}
