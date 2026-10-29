using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.WorkPriority
{
	/// <summary>
	/// 新小人的默认工作优先级：灭火 1、就医 2、其余 5。
	/// </summary>
	/// <remarks>
	/// 两条初始化路径之后各补一次：<c>Pawn_WorkSettings.EnableAndInitialize</c>
	/// （新出生、新加入）与 <c>GameInitData.PrepForMapGen</c>（新开局的起始殖民者，
	/// 原版在那里会绕过 EnableAndInitialize 直接改优先级）。
	/// 已有存档里玩家调过的数字不会被改；不能从事的工作（特质/背景禁用）保持 0。
	/// </remarks>
	public static class WorkPriorityDefaults
	{
		public static void Apply(Pawn pawn, Pawn_WorkSettings settings)
		{
			if (pawn == null || settings == null || !settings.EverWork)
			{
				return;
			}

			// 机械族有自己的一套工作配置（RaceProps.mechWorkTypePriorities），不要覆盖。
			if (!pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
			{
				return;
			}

			foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
			{
				if (pawn.WorkTypeIsDisabled(workType))
				{
					continue;
				}

				settings.SetPriority(workType, PriorityFor(workType));
			}
		}

		public static int PriorityFor(WorkTypeDef workType)
		{
			if (workType == null)
			{
				return WorkPriorityModule.DefaultPriorityValue;
			}

			switch (workType.defName)
			{
				case WorkPriorityModule.FirefighterDefName:
					return WorkPriorityModule.FirefighterPriority;
				case WorkPriorityModule.PatientDefName:
					return WorkPriorityModule.PatientPriority;
				default:
					return WorkPriorityModule.DefaultPriorityValue;
			}
		}

		/// <summary>
		/// 原版教程「手动分配工作优先级」的说明写着「标记『4』为最后去做」，
		/// 上限提到 9 后把这句话改掉（简中/繁中各一条，其他语言原样保留）。
		/// </summary>
		public static void FixConceptHelpText()
		{
			ConceptDef concept = DefDatabase<ConceptDef>.GetNamedSilentFail("ManualWorkPriorities");
			if (concept == null)
			{
				return;
			}

			// 1.6 把 helpText 改成了 private，按字段名反射读写，改名时静默跳过。
			Traverse field = Traverse.Create(concept).Field("helpText");
			if (!field.FieldExists())
			{
				return;
			}

			string text = field.GetValue<string>();
			if (text.NullOrEmpty())
			{
				return;
			}

			string replaced = text
				.Replace("标记「4」为最后去做", "标记「9」为最后去做")
				.Replace("標記「4」為最後去做", "標記「9」為最後去做");
			if (replaced != text)
			{
				field.SetValue(replaced);
			}
		}
	}
}
