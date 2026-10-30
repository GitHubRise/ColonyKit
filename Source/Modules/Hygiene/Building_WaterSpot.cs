using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public enum WaterSpotTaskType
	{
		/// <summary>取水 N 次：完成 N 次打水就算完。</summary>
		Draw,

		/// <summary>维持 N 个满桶：殖民地满桶少于 N 就继续打水。</summary>
		Maintain
	}

	public class WaterSpotTask : IExposable
	{
		public WaterSpotTaskType type;
		public int target = 1;
		public int completed;

		public void ExposeData()
		{
			Scribe_Values.Look(ref type, "type", WaterSpotTaskType.Draw);
			Scribe_Values.Look(ref target, "target", 1);
			Scribe_Values.Look(ref completed, "completed", 0);
		}
	}

	/// <summary>
	/// 免费取水点（蓝色地贴，类似屠宰点）：无需材料/研究，放下去就能直接喝水/洗手/打水。
	/// 另带一个「清单」：可以添加「取水 N 次」或「维持 N 个满桶」任务，
	/// WorkGiver_FillWaterSpot 会派小人拿空桶来这里打满。
	/// </summary>
	public class Building_WaterSpot : Building_Basin
	{
		public List<WaterSpotTask> tasks = new List<WaterSpotTask>();

		private int countCheckTick = -9999;

		private int filledCount;

		public override bool CanSetGender => false;

		public override bool ShowFixtureGizmos => false;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		public override float waterUsed => 2f * HygieneSettings.Current.waterUseMultiplier;

		public override PathEndMode InteractionPathMode => PathEndMode.Touch;

		/// <summary>免费取水点不需要水网：永远可用。</summary>
		public override bool HasWaterSupply => true;

		public override AcceptanceReport Working(float waterUsed = 0f)
		{
			return true;
		}

		/// <summary>无限地表水：喝/洗手不消耗，水桶打水由 CompWaterInlet 按井水处理。</summary>
		public override bool TryUseWater(out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Untreated;
			return true;
		}

		public List<WaterSpotTask> Tasks => tasks;

		public bool NeedsWork
		{
			get
			{
				if (tasks == null)
				{
					return false;
				}
				if (tasks.Any(t => t.type == WaterSpotTaskType.Draw && t.completed < t.target))
				{
					return true;
				}
				return tasks.Any(t => t.type == WaterSpotTaskType.Maintain && CountFilledBuckets() < t.target);
			}
		}

		public void AddTask(WaterSpotTaskType type, int count)
		{
			count = Mathf.Clamp(count, 1, 99);
			if (tasks == null)
			{
				tasks = new List<WaterSpotTask>();
			}
			WaterSpotTask task = tasks.FirstOrDefault(t => t.type == type);
			if (task == null)
			{
				tasks.Add(new WaterSpotTask { type = type, target = count });
			}
			else
			{
				task.target = Mathf.Clamp(task.target + count, 1, 99);
			}
		}

		public void AdjustTask(WaterSpotTask task, int delta)
		{
			if (task == null || tasks == null)
			{
				return;
			}
			task.target = Mathf.Clamp(task.target + delta, 0, 99);
			if (task.target <= 0)
			{
				tasks.Remove(task);
				return;
			}
			if (task.completed > task.target)
			{
				task.completed = task.target;
			}
		}

		public void RemoveTask(WaterSpotTask task)
		{
			tasks?.Remove(task);
		}

		/// <summary>完成一次打水：只推进第一个还没完成的「取水 N 次」任务；维持任务按满桶实时统计。</summary>
		public void NotifyBucketFilled()
		{
			if (tasks == null)
			{
				return;
			}
			foreach (WaterSpotTask task in tasks)
			{
				if (task.type == WaterSpotTaskType.Draw && task.completed < task.target)
				{
					task.completed++;
					return;
				}
			}
		}

		/// <summary>殖民地当前满桶数（地图上的 + 殖民者物品栏里的），每 250 tick 缓存。</summary>
		public int CountFilledBuckets()
		{
			Map map = Map;
			if (map == null)
			{
				return 0;
			}
			int now = Find.TickManager?.TicksGame ?? 0;
			if (countCheckTick < 0 || now - countCheckTick >= 250)
			{
				countCheckTick = now;
				filledCount = CountFilledBucketsNow(map);
			}
			return filledCount;
		}

		private static int CountFilledBucketsNow(Map map)
		{
			int count = 0;
			ThingDef bucketDef = WaterContainerUtility.BucketDef;
			if (bucketDef == null)
			{
				return 0;
			}
			foreach (Thing thing in map.listerThings.ThingsOfDef(bucketDef))
			{
				if (IsFullBucket(thing))
				{
					count++;
				}
			}
			foreach (Pawn pawn in map.mapPawns.FreeColonists)
			{
				if (pawn.inventory?.innerContainer == null)
				{
					continue;
				}
				foreach (Thing thing in pawn.inventory.innerContainer)
				{
					if (thing.def == bucketDef && IsFullBucket(thing))
					{
						count++;
					}
				}
			}
			return count;
		}

		private static bool IsFullBucket(Thing thing)
		{
			CompBasinWater container = thing.TryGetComp<CompBasinWater>();
			return container != null && container.Water >= 59f;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref tasks, "tasks", LookMode.Deep);
			if (tasks == null)
			{
				tasks = new List<WaterSpotTask>();
			}
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

			Append("CK.Hygiene.WaterSpot.FreeWater".Translate());
			Append("CK.Hygiene.WaterSpot.Drinkable".Translate());
			if (tasks != null && tasks.Count > 0)
			{
				Append("CK.Hygiene.WaterSpot.Tasks".Translate(tasks.Count, CountFilledBuckets()));
			}
			return builder.ToString().TrimEndNewlines();
		}
	}
}
