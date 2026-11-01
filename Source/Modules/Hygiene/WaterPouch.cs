using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>一个 pawn 的水袋存档数据。</summary>
	public class WaterPouchRecord : IExposable
	{
		public float water;

		/// <summary>袋里最脏的一档水；混合时取最脏，喝的时候按这一档做污染判定。</summary>
		public byte contamination = (byte)ContaminationLevel.Treated;

		public ContaminationLevel Quality
		{
			get => (ContaminationLevel)contamination;
			set => contamination = (byte)value;
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref water, "water", 0f);
			Scribe_Values.Look(ref contamination, "contamination", (byte)ContaminationLevel.Treated);
		}
	}

	/// <summary>
	/// 随身水袋：每个类人生物一个 0–100 L 的独立水袋，不占物品栏负重。
	/// 殖民者/奴隶/囚徒开局 30 L；访客、商队、敌人开局 0 L，靠殖民地水源或地表水补。
	/// 数据挂在 GameComponent 上，随存档序列化，不依赖 ThingDef 的 comps。
	/// </summary>
	public class GameComponent_WaterPouches : GameComponent
	{
		public const float StartWater = 30f;

		public const float FillTarget = 30f;

		public const float MaxWater = 100f;

		private Dictionary<Pawn, WaterPouchRecord> pouches = new Dictionary<Pawn, WaterPouchRecord>();

		public GameComponent_WaterPouches(Game game)
		{
		}

		public static GameComponent_WaterPouches Instance =>
			Current.Game?.GetComponent<GameComponent_WaterPouches>();

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref pouches, "ckWaterPouches", LookMode.Reference, LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (pouches == null)
				{
					pouches = new Dictionary<Pawn, WaterPouchRecord>();
					return;
				}
				List<Pawn> broken = null;
				foreach (KeyValuePair<Pawn, WaterPouchRecord> pair in pouches)
				{
					if (pair.Key == null || pair.Value == null)
					{
						broken = broken ?? new List<Pawn>();
						broken.Add(pair.Key);
					}
				}
				if (broken != null)
				{
					foreach (Pawn pawn in broken)
					{
						pouches.Remove(pawn);
					}
				}
			}
		}

		public override void GameComponentTick()
		{
			if (Find.TickManager.TicksGame % 60000 != 0)
			{
				return;
			}
			List<Pawn> dead = null;
			foreach (Pawn pawn in pouches.Keys)
			{
				if (pawn == null || pawn.Destroyed)
				{
					dead = dead ?? new List<Pawn>();
					dead.Add(pawn);
				}
			}
			if (dead == null)
			{
				return;
			}
			foreach (Pawn pawn in dead)
			{
				pouches.Remove(pawn);
			}
		}

		public WaterPouchRecord Get(Pawn pawn)
		{
			if (pawn?.needs == null
				|| pawn.RaceProps.Animal
				|| pawn.needs.TryGetNeed<Need_Thirst>() == null)
			{
				return null;
			}
			if (!pouches.TryGetValue(pawn, out WaterPouchRecord record))
			{
				record = new WaterPouchRecord
				{
					water = DefaultWaterFor(pawn),
					Quality = ContaminationLevel.Treated
				};
				pouches[pawn] = record;
			}
			return record;
		}

		private static float DefaultWaterFor(Pawn pawn)
		{
			if (pawn.Faction == Faction.OfPlayerSilentFail
				|| pawn.IsPrisonerOfColony
				|| pawn.IsQuestLodger())
			{
				return StartWater;
			}
			return 0f;
		}
	}

	/// <summary>水袋的读写入口；没有 GameComponent（例如主菜单）时全部安全返回 0。</summary>
	public static class WaterPouchUtility
	{
		public const float MaxWater = GameComponent_WaterPouches.MaxWater;

		public const float FillTarget = GameComponent_WaterPouches.FillTarget;

		public static float GetWater(Pawn pawn)
		{
			return GameComponent_WaterPouches.Instance?.Get(pawn)?.water ?? 0f;
		}

		public static ContaminationLevel GetQuality(Pawn pawn)
		{
			return GameComponent_WaterPouches.Instance?.Get(pawn)?.Quality
				?? ContaminationLevel.Treated;
		}

		/// <summary>往水袋里加水，超过 100 L 的部分直接丢掉（水袋上限）。</summary>
		public static void AddWater(Pawn pawn, float liters, ContaminationLevel quality)
		{
			if (liters <= 0f)
			{
				return;
			}
			WaterPouchRecord record = GameComponent_WaterPouches.Instance?.Get(pawn);
			if (record == null)
			{
				return;
			}
			record.water = Mathf.Clamp(record.water + liters, 0f, MaxWater);
			if (quality > record.Quality)
			{
				record.Quality = quality;
			}
		}

		/// <summary>从水袋取水，返回实际取到的升数。</summary>
		public static float TakeWater(Pawn pawn, float liters)
		{
			if (liters <= 0f)
			{
				return 0f;
			}
			WaterPouchRecord record = GameComponent_WaterPouches.Instance?.Get(pawn);
			if (record == null || record.water <= 0f)
			{
				return 0f;
			}
			float taken = Mathf.Min(record.water, liters);
			record.water -= taken;
			return taken;
		}
	}
}
