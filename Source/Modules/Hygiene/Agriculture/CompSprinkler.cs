using System.Collections.Generic;
using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	public class CompProperties_Sprinkler : CompProperties
	{
		/// <summary>每格耗水量（升），满半径一天一次。</summary>
		public float usagePerCell = 0.16f;

		/// <summary>默认喷洒半径（格）。</summary>
		public float radius = 6.9f;

		public CompProperties_Sprinkler()
		{
			compClass = typeof(CompSprinkler);
		}
	}

	/// <summary>
	/// DBH 灌溉洒水器的移植：接水网，每天早上 5 点喷一次，把半径内的耕地格子灌满（肥力 ×灌溉强度）。
	/// 水量按 0.16 升/格从水网里抽；水不够就这天不喷。
	/// </summary>
	public class CompSprinkler : ThingComp
	{
		public float radius = 6.9f;

		private CompPipe pipe;

		private CompFlickable flicker;

		public CompProperties_Sprinkler Props => (CompProperties_Sprinkler)props;

		public float Usage => Props.usagePerCell * GenRadial.NumCellsInRadius(radius);

		public bool HasWater => pipe?.pipeNet != null && pipe.pipeNet.WaterStorage >= Usage;

		private bool SwitchedOn => flicker == null || flicker.SwitchIsOn;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			pipe = parent.GetComp<CompPipe>();
			flicker = parent.GetComp<CompFlickable>();
			if (respawningAfterLoad)
			{
				radius = Mathf.Clamp(radius, 1.9f, Props.radius);
			}
			else
			{
				radius = Props.radius;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref radius, "ckSprinklerRadius", 6.9f);
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!parent.IsHashIntervalTick(60) || !parent.Spawned)
			{
				return;
			}
			// 和 DBH 一样每天 5 点喷一次；白天蒸发完，第二天再补。
			if (GenLocalDate.HourOfDay(parent.Map.Tile) == 5 && SwitchedOn)
			{
				TrySpray();
			}
		}

		public bool TrySpray()
		{
			Map map = parent.Map;
			if (map == null || pipe?.pipeNet == null || !HasWater)
			{
				return false;
			}
			float remaining = Usage;
			foreach (CompWaterStorage tower in pipe.pipeNet.WaterTowers.InRandomOrder())
			{
				float taken = Mathf.Min(tower.WaterStorage, remaining);
				tower.WaterStorage -= taken;
				if (tower.WaterStorage < 0f || float.IsNaN(tower.WaterStorage))
				{
					tower.WaterStorage = 0f;
				}
				remaining -= taken;
				if (remaining <= 0f)
				{
					break;
				}
			}
			MapComponent_Agriculture comp = MapComponent_Agriculture.Get(map);
			if (comp == null)
			{
				return true;
			}
			int cells = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < cells; i++)
			{
				IntVec3 cell = parent.Position + GenRadial.RadialPattern[i];
				if (!cell.InBounds(map)
					|| !GenSight.LineOfSight(parent.Position, cell, map)
					|| cell.GetTerrain(map).fertility <= 0f)
				{
					continue;
				}
				comp.AddIrrigation(cell, 100f);
			}
			return true;
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Agriculture.SprinklerStatus".Translate(
				radius.ToString("0.0"), Usage.ToString("F0"),
				HasWater ? "CK.Agriculture.WaterOk".Translate().ToString()
					: "CK.Agriculture.NoWater".Translate().ToString());
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			yield return new Command_Action
			{
				defaultLabel = "CK.Agriculture.RadiusDown".Translate(),
				defaultDesc = "CK.Agriculture.RadiusDownDesc".Translate(),
				action = () => radius = Mathf.Max(1.9f, radius - 1f)
			};
			yield return new Command_Action
			{
				defaultLabel = "CK.Agriculture.RadiusUp".Translate(),
				defaultDesc = "CK.Agriculture.RadiusUpDesc".Translate(),
				action = () => radius = Mathf.Min(Props.radius, radius + 1f)
			};
		}

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			GenDraw.DrawRadiusRing(parent.Position, radius);
		}
	}
}
