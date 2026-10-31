using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「铲除污水」设计器：在污染值 &gt; 0.5 的格子上刷标记，
	/// 工人每格铲 120 tick，清掉 5 点污染并产出 1 个粪渣（CK_FecalSludge）。
	/// </summary>
	public class Designator_RemoveSewage : Designator_Cells
	{
		private const float MinPollution = 0.5f;

		public Designator_RemoveSewage()
		{
			defaultLabel = "CK.Hygiene.Assist.RemoveSewage".Translate();
			defaultDesc = "CK.Hygiene.Assist.RemoveSewageDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("Things/Item/CK_FecalSludge");
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
			soundDragChanged = null;
			soundSucceeded = SoundDefOf.Designate_PlanAdd;
		}

		public override DrawStyleCategoryDef DrawStyleCategory =>
			DrawStyleCategoryDefOf.FilledRectangle;

		public override bool DragDrawMeasurements => true;

		private static DesignationDef Def =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_RemoveSewageDes");

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(Map) || c.Fogged(Map))
			{
				return false;
			}
			DesignationDef def = Def;
			if (def == null || Map.designationManager.DesignationAt(c, def) != null)
			{
				return false;
			}
			MapComponent_SewagePollution pollution =
				Map.GetComponent<MapComponent_SewagePollution>();
			if (pollution == null || pollution.PollutionAt(c) <= MinPollution)
			{
				return false;
			}
			Building edifice = c.GetEdifice(Map);
			if (edifice != null
				&& edifice.def.Fillage == FillCategory.Full
				&& edifice.def.passability == Traversability.Impassable)
			{
				return false;
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
