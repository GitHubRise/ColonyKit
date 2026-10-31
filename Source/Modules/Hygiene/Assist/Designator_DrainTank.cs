using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「排空水箱」设计器：点选或框选带 <see cref="CompWaterStorage"/> 的储水设施，
	/// 刷上 <c>CK_DrainTankDes</c> 标记后由工人把水排到脚下（DBH 的排水标记的独立设计器版）。
	/// </summary>
	public class Designator_DrainTank : Designator
	{
		public Designator_DrainTank()
		{
			defaultLabel = "CK.Hygiene.Assist.DrainTank".Translate();
			defaultDesc = "CK.Hygiene.Assist.DrainTankDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/CK_SewageOutlet");
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
			soundDragChanged = null;
			soundSucceeded = SoundDefOf.Designate_PlanAdd;
		}

		public override DrawStyleCategoryDef DrawStyleCategory =>
			DrawStyleCategoryDefOf.FilledRectangle;

		public override bool DragDrawMeasurements => true;

		private static DesignationDef Def =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_DrainTankDes");

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(Map))
			{
				return false;
			}
			Thing drainable = DrainableInCell(c);
			return drainable != null && CanDesignateThing(drainable).Accepted;
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			Thing drainable = DrainableInCell(c);
			if (drainable != null)
			{
				DesignateThing(drainable);
			}
		}

		public override AcceptanceReport CanDesignateThing(Thing t)
		{
			if (!(t is Building))
			{
				return false;
			}
			CompWaterStorage storage = t.TryGetComp<CompWaterStorage>();
			if (storage == null || storage.WaterStorage <= 0.01f)
			{
				return false;
			}
			if (!DebugSettings.godMode && t.Faction != Faction.OfPlayer)
			{
				if (t.Faction != null)
				{
					return false;
				}
				if (!t.ClaimableBy(Faction.OfPlayer))
				{
					return false;
				}
			}
			DesignationDef def = Def;
			if (def == null || Map.designationManager.DesignationOn(t, def) != null)
			{
				return false;
			}
			if (Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			return true;
		}

		public override void DesignateThing(Thing t)
		{
			DesignationDef def = Def;
			if (def != null)
			{
				Map.designationManager.AddDesignation(new Designation(t, def));
			}
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}

		private Thing DrainableInCell(IntVec3 c)
		{
			foreach (Thing thing in c.GetThingList(Map))
			{
				if (thing.TryGetComp<CompWaterStorage>() != null)
				{
					return thing;
				}
			}
			return null;
		}
	}
}
