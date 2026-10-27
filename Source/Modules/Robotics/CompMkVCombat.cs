using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	public class CompProperties_MkVCombat : CompProperties
	{
		public float engageRange = 28f;
		public float returnRange = 2f;

		public CompProperties_MkVCombat()
		{
			compClass = typeof(CompMkVCombat);
		}
	}

	public enum MkVCombatState
	{
		Idle,
		Engaged,
		Returning
	}

	/// <summary>Mk-V 战斗状态：只跟踪目标、交战/回舱状态，供 ThinkTree、状态页和检查面板使用。</summary>
	public class CompMkVCombat : ThingComp
	{
		public MkVCombatState state = MkVCombatState.Idle;
		public Pawn target;
		public Building homeBay;
		public int lastTargetTick;

		private CompProperties_MkVCombat Props => (CompProperties_MkVCombat)props;

		private Pawn Pawn => parent as Pawn;

		public Building HomeBay
		{
			get
			{
				if (homeBay != null && homeBay.Spawned && homeBay.Map == Pawn?.Map)
				{
					return homeBay;
				}
				CompNannyBot bot = Pawn?.TryGetComp<CompNannyBot>();
				return bot?.homeStation;
			}
		}

		public bool IsEngaged => state == MkVCombatState.Engaged;

		public float EngageRange => Props.engageRange;

		public float ReturnRange => Props.returnRange;

		public void SetEngaged(Pawn newTarget)
		{
			target = newTarget;
			state = MkVCombatState.Engaged;
			lastTargetTick = Find.TickManager.TicksGame;
		}

		public void SetReturning()
		{
			target = null;
			state = MkVCombatState.Returning;
			lastTargetTick = Find.TickManager.TicksGame;
		}

		public void Clear()
		{
			target = null;
			state = MkVCombatState.Idle;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref state, "ck_mkvCombatState", MkVCombatState.Idle);
			Scribe_References.Look(ref target, "ck_mkvCombatTarget");
			Scribe_References.Look(ref homeBay, "ck_mkvCombatHomeBay");
			Scribe_Values.Look(ref lastTargetTick, "ck_mkvCombatLastTarget", 0);
		}

		public override string CompInspectStringExtra()
		{
			if (Pawn == null)
			{
				return null;
			}
			string weapon = Pawn.equipment?.Primary != null
				? Pawn.equipment.Primary.LabelCap
				: "CK.Robotics.MkVCombat.NoWeapon".Translate();
			string status = state == MkVCombatState.Engaged
				? "CK.Robotics.MkVCombat.Status.Engaged".Translate()
				: state == MkVCombatState.Returning
					? "CK.Robotics.MkVCombat.Status.Returning".Translate()
					: "CK.Robotics.MkVCombat.Status.Idle".Translate();
			string line = "CK.Robotics.MkVCombat.Inspect".Translate(weapon, status);
			if (target != null && !target.Dead)
			{
				line += "\n" + "CK.Robotics.MkVCombat.Target".Translate(target.LabelShort);
			}
			return line;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Pawn == null || state == MkVCombatState.Idle)
			{
				yield break;
			}
			yield return new Command_Action
			{
				defaultLabel = "CK.Robotics.MkVCombat.Disengage".Translate(),
				defaultDesc = "CK.Robotics.MkVCombat.Disengage.Desc".Translate(),
				icon = TexCommand.CannotShoot,
				action = () =>
				{
					SetReturning();
					Pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
				}
			};
		}
	}
}
