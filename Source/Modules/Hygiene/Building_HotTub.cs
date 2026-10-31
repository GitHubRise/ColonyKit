using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_HotTub 的移植：2×2 的热水按摩浴缸。
	/// 第一次使用时从水网注水（1000 L），自己加热、不需要排污口；
	/// 泡澡期间清洁卫生、回休息值、做水疗（healscars），并算作「水疗」娱乐。
	/// 视觉上只保留了「注满水时画一层水面」，DBH 的焦散/泡泡特效没搬。
	/// </summary>
	public class Building_HotTub : Building_AssignableFixture
	{
		public HashSet<Pawn> occupants = new HashSet<Pawn>();

		public float cleanedPerTick = 0.0001f;

		public ContaminationLevel contaminated;

		public CompPipe PipeComp;

		public int seatCount = 2;

		public float usage = 1000f;

		public float WaterInTub;

		private Graphic fullcache;

		public bool IsFull => WaterInTub >= usage;

		public float waterUsedPerTick => 1f;

		public override FixtureType fixture => FixtureType.Bath;

		public override FixtureQuality Quality => FixtureQuality.fine;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			PipeComp = GetComp<CompPipe>();
			powerComp = GetComp<CompPowerTrader>();
		}

		public override void Notify_ColorChanged()
		{
			base.Notify_ColorChanged();
			fullcache = null;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref WaterInTub, "WaterInTub", 0f);
			Scribe_Values.Look(ref contaminated, "tubContamination", ContaminationLevel.Untreated);
		}

		protected override void Tick()
		{
			base.Tick();
			// 有水时每 60 tick 在水面随机点冒一串泡泡（DBH Mote_MyBubbles）。
			if (WaterInTub <= 0f || !this.IsHashIntervalTick(60))
			{
				return;
			}
			FleckDef bubbles = DefDatabase<FleckDef>.GetNamedSilentFail("CK_MoteBubbles");
			if (bubbles == null)
			{
				return;
			}
			Vector3 loc = this.TrueCenter();
			loc.y += 0.63f;
			loc.x += Rand.Range(-0.75f, 0.75f);
			loc.z += Rand.Range(-0.75f, 0.75f);
			if (!loc.ShouldSpawnMotesAt(Map))
			{
				return;
			}
			FleckCreationData data = FleckMaker.GetDataStatic(loc, Map, bubbles, Rand.Range(0.2f, 0.5f));
			Map.flecks.CreateFleck(data);
		}

		public AcceptanceReport Working()
		{
			if (powerComp != null && !powerComp.PowerOn)
			{
				return "NoPower".Translate();
			}
			if (PipeComp?.pipeNet == null)
			{
				return "CK.Hygiene.NoWaterSupply".Translate();
			}
			return true;
		}

		public IntVec3 GetSleepingSlotPos(int index)
		{
			return BedUtility.GetSleepingSlotPos(index, Position, Rotation, def.size);
		}

		public Pawn GetCurOccupantAt(IntVec3 pos)
		{
			for (int i = 0; i < seatCount; i++)
			{
				if (GetSleepingSlotPos(i) == pos)
				{
					return GetCurOccupant(i);
				}
			}
			return null;
		}

		public Pawn GetCurOccupant(int slotIndex)
		{
			if (!Spawned)
			{
				return null;
			}
			IntVec3 slot = GetSleepingSlotPos(slotIndex);
			List<Thing> things = Map.thingGrid.ThingsListAt(slot);
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Pawn pawn && pawn.CurJob != null
					&& pawn.jobs.posture == PawnPosture.LayingOnGroundFaceUp)
				{
					return pawn;
				}
			}
			return null;
		}

		/// <summary>注水：每 tick 从水网抽 1 L，直到 1000。返回 false 表示水不够。</summary>
		public bool TryFillTub()
		{
			if (PipeComp?.pipeNet == null)
			{
				return false;
			}
			if (!PipeComp.pipeNet.PullWater(waterUsedPerTick, out ContaminationLevel contam))
			{
				return false;
			}
			contaminated = contam;
			WaterInTub += waterUsedPerTick;
			return true;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (!IsFull)
			{
				return;
			}
			if (fullcache == null)
			{
				fullcache = GraphicDatabase.Get<Graphic_Single>(
					"Things/Building/Hygiene/CK_HotTubWater",
					ShaderDatabase.CutoutComplex,
					def.graphicData.drawSize,
					Color.white,
					Color.white);
			}
			Vector3 pos = DrawPos;
			pos.y += 0.03f;
			Graphics.DrawMesh(fullcache.MeshAt(Rotation), pos, Rotation.AsQuat, fullcache.MatAt(Rotation), 0);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			string water = "CK.Hygiene.HotTubWater".Translate(
				WaterInTub.ToString("F0"), usage.ToString("F0"));
			return text.NullOrEmpty() ? water : text + "\n" + water;
		}
	}
}
