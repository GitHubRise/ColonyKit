using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>保姆机器人模块用到的 Def 引用；由 DefOf 机制在 Def 加载后绑定。</summary>
	[DefOf]
	public static class NannyBotDefOf
	{
		public static ThingDef CK_RobotNanny;
		public static ThingDef CK_RobotMkV;
		public static PawnKindDef CK_RobotNannyKind;
		public static PawnKindDef CK_RobotMkVKind;
		public static ThingDef CK_ArcEmitter;
		public static ThingDef CK_ArcBlade;
		public static HediffDef CK_ArcBladeParalysis;
		public static ThingDef CK_NannyBotStation;
		public static JobDef CK_NannyCleanFilth;
		public static JobDef CK_NannyCharge;
		public static JobDef CK_RepairNannyBot;
		public static JobDef CK_RobotRestInBed;
		public static JobDef CK_NannyUnblock;
		public static NeedDef CK_RobotEnergy;
		public static StatDef CK_MkVInsulationCold;
		public static StatDef CK_MkVInsulationHeat;

		static NannyBotDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(NannyBotDefOf));
		}
	}
}
