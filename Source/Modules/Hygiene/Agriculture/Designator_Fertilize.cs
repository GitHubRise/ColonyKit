using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>
	/// 「施肥」设计器：在耕地上刷施肥标记，工人每格消耗 10 个有机肥把它施掉。
	/// 用原版 Designation 存标记（随存档自动保存），取消标记用原版「取消」设计器即可。
	/// </summary>
	public class Designator_Fertilize : Designator_Cells
	{
		public Designator_Fertilize()
		{
			defaultLabel = "CK.Agriculture.Fertilize".Translate();
			defaultDesc = "CK.Agriculture.FertilizeDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/CK_Fertilizer");
			soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
			soundDragChanged = null;
			soundSucceeded = SoundDefOf.Designate_PlanAdd;
			useMouseIcon = true;
		}

		public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

		public override bool DragDrawMeasurements => true;

		public override bool Visible
		{
			get
			{
				ResearchProjectDef research =
					DefDatabase<ResearchProjectDef>.GetNamedSilentFail("CK_SewageSludgeComposting");
				return research == null || research.IsFinished;
			}
		}

		private static DesignationDef Def =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_Fertilize");

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(Map) || c.Fogged(Map))
			{
				return false;
			}
			if (c.GetEdifice(Map) != null || c.GetTerrain(Map).fertility <= 0f)
			{
				return false;
			}
			if (Def == null || Map.designationManager.DesignationAt(c, Def) != null)
			{
				return false;
			}
			MapComponent_Agriculture comp = MapComponent_Agriculture.Get(Map);
			if (comp != null && comp.FertilizerAt(c) > 0f)
			{
				return "CK.Agriculture.AlreadyFertilized".Translate();
			}
			return true;
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			DesignationDef def = Def;
			if (def != null)
			{
				Map.designationManager.AddDesignation(new Designation(c, def));
			}
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}
	}
}
