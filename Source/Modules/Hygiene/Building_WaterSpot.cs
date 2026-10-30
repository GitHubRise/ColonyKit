using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 免费取水点（蓝色地贴，类似屠宰点）：无需材料/研究，放下就能直接喝水、洗手、打水。
	/// 清单使用原版 BillStack / Bill_Production，配合两条 RecipeDef：
	/// 「取水」每次打 1 桶，「批量取水」一次最多打 5 桶；重复模式仍是原版的
	/// 做 X 次 / 维持 X 个 / 不限次数。
	/// </summary>
	public class Building_WaterSpot : Building_Basin, IBillGiver
	{
		private BillStack billStack;

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

		/// <summary>无限地表水：喝/洗手不消耗，水桶打水按 CompWaterInlet 的井水处理。</summary>
		public override bool TryUseWater(out ContaminationLevel contam)
		{
			contam = ContaminationLevel.Untreated;
			return true;
		}

		public BillStack BillStack => billStack ?? (billStack = new BillStack(this));

		public IEnumerable<IntVec3> IngredientStackCells => Enumerable.Empty<IntVec3>();

		public bool CurrentlyUsableForBills()
		{
			return Spawned && !this.IsBurning();
		}

		public bool UsableForBillsAfterFueling()
		{
			return true;
		}

		public void Notify_BillDeleted(Bill bill)
		{
		}

		public bool NeedsWork => BillStack.AnyShouldDoNow;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (billStack == null)
			{
				billStack = new BillStack(this);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref billStack, "billStack", this);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && billStack != null)
			{
				billStack.billGiver = this;
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
			int bills = BillStack.Count;
			if (bills > 0)
			{
				Append("CK.Hygiene.WaterSpot.Bills".Translate(
					bills,
					WaterContainerUtility.CountFullBuckets(Map)));
			}
			return builder.ToString().TrimEndNewlines();
		}
	}
}
