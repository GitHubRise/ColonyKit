using LudeonTK;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>
	/// 蓝图配套的调试工具：深冷库需要把大量空调拉到最低目标温度，
	/// 逐台点 Gizmo 太慢，这里一键把当前地图上所有玩家空调设为 -273°C。
	/// </summary>
	public static class DebugActions_Blueprint
	{
		[DebugAction("蓝图工具", "全场空调设为 -273", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void SetAllCoolersMinTemperature()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			int count = 0;
			foreach (Building building in map.listerBuildings.allBuildingsColonist)
			{
				if (!(building is Building_Cooler))
				{
					continue;
				}

				CompTempControl control = building.GetComp<CompTempControl>();
				if (control == null)
				{
					continue;
				}

				control.TargetTemperature = -273f;
				count++;
			}

			Messages.Message(
				"已把 " + count + " 台空调的目标温度设为 -273°C",
				MessageTypeDefOf.TaskCompletion,
				false);
		}
	}
}
