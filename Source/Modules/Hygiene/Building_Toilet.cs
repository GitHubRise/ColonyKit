using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene
{
	public class Building_Toilet : Building_AssignableFixture
	{
		public bool seatUp;
		private Graphic seatUpGraphic;

		public override float waterUsed => 14f * HygieneSettings.Current.waterUseMultiplier;

		public override FixtureType fixture => FixtureType.Toilet;

		public override FixtureQuality Quality => FixtureQuality.normal;

		/// <summary>马桶圈放下的贴图路径；智能马桶会覆盖它换自己的美术。</summary>
		protected virtual string SeatUpTexPath => "Things/Building/Hygiene/CK_ToiletUp";

		public override Graphic Graphic
		{
			get
			{
				if (!seatUp)
				{
					return base.Graphic;
				}
				if (seatUpGraphic == null)
				{
					seatUpGraphic = GraphicDatabase.Get(
						typeof(Graphic_Multi),
						SeatUpTexPath,
						ShaderDatabase.CutoutComplex,
						def.graphicData.drawSize,
						Color.white,
						Color.white);
				}
				return seatUpGraphic;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref seatUp, "seatUp", false);
		}

		public virtual bool TryUseFlush()
		{
			CompPipe waterPipe = pipe;
			CompPipe sewagePipe = null;
			foreach (CompPipe candidate in GetComps<CompPipe>())
			{
				if (candidate.mode == PipeType.Sewage)
				{
					sewagePipe = candidate;
					break;
				}
			}
			if (!HasWaterSupply || !Working().Accepted)
			{
				return false;
			}
			if (!waterPipe.pipeNet.PullWater(waterUsed, out _))
			{
				return false;
			}
			if (sewagePipe?.pipeNet != null)
			{
				sewagePipe.pipeNet.PushSewage(waterUsed);
			}
			// 水已经放下去了：冲水声放在最后，和 DBH 一致。
			DefDatabase<SoundDef>.GetNamed("CK_ToiletFlush").PlayOneShot(this);
			return true;
		}

		public bool CanUse(Pawn pawn)
		{
			return Spawned
				&& PawnAllowed(pawn)
				&& !Occupied(pawn)
				&& pawn.CanReserve(this)
				&& pawn.CanReach(this, PathEndMode.OnCell, Danger.Deadly)
				&& HasWaterSupply
				&& Working().Accepted;
		}
	}
}
