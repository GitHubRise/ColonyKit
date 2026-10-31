using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_FireSprinkler : CompProperties
	{
		/// <summary>灭火半径（格）；同一房间内的火不受此半径限制。</summary>
		public float Radius = 9.9f;

		/// <summary>每次喷水消耗的水量（升）。</summary>
		public float WaterPerSpray = 2f;

		/// <summary>喷水间隔（tick）。</summary>
		public int SprayInterval = 60;

		public CompProperties_FireSprinkler()
		{
			compClass = typeof(CompFireSprinkler);
		}
	}

	/// <summary>
	/// DBH 消防喷淋的移植：接水网，房间内或半径内有火且管网水箱里存水足够时，
	/// 每 60 tick 抽 2 升水把火浇灭，并抛水花、播喷淋声。没水就不工作。
	/// </summary>
	public class CompFireSprinkler : ThingComp
	{
		private readonly List<Fire> fires = new List<Fire>();

		private CompPipe pipe;

		public CompProperties_FireSprinkler Props => (CompProperties_FireSprinkler)props;

		private bool HasWater => pipe?.pipeNet != null
			&& pipe.pipeNet.WaterStorage >= Props.WaterPerSpray;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			pipe = parent.GetComp<CompPipe>();
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!parent.Spawned || !parent.IsHashIntervalTick(Props.SprayInterval))
			{
				return;
			}
			Map map = parent.Map;
			if (map == null || pipe?.pipeNet == null)
			{
				return;
			}
			CollectFires(map);
			if (fires.Count == 0)
			{
				return;
			}
			// 水不够就干等，火焰继续烧：和「没水不工作」的约定一致。
			if (!pipe.pipeNet.PullWater(Props.WaterPerSpray, out _))
			{
				return;
			}
			ThrowSplash(map);
			for (int i = 0; i < fires.Count; i++)
			{
				Fire fire = fires[i];
				if (fire != null && fire.Spawned && !fire.Destroyed)
				{
					fire.Destroy(DestroyMode.Vanish);
				}
			}
			PlaySound();
		}

		/// <summary>收集房间内或半径内、能浇到的火；火焰在墙后时不隔墙喷。</summary>
		private void CollectFires(Map map)
		{
			fires.Clear();
			Room room = parent.GetRoom();
			List<Thing> all = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
			for (int i = 0; i < all.Count; i++)
			{
				if (!(all[i] is Fire fire) || fire.Destroyed || !fire.Spawned)
				{
					continue;
				}
				if (room != null && fire.GetRoom() == room)
				{
					fires.Add(fire);
					continue;
				}
				if (fire.Position.DistanceTo(parent.Position) <= Props.Radius
					&& GenSight.LineOfSight(parent.Position, fire.Position, map))
				{
					fires.Add(fire);
				}
			}
		}

		private void ThrowSplash(Map map)
		{
			FleckDef splash = DefDatabase<FleckDef>.GetNamedSilentFail("CK_MoteWaterSplashes");
			if (splash == null)
			{
				return;
			}
			FleckMaker.Static(parent.Position.ToVector3Shifted(), map, splash, 2.5f);
			for (int i = 0; i < fires.Count; i++)
			{
				Fire fire = fires[i];
				if (fire != null)
				{
					FleckMaker.Static(fire.Position.ToVector3Shifted(), map, splash, 1.5f);
				}
			}
		}

		private void PlaySound()
		{
			SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("CK_Sprinklers");
			if (sound != null)
			{
				SoundStarter.PlayOneShot(sound, SoundInfo.InMap(parent));
			}
		}

		public override string CompInspectStringExtra()
		{
			string status;
			if (pipe?.pipeNet == null)
			{
				status = "CK.Hygiene.FireSprinkler.NoNetwork".Translate();
			}
			else if (!HasWater)
			{
				status = "CK.Hygiene.FireSprinkler.NoWater".Translate();
			}
			else
			{
				status = "CK.Hygiene.FireSprinkler.Ready".Translate();
			}
			return "CK.Hygiene.FireSprinkler.Status".Translate(status);
		}
	}
}
