using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_IceBlock : CompProperties
	{
		/// <summary>融化后能给水网补多少升水。</summary>
		public float WaterLiters = 10f;

		public CompProperties_IceBlock()
		{
			compClass = typeof(CompIceBlock);
		}
	}

	/// <summary>
	/// 冰块：10 L 水冻成的一块冰，不腐烂、不降解。放在取水点附近且温度高于 0°C 时，
	/// 可以在检视面板点「融化成水」，把水补回取水点的水箱。
	/// </summary>
	public class CompIceBlock : ThingComp
	{
		public CompProperties_IceBlock Props => (CompProperties_IceBlock)props;

		private Building_WaterPoint FindPoint()
		{
			if (!parent.Spawned)
			{
				return null;
			}
			Building_WaterPoint best = null;
			float bestDistance = 10f;
			foreach (Building_WaterPoint point in parent.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_WaterPoint>())
			{
				if (point == null || point.Space < Props.WaterLiters)
				{
					continue;
				}
				float distance = point.Position.DistanceTo(parent.Position);
				if (distance <= bestDistance)
				{
					bestDistance = distance;
					best = point;
				}
			}
			return best;
		}

		private void Melt(Building_WaterPoint point)
		{
			if (point == null)
			{
				return;
			}
			float added = point.AddWater(Props.WaterLiters, ContaminationLevel.Untreated);
			if (added < Props.WaterLiters - 0.01f)
			{
				return;
			}
			Messages.Message(
				"CK.Hygiene.Ice.Melted".Translate(added.ToString("F0")),
				parent,
				MessageTypeDefOf.PositiveEvent);
			// 一垛冰代表多块冰：每次只融一块，垛里还有就减一，最后一块才销毁整件。
			if (parent.stackCount > 1)
			{
				parent.stackCount--;
			}
			else
			{
				parent.Destroy(DestroyMode.Vanish);
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			bool spawned = parent.Spawned;
			bool warm = spawned && parent.Position.GetTemperature(parent.Map) > 0f;
			Building_WaterPoint point = spawned ? FindPoint() : null;
			Command_Action melt = new Command_Action
			{
				defaultLabel = "CK.Hygiene.Ice.Melt".Translate(),
				defaultDesc = "CK.Hygiene.Ice.MeltDesc".Translate(Props.WaterLiters.ToString("F0")),
				action = () => Melt(point)
			};
			if (!warm)
			{
				melt.Disabled = true;
				melt.disabledReason = "CK.Hygiene.Ice.TooCold".Translate();
			}
			else if (point == null)
			{
				melt.Disabled = true;
				melt.disabledReason = "CK.Hygiene.Ice.NoPoint".Translate();
			}
			yield return melt;
		}
	}
}
