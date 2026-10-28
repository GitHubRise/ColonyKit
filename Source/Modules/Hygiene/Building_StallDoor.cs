using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_StallDoor 的移植：过道隔间门。
	/// 只在小人经过时开合，并且**只挡隐私视线**（PrivacyUtil 的 LOS 检查把门所在格当遮挡），
	/// 不分割房间、不挡热量、也不影响战斗视线。
	/// </summary>
	public class Building_StallDoor : Building
	{
		private bool openInt;

		public int visualTicksOpen;

		public int ticksUntilClose;

		public int TicksToOpenNow
		{
			get
			{
				float f = 45f / this.GetStatValue(StatDefOf.DoorOpenSpeed);
				return Mathf.RoundToInt(f);
			}
		}

		private int VisualTicksToOpen => TicksToOpenNow;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref openInt, "open", false);
			if (Scribe.mode == LoadSaveMode.LoadingVars && openInt)
			{
				visualTicksOpen = VisualTicksToOpen;
			}
		}

		public void DoorOpen(int ticksToClose = 60)
		{
			ticksUntilClose = ticksToClose;
			if (!openInt)
			{
				openInt = true;
				def.building?.soundDoorOpenManual?.PlayOneShot(new TargetInfo(Position, Map));
			}
		}

		public void Notify_PawnApproaching(Pawn p)
		{
			DoorOpen(10);
		}

		protected override void Tick()
		{
			base.Tick();
			if (!openInt)
			{
				if (visualTicksOpen > 0)
				{
					visualTicksOpen--;
				}
				return;
			}
			if (visualTicksOpen < VisualTicksToOpen)
			{
				visualTicksOpen++;
			}
			if (Map.thingGrid.CellContains(Position, ThingCategory.Pawn))
			{
				ticksUntilClose = 10;
				return;
			}
			ticksUntilClose--;
			if (ticksUntilClose <= 0)
			{
				openInt = false;
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			Rotation = DoorUtility.DoorRotationAt(Position, Map, preferFences: false);
			float open = Mathf.Clamp01((float)visualTicksOpen / VisualTicksToOpen);
			float offset = 0.45f * open;
			for (int i = 0; i < 2; i++)
			{
				Vector3 dir = i == 0 ? new Vector3(0f, 0f, -1f) : new Vector3(0f, 0f, 1f);
				Mesh mesh = i == 0 ? MeshPool.plane10 : MeshPool.plane10Flip;
				Rot4 rotation = Rotation;
				rotation.Rotate(RotationDirection.Clockwise);
				dir = rotation.AsQuat * dir;
				Vector3 pos = DrawPos;
				pos.y = AltitudeLayer.DoorMoveable.AltitudeFor();
				pos += dir * offset;
				Graphics.DrawMesh(mesh, pos, Rotation.AsQuat, Graphic.MatAt(Rotation), 0);
			}
			Comps_PostDraw();
		}
	}

	/// <summary>小人走到隔间门门口时把门推开（DBH 同款补丁）。</summary>
	[HarmonyPatch(typeof(Pawn_PathFollower), "SetupMoveIntoNextCell")]
	internal static class Patch_Pawn_PathFollower_StallDoor
	{
		// pawn / nextCell 在原版里不是 public，用 FieldRef 取。
		private static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> PawnField =
			AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");

		private static readonly AccessTools.FieldRef<Pawn_PathFollower, IntVec3> NextCellField =
			AccessTools.FieldRefAccess<Pawn_PathFollower, IntVec3>("nextCell");

		private static void Postfix(Pawn_PathFollower __instance)
		{
			Pawn pawn = PawnField(__instance);
			if (pawn?.Map == null)
			{
				return;
			}
			ThingDef doorDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_ToiletStallDoor");
			if (doorDef == null)
			{
				return;
			}
			if (pawn.Map.thingGrid.ThingAt(NextCellField(__instance), doorDef) is Building_StallDoor door)
			{
				door.Notify_PawnApproaching(pawn);
			}
		}
	}
}
