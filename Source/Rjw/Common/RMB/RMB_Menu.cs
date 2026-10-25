#nullable enable

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Multiplayer.API;
using rjw.Modules.Interactions;

namespace rjw.RMB
{
	/// <summary>
	/// RJW 右键菜单选项的入口点。
	/// RimWorld 使用反射按父类型查找此类。
	/// </summary>
	public class RMB_Menu : FloatMenuOptionProvider
	{
		protected override bool Drafted => false;
		protected override bool Undrafted => true;
		protected override bool Multiselect => false;

		public override IEnumerable<FloatMenuOption> GetOptions(FloatMenuContext context)
		{
			if (!RJWSettings.RMB_enabled)
			{
				yield break;
			}

			Pawn pawn = context.FirstSelectedPawn;
			AcceptanceReport canControlPawn = CanControlPawn(pawn);
			if (!canControlPawn)
			{
				if (RJWSettings.DevMode)
					yield return new FloatMenuOption($"No interactions: {canControlPawn.Reason}", null);

				yield break;
			}

			foreach (Thing target in context.ClickedThings)
			{
				if (target is Pawn targetPawn)
				{
					if (!targetPawn.Spawned)
					{
						if (RJWSettings.DevMode)
							yield return new FloatMenuOption($"No interactions for {targetPawn}: Pawn not spawned", null);
						continue;
					}
					if (targetPawn.Drafted)
					{
						if (RJWSettings.DevMode)
							yield return new FloatMenuOption($"No interactions for {targetPawn}: Pawn is drafted", null);
						continue;
					}
					if (targetPawn.IsHiddenFromPlayer())
					{
						if (RJWSettings.DevMode)
							yield return new FloatMenuOption($"No interactions for {targetPawn}: Pawn is hidden", null);
						continue;
					}
					if (!targetPawn.HasComp<CompRJW>())
					{
						if (RJWSettings.DevMode)
							yield return new FloatMenuOption($"No interactions for {targetPawn}: Doesn't have CompRJW", null);
						continue;
					}
				}

				if (!pawn.CanReach(target, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					//option = new FloatMenuOption("CannotReach".Translate(target.Thing.LabelCap, target.Thing) + " (" + "NoPath".Translate() + ")", null);
					continue;
				}

				foreach (FloatMenuOption option in GetOptionsForTarget(pawn, target))
				{
					yield return option;
				}
			}
		}

		/// <summary>
		/// 检查 <paramref name="pawn"/> 是否可由玩家控制
		/// </summary>
		/// <returns>
		/// AcceptanceReport。原因字符串未翻译，只应在开发者模式中显示
		/// </returns>
		public static AcceptanceReport CanControlPawn(Pawn pawn)
		{
			// 如果该小人无法接受工作，就不必处理。
			if (pawn.jobs == null)
				return "Cannot take jobs";

			// 如果小人已被征召 - 退出。
			if (pawn.Drafted)
				return "Drafted";

			// 正被强暴 - 无法控制
			if (pawn.jobs.curDriver is JobDriver_SexBaseRecieverRaped)
				return "Getting raped";

			//是殖民者？是 Hospitality 的殖民者/访客？访客无法控制
			if (!pawn.IsFreeColonist || pawn.Faction == null || pawn.GetExtraHomeFaction(null) != null)
				return "Not a colonist";

			//非英雄模式或未 override_control - 退出
			if (!(RJWSettings.RPG_hero_control || RJWSettings.override_control))
				return "Enable hero mode or direct control";

			if (!pawn.HasComp<CompRJW>())
				return "Doesn't have CompRJW";

			bool isHero = false;    //是英雄
			bool otherPlayerHero = false;    //不是自己拥有的英雄？以后也许还会检查囚犯等

			if (RJWSettings.RPG_hero_control)
			{
				isHero = pawn.IsDesignatedHero();
				otherPlayerHero = isHero && !pawn.IsHeroOwner();
			}
			else if (!RJWSettings.override_control)
				return "Not a hero, direct control disabled";

			//不是英雄，也未 override_control - 退出
			if (!isHero && !RJWSettings.override_control)
				return "Not a hero";

			//不是自己拥有的英雄 - 退出
			if (isHero && otherPlayerHero)
				return "Not owned hero";

			if (pawn.IsPrisoner) // 移除了奴隶限制 - 既然可以征召奴隶，为什么不直接控制他们？
				return "Prisoner";

			return true;
		}

		/// <summary>
		/// 创建 RJW 浮动菜单选项
		/// </summary>
		/// <param name="pawn">选中的小人</param>
		/// <param name="target">玩家点击的小人或物品</param>
		public static IEnumerable<FloatMenuOption> GetOptionsForTarget(Pawn pawn, Thing target)
		{
			if (pawn == target)
			{
				foreach (FloatMenuOption opt in RMB_Masturbate.GetOptions(pawn))
					yield return opt;
			}
			else if (target is Pawn targetPawn)
			{
				foreach (FloatMenuOption opt in RMB_Mechanitor.GetOptions(pawn, targetPawn))
					yield return opt;

				if (xxx.is_human(targetPawn))
				{
					foreach (FloatMenuOption opt in RMB_Invite.GetOptions(pawn, targetPawn))
						yield return opt;

					foreach (FloatMenuOption opt in RMB_Sex.GetOptions(pawn, targetPawn))
						yield return opt;
				}

				if (xxx.is_animal(targetPawn))
				{
					foreach (FloatMenuOption opt in RMB_Bestiality.GetOptions(pawn, targetPawn))
						yield return opt;;

					foreach (FloatMenuOption opt in RMB_RapeAnimal.GetOptions(pawn, targetPawn))
						yield return opt;
				}
				else
				{
					foreach (FloatMenuOption opt in RMB_Rape.GetOptions(pawn, targetPawn))
						yield return opt;
				}

				foreach (FloatMenuOption opt in RMB_Socialize.GetOptions(pawn, targetPawn))
					yield return opt;
			}
			else if (target is Corpse corpse)
			{
				foreach (FloatMenuOption opt in RMB_Necro.GetOptions(pawn, corpse))
					yield return opt;
			}
		}

		/// <summary>
		/// 为两个小人生成子菜单选项
		/// </summary>
		/// <param name="pawn">发起者小人</param>
		/// <param name="target">接受者小人或尸体</param>
		/// <param name="job"></param>
		/// <param name="rape"></param>
		/// <param name="reverse"></param>
		/// <returns>菜单选项列表，其中每一项代表一种可能的互动</returns>
		public static IEnumerable<FloatMenuOption> GenerateNonSoloSexRoleOptions(Pawn pawn, LocalTargetInfo target, JobDef job, bool rape, bool reverse = false)
		{
			bool foundInteraction = false;

			Pawn partner = target.Pawn;

			if (target.Thing is Corpse corpse)
			{
				partner = corpse.InnerPawn;
			}

			SexInteractionTag mainTag = SexInteractionTag.Consensual;
			if (xxx.is_animal(partner))
			{
				mainTag = SexInteractionTag.Bestiality;
			}
			else if (rape)
			{
				mainTag = SexInteractionTag.Rape;
			}

			foreach (InteractionDef d in SexUtility.SexInteractions)
			{
				var interaction = new SexInteraction(d);
				if (interaction.Sextype == xxx.rjwSextype.None)
					continue;
				if (interaction.Sextype == xxx.rjwSextype.Masturbation)
					continue;
				if (!interaction.HasInteractionTag(mainTag))
					continue;
				if (reverse != interaction.HasInteractionTag(SexInteractionTag.Reverse))
					continue;

				var props = new SexProps(pawn, partner) { interaction = interaction };
				var resolved = SexInteractionHelper.ResolveInteraction(props);
				if (resolved == null)
					continue;

				string label = interaction.Extension.RMBLabel.CapitalizeFirst();
				if (RJWSettings.DevMode)
					label += $" ( defName: {d.defName} )";

				FloatMenuOption option = new(label, delegate () { HaveSex(pawn, job, target, d, resolved); }, MenuOptionPriority.High);
				yield return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, target);
			}

			if (RJWSettings.DevMode && !foundInteraction)
			{
				yield return new FloatMenuOption("No interactions found", null);
			}
		}

		//多人游戏同步操作

		/// <summary>
		/// 停止 <paramref name="pawn"/> 当前的工作并开始新的性行为工作
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="jobDef"></param>
		/// <param name="target">接受者小人或尸体</param>
		/// <param name="interactionDef">性行为互动定义</param>
		/// <param name="resolved">预先解析好的互动数据；为 null 时从 <paramref name="interactionDef"/> 解析。</param>
		[SyncMethod]
		public static void HaveSex(Pawn pawn, JobDef jobDef, LocalTargetInfo target, InteractionDef interactionDef, SexInteractionResolved? resolved = null)
		{
			Pawn partner;
			if (target.Thing is Corpse corpse)
			{
				partner = corpse.InnerPawn;
			}
			else if (target.Pawn == null || pawn == target.Pawn) // 自慰
			{
				partner = pawn;
			}
			else
			{
				partner = target.Pawn;
			}

			SexInteraction interaction = new SexInteraction(interactionDef);

			bool rape = interaction.HasInteractionTag(SexInteractionTag.Rape);

			Job job;
			if (jobDef == xxx.casual_sex)
			{
				job = new Job(jobDef, target, partner.CurrentBed());
			}
			else if (jobDef == xxx.bestialityForFemale)
			{
				if (target.Pawn?.ownership.OwnedBed == null) //检查伴侣是否有床
					job = new Job(jobDef, target, pawn.ownership.OwnedBed); //如果伴侣没有分配床，就去小人自己的床
				else job = new Job(jobDef, target, target.Pawn.ownership.OwnedBed); //如果伴侣有床 - 就去伴侣那里
			}
			else if (jobDef == xxx.Masturbate)
			{
				job = new Job(jobDef, pawn, null, target.Cell);
			}
			else
			{
				job = new Job(jobDef, target);
			}

			var SP = new SexProps(pawn, partner)
			{
				isRape = rape,
				isRapist = rape,
				canBeGuilty = false, //pawn.IsHeroOwner();//TODO: fix for MP someday
				interaction = interaction
			};

			if (resolved != null)
			{
				SP.resolved = resolved;
			}

			pawn.GetRJWPawnData()!.SexProps = SP;
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			pawn.jobs.TryTakeOrderedJob(job);
		}
	}
}
