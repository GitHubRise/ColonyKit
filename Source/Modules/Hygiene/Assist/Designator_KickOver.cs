using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「倒掉污水」设计器：框选/点选粪渣桶（带 <see cref="CompKickOver"/> 的物品），
	/// 刷上 <c>CK_KickOverDes</c> 标记后由工人把桶踢翻在地上。
	/// </summary>
	public class Designator_KickOver : Designator
	{
		public Designator_KickOver()
		{
			defaultLabel = "CK.Hygiene.Assist.KickOver".Translate();
			defaultDesc = "CK.Hygiene.Assist.KickOverDesc".Translate();
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
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_KickOverDes");

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(Map))
			{
				return false;
			}
			return DumpableInCell(c) != null;
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			Thing dumpable = DumpableInCell(c);
			if (dumpable != null)
			{
				DesignateThing(dumpable);
			}
		}

		public override AcceptanceReport CanDesignateThing(Thing t)
		{
			if (t?.TryGetComp<CompKickOver>() == null)
			{
				return false;
			}
			DesignationDef def = Def;
			if (def == null || Map.designationManager.DesignationOn(t, def) != null)
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

		private Thing DumpableInCell(IntVec3 c)
		{
			foreach (Thing thing in c.GetThingList(Map))
			{
				if (thing.TryGetComp<CompKickOver>() != null)
				{
					return thing;
				}
			}
			return null;
		}
	}
}
