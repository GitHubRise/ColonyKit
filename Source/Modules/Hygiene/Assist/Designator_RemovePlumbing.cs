using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「拆除管道」设计器：点选/框选带 <see cref="CompPipe"/> 的管网建筑，
	/// 直接打上原版拆除标记（DBH Designator_RemovePlumbing 的简化版，不再先选管道类型）。
	/// </summary>
	public class Designator_RemovePlumbing : Designator
	{
		public Designator_RemovePlumbing()
		{
			defaultLabel = "CK.Hygiene.Assist.RemovePlumbing".Translate();
			defaultDesc = "CK.Hygiene.Assist.RemovePlumbingDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Commands/Deconstruct");
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
			soundDragChanged = null;
			soundSucceeded = SoundDefOf.Designate_PlanAdd;
		}

		public override DrawStyleCategoryDef DrawStyleCategory =>
			DrawStyleCategoryDefOf.FilledRectangle;

		public override bool DragDrawMeasurements => true;

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(Map) || (!DebugSettings.godMode && c.Fogged(Map)))
			{
				return false;
			}
			return PlumbingInCell(c) != null;
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			Thing plumbing = PlumbingInCell(c);
			if (plumbing != null)
			{
				DesignateThing(plumbing);
			}
		}

		public override AcceptanceReport CanDesignateThing(Thing t)
		{
			if (!(t is Building building) || building.TryGetComp<CompPipe>() == null)
			{
				return false;
			}
			if (!DebugSettings.godMode && building.Faction != Faction.OfPlayer)
			{
				if (building.Faction != null)
				{
					return false;
				}
				if (!building.ClaimableBy(Faction.OfPlayer))
				{
					return false;
				}
			}
			if (Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			if (Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null)
			{
				return false;
			}
			return true;
		}

		public override void DesignateThing(Thing t)
		{
			Map.designationManager.AddDesignation(new Designation(t, DesignationDefOf.Deconstruct));
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}

		private Thing PlumbingInCell(IntVec3 c)
		{
			foreach (Thing thing in c.GetThingList(Map))
			{
				if (thing.TryGetComp<CompPipe>() != null && CanDesignateThing(thing).Accepted)
				{
					return thing;
				}
			}
			return null;
		}
	}
}
