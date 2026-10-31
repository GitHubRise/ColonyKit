using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_KickOver : CompProperties
	{
		public CompProperties_KickOver()
		{
			compClass = typeof(CompKickOver);
		}
	}

	/// <summary>
	/// 可倒掉的污水桶（CK_FecalSludge）使用的 Comp：
	/// 提供「倒掉」按钮，给物品刷/取消 <c>CK_KickOverDes</c> 标记，由搬运工把桶踢翻在地上。
	/// </summary>
	public class CompKickOver : ThingComp
	{
		private static DesignationDef DesDef =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_KickOverDes");

		public bool IsMarkedForDumping
		{
			get
			{
				Map map = parent.Map;
				DesignationDef def = DesDef;
				return map != null && def != null
					&& map.designationManager.DesignationOn(parent, def) != null;
			}
		}

		public void ToggleDesignation()
		{
			Map map = parent.Map;
			DesignationDef def = DesDef;
			if (map == null || def == null)
			{
				return;
			}
			Designation designation = map.designationManager.DesignationOn(parent, def);
			if (designation != null)
			{
				designation.Delete();
				return;
			}
			map.designationManager.AddDesignation(new Designation(parent, def));
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			if (!parent.Spawned || parent.Map == null)
			{
				yield break;
			}
			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.Assist.KickOver".Translate(),
				defaultDesc = "CK.Hygiene.Assist.KickOverDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("Things/Item/CK_FecalSludge"),
				action = ToggleDesignation
			};
		}
	}

	/// <summary>
	/// 选中殖民者后右键粪渣桶时的「倒掉」指令：直接派 <c>CK_KickOver</c> 工作，
	/// 不依赖搬运工作类型是否开启（和设计器的标记路径互为补充）。
	/// </summary>
	public class FloatMenuOptionProvider_KickOver : FloatMenuOptionProvider
	{
		protected override bool Drafted => false;

		protected override bool Undrafted => true;

		protected override bool Multiselect => false;

		protected override bool RequiresManipulation => true;

		public override IEnumerable<FloatMenuOption> GetOptionsFor(
			Thing clickedThing, FloatMenuContext context)
		{
			CompKickOver comp = clickedThing?.TryGetComp<CompKickOver>();
			Pawn pawn = context?.FirstSelectedPawn;
			if (comp == null || pawn == null || !pawn.IsColonistPlayerControlled)
			{
				yield break;
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_KickOver");
			bool reachable = def != null
				&& pawn.CanReserveAndReach(
					clickedThing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger());
			string label = "CK.Hygiene.Assist.KickOver".Translate().ToString();
			if (!reachable)
			{
				label += " " + "CannotReach".Translate().ToString();
			}
			yield return new FloatMenuOption(
				label,
				reachable
					? (Action)(() =>
					{
						Job job = JobMaker.MakeJob(def, clickedThing);
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					})
					: null,
				MenuOptionPriority.Default,
				null,
				clickedThing);
		}
	}
}
