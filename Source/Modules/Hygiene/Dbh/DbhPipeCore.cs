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
				if (pipeNetRef == null && MapComp != null)
				{
					MapComp.PipeComp.RegenPipeGrids();
				}
				return pipeNetRef;
			}
		}

		public bool closed => flicker != null && !flicker.SwitchIsOn;

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
		private bool dirty = true;

		public HygienePipeMapComp(Map map) : base(map)
		{
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			RegenPipeGrids();
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
			foreach (IntVec3 cell in a.OccupiedRect())
			{
				foreach (IntVec3 other in b.OccupiedRect())
				{
					if (cell.DistanceTo(other) <= 1.5f)
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
		public List<CompSewageHandler> Sewers = new List<CompSewageHandler>();
		public List<CompPipe> Pipes = new List<CompPipe>();
		public float WaterCap;
		public float WaterPumpedPerTick;
		public float PumpingCapacitySum;
		public float GroundWaterCapacitySum;
		public bool HasFilter => false;

		public float WaterStorage => WaterTowers.Sum(t => t.WaterStorage);

		public bool HasHotWater => Heaters.Any(h => h.WorkingNow);

		public bool PullHotWater(float heatPerTick)
		{
			return HasHotWater;
		}

		public bool PushSewage(float amount)
		{
			foreach (CompSewageHandler sewer in Sewers)
			{
				if (sewer != null && !sewer.Blocked && sewer.sewageBuffer + amount <= sewer.Props.capacity)
				{
					sewer.sewageBuffer += amount;
					return true;
				}
			}
			return false;
		}

		public void InitNet()
		{
			Pipes.Clear();
			WaterTowers.Clear();
			WaterPumps.Clear();
			WaterWells.Clear();
			Heaters.Clear();
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
			if (WaterTowers.Sum(t => t.WaterStorage) < amount)
			{
				return false;
			}
			float remaining = amount;
			foreach (CompWaterStorage tank in WaterTowers)
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
			foreach (CompWaterStorage tank in WaterTowers.Where(t => t.space > 0f))
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
				WaterCap += tank.Props.WaterStorageCap;
			}
			PumpingCapacitySum = WaterPumps.Sum(p => p.Capacity);
			GroundWaterCapacitySum = WaterWells.Count > 0
				? WaterWells.Sum(w => w.GroundWaterCapacity)
				: PumpingCapacitySum;
			WaterPumpedPerTick = Mathf.Min(PumpingCapacitySum, GroundWaterCapacitySum) / 60000f;
		}
	}
}
