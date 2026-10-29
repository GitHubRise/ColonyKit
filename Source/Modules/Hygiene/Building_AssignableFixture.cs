using System.Collections.Generic;
using System.Linq;
using ColonyKit.Modules.Hygiene.Dbh;
using ColonyKit.Modules.Robotics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public enum FixtureType
	{
		Undefined,
		Basin,
		Bath,
		Toilet,
		Litter
	}

	public enum FixtureQuality
	{
		shabby,
		normal,
		fine,
		luxury
	}

	/// <summary>DBH Building_AssignableFixture 的淋浴子集：水/电/可用性检查 + 性别/访客 Gizmo。</summary>
	public class Building_AssignableFixture : Building
	{
		public CompPipe pipe;
		public CompPowerTrader powerComp;
		public CompBlockage blockageComp;
		internal List<Building_Bed> BedRefs;
		public Gender gender = Gender.None;
		public bool AllowColonists = true;
		public bool AllowSlaves = true;
		public bool AllowGuests = true;
		public bool AnimalsOnly;
		private bool occupancyCheck;

		/// <summary>正在使用洁具的起始 tick；-1 表示空闲。用于占用/排队 Alert。</summary>
		public int occupiedSinceTick = -1;

		public virtual float waterUsed => 0f;

		/// <summary>洁具必须接在同网有水的水管上；没有水网或自身阀门关闭时不可用。</summary>
		public virtual bool HasWaterSupply => pipe != null && !pipe.closed && pipe.pipeNet != null;

		/// <summary>Does this fixture refuse to work at all without power? Simple showers can still run cold.</summary>
		public virtual bool PowerRequiredToUse => true;

		public virtual FixtureType fixture => FixtureType.Undefined;

		public virtual FixtureQuality Quality => FixtureQuality.normal;

		public virtual bool NeedsSewage => false;

		public virtual bool CanSetGender => true;

		public bool OccupancyCheck => occupancyCheck && !AnimalsOnly && !ForPrisoners;

		public bool ForPrisoners => Position.IsInPrisonCell(Map);

		public string GenderText
		{
			get
			{
				switch (gender)
				{
					case Gender.Male:
						return "CK.Hygiene.Gender.Male".Translate();
					case Gender.Female:
						return "CK.Hygiene.Gender.Female".Translate();
					default:
						return "CK.Hygiene.Gender.Unisex".Translate();
				}
			}
		}

		public bool Occupied(Pawn checker)
		{
			if (!OccupancyCheck || !Spawned)
			{
				return false;
			}
			Room room = Position.GetRoom(Map);
			return room != null && room.ContainedAndAdjacentThings.Any(
				t => t is Pawn pawn && pawn != checker);
		}

		public virtual bool PawnAllowed(Pawn pawn)
		{
			if (pawn == null || gender != Gender.None && pawn.gender != gender)
			{
				return false;
			}
			if (pawn.IsPrisonerOfColony)
			{
				return ForPrisoners;
			}
			if (pawn.IsSlave)
			{
				return AllowSlaves;
			}
			if (pawn.IsColonist || NannyBotUtility.IsNannyBot(pawn))
			{
				// 机械保姆不是 Humanlike，原版 IsColonist 认不出来；按殖民者处理。
				return AllowColonists;
			}
			if (pawn.IsQuestLodger() || pawn.IsWildMan())
			{
				return AllowGuests;
			}
			return AllowGuests;
		}

		public List<Building_Bed> AllSpawnedBeds()
		{
			if (BedRefs == null)
			{
				BedRefs = new List<Building_Bed>();
				return BedRefs;
			}
			BedRefs.RemoveAll(bed => bed == null || !bed.Spawned);
			return BedRefs;
		}

		public List<Pawn> AssignedPawns
		{
			get
			{
				var pawns = new List<Pawn>();
				foreach (Building_Bed bed in AllSpawnedBeds())
				{
					pawns.AddRange(bed.CompAssignableToPawn.AssignedPawnsForReading);
				}
				return pawns;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref gender, "gender", Gender.None);
			Scribe_Values.Look(ref AllowColonists, "AllowColonists", true);
			Scribe_Values.Look(ref AllowSlaves, "AllowSlaves", true);
			Scribe_Values.Look(ref AllowGuests, "AllowGuests", true);
			Scribe_Values.Look(ref AnimalsOnly, "AnimalsOnly", false);
			Scribe_Values.Look(ref occupancyCheck, "OccupancyCheckInt", false);
			Scribe_Values.Look(ref occupiedSinceTick, "occupiedSinceTick", -1);
			Scribe_Collections.Look(ref BedRefs, "bedRefs", LookMode.Reference);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			pipe = GetComp<CompPipe>();
			powerComp = GetComp<CompPowerTrader>();
			blockageComp = GetComp<CompBlockage>();
		}

		public override void TickRare()
		{
			base.TickRare();
			blockageComp?.CheckForBlockage();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}

			if (CanSetGender)
			{
				yield return new Command_Action
				{
					defaultLabel = "CK.Hygiene.Gender".Translate(GenderText),
					defaultDesc = "CK.Hygiene.GenderDesc".Translate(),
					action = () => gender = gender == Gender.None ? Gender.Male
						: gender == Gender.Male ? Gender.Female : Gender.None
				};
			}

			yield return new Command_Toggle
			{
				defaultLabel = "CK.Hygiene.OccupancyRestriction".Translate(),
				defaultDesc = "CK.Hygiene.OccupancyRestrictionDesc".Translate(),
				isActive = () => occupancyCheck,
				toggleAction = () => occupancyCheck = !occupancyCheck
			};

			yield return new Command_Toggle
			{
				defaultLabel = "CK.Hygiene.AllowColonists".Translate(),
				defaultDesc = "CK.Hygiene.AllowColonistsDesc".Translate(),
				isActive = () => AllowColonists,
				toggleAction = () => AllowColonists = !AllowColonists
			};

			if (Faction == Faction.OfPlayer && !ForPrisoners)
			{
				yield return new Command_Action
				{
					defaultLabel = "CK.Hygiene.ConnectBed".Translate(),
					defaultDesc = "CK.Hygiene.ConnectBedDesc".Translate(),
					action = () => Find.Targeter.BeginTargeting(ForBed(),
						x => SetBed(x, Input.GetKey(KeyCode.LeftShift)))
				};
			}
		}

		public static TargetingParameters ForBed()
		{
			return new TargetingParameters
			{
				canTargetPawns = false,
				canTargetBuildings = true,
				validator = t => t.HasThing && t.Thing is Building_Bed
			};
		}

		public void SetBed(LocalTargetInfo target, bool shift)
		{
			if (!shift)
			{
				AllSpawnedBeds().Clear();
			}
			if (target.Thing is Building_Bed bed && !AllSpawnedBeds().Contains(bed))
			{
				AllSpawnedBeds().Add(bed);
			}
		}

		public virtual AcceptanceReport Working(float WaterUsed = 0f)
		{
			if (WaterUsed == 0f)
			{
				WaterUsed = waterUsed;
			}
			if (blockageComp != null && blockageComp.blocked)
			{
				return "CK.Hygiene.Blocked".Translate();
			}
			if (PowerRequiredToUse && powerComp != null && !powerComp.PowerOn)
			{
				return "NoPower".Translate();
			}
			if (pipe == null)
			{
				return true;
			}
			PlumbingNet net = pipe.pipeNet;
			// 不要用 DBH 的 "Nowater"：本模组和原版都没有这个键，
			// 开发者模式下会被伪本地化成 “Nòẅàṭèṟ” 这类乱码。
			if (net == null || net.WaterTowers.Count == 0)
			{
				return "CK.Hygiene.NoWaterSupply".Translate();
			}
			if (WaterUsed != 0f && net.WaterStorage <= WaterUsed)
			{
				return "CK.Hygiene.NotEnoughWater".Translate();
			}
			return true;
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			// base 可能为空（马桶等 comp 不产出文案），此时再拼 "\n" 会产生空行，
			// 检查面板会报 "Inspect string for ... contains empty lines"。
			AcceptanceReport report = Working();
			if (!report.Accepted)
			{
				if (!text.NullOrEmpty())
				{
					text += "\n";
				}
				text += report.Reason;
			}
			return text;
		}
	}
}
