using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>桑拿房判定（DBH RoomRoleWorker_Sauna 的移植）：封闭、不露天，且塞了桑拿加热器的房间。</summary>
	public class RoomRoleWorker_Sauna : RoomRoleWorker
	{
		public override float GetScore(Room room)
		{
			if (room.UsesOutdoorTemperature || room.TouchesMapEdge)
			{
				return 0f;
			}
			int heaters = 0;
			foreach (Region region in room.Regions)
			{
				foreach (ThingDef def in SaunaDefs())
				{
					heaters += region.ListerThings.ThingsOfDef(def).Count;
				}
			}
			if (heaters > 0 && room.CellCount / heaters < 50)
			{
				return 9999f;
			}
			return 0f;
		}

		private static ThingDef[] SaunaDefs()
		{
			return new[]
			{
				DefDatabase<ThingDef>.GetNamedSilentFail("CK_SaunaHeaterElec"),
				DefDatabase<ThingDef>.GetNamedSilentFail("CK_SaunaHeaterLog")
			};
		}
	}
}
