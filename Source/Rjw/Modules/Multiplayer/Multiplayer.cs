#nullable disable

using Verse;
using RimWorld;
using Multiplayer.API;

namespace rjw
{
	[StaticConstructorOnStartup]
	public static class RJW_Multiplayer
	{
		static RJW_Multiplayer()
		{
			if (!MP.enabled) return;

			// 神奇之处就在这里，你的特性
			// 会自动注册，类似 Harmony 的 PatchAll。
			MP.RegisterAll();

				/*
				ModLog.Message("RJW MP compat testing");
				var type = AccessTools.TypeByName("rjw.RJWdesignations");
				//ModLog.Message("rjw MP compat " + type.Name);
				ModLog.Message("is host " + MP.IsHosting);
				ModLog.Message("PlayerName " + MP.PlayerName);
				ModLog.Message("IsInMultiplayer " + MP.IsInMultiplayer);

				//MP.RegisterSyncMethod(type, "Comfort");
				/*
				MP.RegisterSyncMethod(type, "<GetGizmos>Service");
				MP.RegisterSyncMethod(type, "<GetGizmos>BreedingHuman");
				MP.RegisterSyncMethod(type, "<GetGizmos>BreedingAnimal");
				MP.RegisterSyncMethod(type, "<GetGizmos>Breeder");
				MP.RegisterSyncMethod(type, "<GetGizmos>Milking");
				MP.RegisterSyncMethod(type, "<GetGizmos>Hero");
				*/


			// 你可以选择不自动注册，而是手动完成
			// 使用 MP.Register* 系列方法。

			// 在其他地方使用 MP.IsInMultiplayer 来据此处理
			// 用户可能启用了它但并未处于多人会话中
		}

		//为 Verse.Rand 生成 PredictableSeed
		public static int PredictableSeed()
		{
			int seed = 0;
			try
			{
				Map map = Find.CurrentMap;
				//int seedHourOfDay = GenLocalDate.HourOfDay(map);
				//int seedDayOfYear = GenLocalDate.DayOfYear(map);
				//int seedYear = GenLocalDate.Year(map);
				seed = (GenLocalDate.HourOfDay(map) + GenLocalDate.DayOfYear(map)) * GenLocalDate.Year(map);
				//int seed = (seedHourOfDay + seedDayOfYear) * seedYear;
				//ModLog.Warning("seedHourOfDay: " + seedHourOfDay + "\nseedDayOfYear: " + seedDayOfYear + "\nseedYear: " + seedYear + "\n" + seed);
			}
			catch
			{
				seed = Rand.Int;
			}
			return seed;
		}

		//为 Verse.Rand 生成 PredictableSeed
		[SyncMethod]
		public static float RJW_MP_RAND()
		{
			return Rand.Value;
		}
	}
}
