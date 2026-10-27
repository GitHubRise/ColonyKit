using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>给刷了「施肥」标记的格子施肥（DBH WorkGiver_PlaceFertilizer 的移植，Growing 工作类型）。</summary>
	public class WorkGiver_PlaceFertilizer : WorkGiver_Scanner
	{
		private const int FertilizerPerCell = 10;

		public override bool Prioritized => false;

		private static DesignationDef FertDef =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_Fertilize");

		private static ThingDef BiosolidsDef => DefDatabase<ThingDef>.GetNamedSilentFail("CK_Biosolids");

		public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
		{
			DesignationDef def = FertDef;
			if (def == null)
			{
				yield break;
			}
			foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(def))
			{
				yield return designation.target.Cell;
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			ThingDef biosolids = BiosolidsDef;
			if (FertDef == null || biosolids == null || pawn.carryTracker.MaxStackSpaceEver(biosolids) < FertilizerPerCell)
			{
				return true;
			}
			return FindFuel(pawn, FertilizerPerCell).Count == 0;
		}

		public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
		{
			return CanFertilize(pawn, c);
		}

		private static bool CanFertilize(Pawn pawn, IntVec3 c)
		{
			Map map = pawn.Map;
			if (!c.InBounds(map) || c.Fogged(map) || c.GetEdifice(map) != null)
			{
				return false;
			}
			if (map.designationManager.DesignationAt(c, FertDef) == null)
			{
				return false;
			}
			if (c.GetTerrain(map).fertility <= 0f)
			{
				return false;
			}
			MapComponent_Agriculture comp = MapComponent_Agriculture.Get(map);
			if (comp == null || comp.FertilizerAt(c) > 0f)
			{
				return false;
			}
			if (!pawn.CanReserve(c, 1, -1, null))
			{
				return false;
			}
			if (c.GetZone(map) is Zone_Growing growing && !growing.allowSow)
			{
				JobFailReason.Is("GrowingZoneNotAllowed".Translate());
				return false;
			}
			return true;
		}

		public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
		{
			ThingDef biosolids = BiosolidsDef;
			if (biosolids == null)
			{
				return null;
			}
			int maxCells = Mathf.Max(1, pawn.carryTracker.MaxStackSpaceEver(biosolids) / FertilizerPerCell);
			List<IntVec3> cells = new List<IntVec3> { c };
			foreach (IntVec3 near in GenRadial.RadialCellsAround(c, 10f, useCenter: false))
			{
				if (cells.Count >= maxCells)
				{
					break;
				}
				if (CanFertilize(pawn, near))
				{
					cells.Add(near);
				}
			}
			int needed = FertilizerPerCell * cells.Count;
			List<Thing> fuel = FindFuel(pawn, needed);
			if (fuel.Count == 0)
			{
				return null;
			}
			int total = 0;
			for (int i = 0; i < fuel.Count; i++)
			{
				total += fuel[i].stackCount;
			}
			int affordable = Mathf.Min(total / FertilizerPerCell, cells.Count);
			if (affordable <= 0)
			{
				return null;
			}
			if (affordable < cells.Count)
			{
				cells = cells.GetRange(0, affordable);
			}
			Job job = JobMaker.MakeJob(
				DefDatabase<JobDef>.GetNamedSilentFail("CK_PlaceFertilizer"), c);
			job.count = Mathf.Min(total, affordable * FertilizerPerCell);
			job.targetQueueA = cells.ConvertAll(cell => new LocalTargetInfo(cell));
			job.targetQueueB = fuel.ConvertAll(thing => new LocalTargetInfo(thing));
			return job;
		}

		private static List<Thing> FindFuel(Pawn pawn, int needed)
		{
			List<Thing> result = new List<Thing>();
			ThingDef biosolids = BiosolidsDef;
			if (biosolids == null)
			{
				return result;
			}
			int total = 0;
			List<Thing> candidates = pawn.Map.listerThings.ThingsOfDef(biosolids);
			candidates.SortBy(t => t.Position.DistanceToSquared(pawn.Position));
			for (int i = 0; i < candidates.Count; i++)
			{
				Thing thing = candidates[i];
				if (thing.IsForbidden(pawn) || !pawn.CanReserve(thing))
				{
					continue;
				}
				if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Some))
				{
					continue;
				}
				result.Add(thing);
				total += thing.stackCount;
				if (total >= needed)
				{
					break;
				}
			}
			return result;
		}
	}
}
