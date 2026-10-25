using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class RoomRoleWorker_PrivateBathroom : RoomRoleWorker
	{
		public override float GetScore(Room room)
		{
			int assigned = 0;
			List<Thing> things = room.ContainedAndAdjacentThings;
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Building_AssignableFixture fixture && fixture.AssignedPawns.Any())
				{
					assigned++;
				}
			}
			return assigned > 0 && !room.IsPrisonCell ? 4000f : 0f;
		}
	}

	public class RoomRoleWorker_PublicBathroom : RoomRoleWorker
	{
		public override float GetScore(Room room)
		{
			int toilets = 0;
			int baths = 0;
			List<Thing> things = room.ContainedAndAdjacentThings;
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Building_AssignableFixture fixture)
				{
					if (fixture.fixture == FixtureType.Toilet) toilets++;
					if (fixture.fixture == FixtureType.Bath) baths++;
				}
			}
			return baths > 0 || toilets > 1 ? 30f * baths + toilets : 0f;
		}
	}
}
