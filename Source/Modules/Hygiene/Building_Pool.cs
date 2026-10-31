using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 游泳池（DBH Building_Pool / Building_FillableThing 的移植）：
	/// 铺地式 9×5，先用管网水灌满（容量 9000 L，5 L/tick），水温 19℃ 以上才能下水；
	/// 建在蒸汽间歇泉上会替换掉喷泉、成为「温水泳池」（和 DBH 一样）。
	/// </summary>
	public class Building_Pool : Building_AssignableFixture
	{
		public const float Capacity = 9000f;

		public const float FillPerTick = 5f;

		public float waterLevel;

		public HashSet<Pawn> occupants = new HashSet<Pawn>();

		public IntVec3 geyserLoc = IntVec3.Invalid;

		public float cleanedPerTick = 0.0001f;

		private Graphic waterGraphicCache;

		public override FixtureType fixture => FixtureType.Bath;

		public override FixtureQuality Quality => FixtureQuality.fine;

		public bool Filled => waterLevel >= Capacity;

		public bool HasWater => waterLevel > 0f;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			foreach (IntVec3 cell in this.OccupiedRect())
			{
				Building_SteamGeyser geyser = cell.GetFirstThing<Building_SteamGeyser>(map);
				if (geyser != null)
				{
					geyserLoc = geyser.Position;
					geyser.DeSpawn();
					break;
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterLevel, "waterLevel", 0f);
			Scribe_Values.Look(ref geyserLoc, "geyserLoc", IntVec3.Invalid);
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			if (geyserLoc.IsValid && Map != null)
			{
				GenSpawn.Spawn(ThingDefOf.SteamGeyser, geyserLoc, Map);
			}
			base.DeSpawn(mode);
		}

		public bool GoodTemp()
		{
			return geyserLoc.IsValid || AmbientTemperature > 19f;
		}

		public override AcceptanceReport Working(float waterUsed = 0f)
		{
			if (!GoodTemp())
			{
				return "CK.Hygiene.PoolTooCold".Translate(AmbientTemperature.ToStringTemperature());
			}
			if (!HasWater)
			{
				return "CK.Hygiene.PoolEmpty".Translate();
			}
			return true;
		}

		protected override void Tick()
		{
			base.Tick();
			// 灌水：接在水网上就一点点抽，抽满为止（DBH 的 FillableThing 同款行为）。
			if (waterLevel < Capacity && this.IsHashIntervalTick(30))
			{
				CompPipe pipe = GetComp<CompPipe>();
				if (pipe?.pipeNet != null && pipe.pipeNet.PullWater(FillPerTick, out _))
				{
					waterLevel = Mathf.Min(Capacity, waterLevel + FillPerTick);
				}
			}
			// 水面每 60 tick 随机一处水花，限频避免每 tick 生成。
			if (HasWater && this.IsHashIntervalTick(60))
			{
				ThrowWaterSplash();
			}
		}

		private void ThrowWaterSplash()
		{
			FleckDef splashes = DefDatabase<FleckDef>.GetNamedSilentFail("CK_MoteWaterSplashes");
			if (splashes == null)
			{
				return;
			}
			Vector3 loc = this.OccupiedRect().RandomCell.ToVector3Shifted();
			loc.y += 0.05f;
			if (!loc.ShouldSpawnMotesAt(Map))
			{
				return;
			}
			FleckMaker.Static(loc, Map, splashes, Rand.Range(0.6f, 1f));
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (!HasWater)
			{
				return;
			}
			if (waterGraphicCache == null)
			{
				waterGraphicCache = GraphicDatabase.Get<Graphic_Single>(
					"Things/Building/Hygiene/CK_SwimmingPool_water",
					ShaderDatabase.Cutout,
					new Vector2(def.size.x, def.size.z),
					Color.white,
					Color.white);
			}
			Vector3 pos = DrawPos;
			pos.y += 0.02f;
			Graphics.DrawMesh(MeshPool.plane10, pos, Rotation.AsQuat, waterGraphicCache.MatAt(Rotation), 0);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			string water = "CK.Hygiene.PoolWater".Translate(waterLevel.ToString("F0"), Capacity.ToString("F0"));
			string result = text.NullOrEmpty() ? water : text + "\n" + water;
			if (!GoodTemp())
			{
				result += "\n" + "CK.Hygiene.PoolTooCold".Translate(AmbientTemperature.ToStringTemperature());
			}
			else if (geyserLoc.IsValid)
			{
				result += "\n" + "CK.Hygiene.PoolGeyserHeated".Translate();
			}
			return result;
		}
	}
}
