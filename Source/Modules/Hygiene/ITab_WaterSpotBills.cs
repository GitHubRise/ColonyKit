using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 免费取水点的「清单」页：直接复用原版 BillStack.DoListing / Bill_Production，
	/// 所以按钮、排序、删除、暂停和「做 X 次 / 维持 X 个 / 不限次数」配置都和屠宰点一致。
	/// </summary>
	public class ITab_WaterSpotBills : ITab
	{
		private float viewHeight = 1000f;

		private Vector2 scrollPosition;

		private static readonly Vector2 WinSize = new Vector2(420f, 480f);

		private IBillGiver Giver => SelThing as IBillGiver;

		public ITab_WaterSpotBills()
		{
			size = WinSize;
			labelKey = "TabBills";
		}

		protected override void FillTab()
		{
			IBillGiver giver = Giver;
			if (giver == null)
			{
				return;
			}
			Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
			giver.BillStack.DoListing(rect, OptionsMaker, ref scrollPosition, ref viewHeight);
		}

		private List<FloatMenuOption> OptionsMaker()
		{
			List<FloatMenuOption> opts = new List<FloatMenuOption>();
			ThingDef def = SelThing?.def;
			if (def?.AllRecipes != null)
			{
				foreach (RecipeDef recipe in def.AllRecipes)
				{
					if (!recipe.AvailableNow || !recipe.AvailableOnNow(SelThing))
					{
						continue;
					}
					RecipeDef captured = recipe;
					opts.Add(new FloatMenuOption(
						captured.LabelCap,
						() =>
						{
							Bill bill = captured.MakeNewBill();
							Giver.BillStack.AddBill(bill);
						},
						iconTex: captured.UIIcon,
						shownItemForIcon: captured.UIIconThing));
				}
			}
			if (opts.Count == 0)
			{
				opts.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
			}
			return opts;
		}
	}
}
