using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 取水点：接在水网上的公共水龙头 + 150 L 小水箱。
	/// 殖民者可以直接在这里喝水、洗手，也能用水桶接水带走；
	/// 在检视页设置「取水次数」后，会自动派活拿水桶去同网水井打水倒回这里。
	/// 同网有工作的水泵时打水快，没有水泵只能手动慢慢打。
	/// </summary>
	public class Building_WaterPoint : Building_Basin
	{
		public const int MaxDraws = 99;

		/// <summary>低于这个水量就不值得再派一趟打水，避免半桶空跑。</summary>
		public const float MinDrawLiters = 20f;

		public int targetDraws;

		public int deliveredDraws;

		private CompWaterStorage storage;

		public CompWaterStorage Storage => storage ?? (storage = GetComp<CompWaterStorage>());

		public float StoredWater => Storage?.WaterStorage ?? 0f;

		public float Capacity => Storage?.Props.WaterStorageCap ?? 0f;

		public float Space => Storage?.space ?? 0f;

		public int RemainingDraws => Mathf.Max(0, targetDraws - deliveredDraws);

		public bool HasPendingDraws => RemainingDraws > 0;

		public bool Pumped => pipe?.pipeNet?.WaterPumps.Any(p => p != null && p.WorkingNow) == true;

		public override bool CanSetGender => false;

		public override bool ShowFixtureGizmos => false;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		public override float waterUsed => 2f * HygieneSettings.Current.waterUseMultiplier;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			storage = GetComp<CompWaterStorage>();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref targetDraws, "targetDraws", 0);
			Scribe_Values.Look(ref deliveredDraws, "deliveredDraws", 0);
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

			Append(Pumped
				? "CK.Hygiene.WaterPoint.FlowFast".Translate()
				: "CK.Hygiene.WaterPoint.FlowSlow".Translate());
			if (targetDraws > 0)
			{
				Append("CK.Hygiene.WaterPoint.DrawProgress".Translate(deliveredDraws, targetDraws));
				Append(FindWellForDraw() != null
					? "CK.Hygiene.WaterPoint.WellReady".Translate()
					: "CK.Hygiene.WaterPoint.NoWell".Translate());
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
		}
	}
}
