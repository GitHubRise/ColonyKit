using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_SepticTank : CompProperties_SewageHandler
	{
		/// <summary>每天处理多少升污水（DBH 的 TreatedPerDay）。</summary>
		public float TreatedPerDay = 1500f;

		public CompProperties_SepticTank()
		{
			compClass = typeof(CompSepticTank);
		}
	}

	/// <summary>
	/// DBH CompSepticTank 的移植：会「主动处理」污水的化粪池/污水处理池。
	/// 每天按 TreatedPerDay 消掉缓冲里的污水；满了 Blocked 并按基类溢出；
	/// 带「排水阈值」和「不要排空」两个设置（工人清污时会用到 EmptyNow）。
	/// </summary>
	public class CompSepticTank : CompSewageHandler
	{
		public float PcntToEmpty = 0.5f;

		public bool AllowDrain;

		public new CompProperties_SepticTank Props => (CompProperties_SepticTank)props;

		public float Pcnt => sewageBuffer / Props.capacity;

		/// <summary>每天处理多少升污水；叠乘设置里的「化粪池处理速率」倍率。</summary>
		public float TreatedPerDay =>
			Props.TreatedPerDay * ColonyKit.Modules.Hygiene.HygieneSettings.Current.sewageProcRate;

		public float TreatedPerTick => TreatedPerDay / 60000f;

		public bool EmptyNow
		{
			get
			{
				if (PcntToEmpty < 0.1f)
				{
					PcntToEmpty = 0.1f;
				}
				return AllowDrain && Pcnt > PcntToEmpty;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref PcntToEmpty, "PcntToEmpty", 0.5f);
			Scribe_Values.Look(ref AllowDrain, "AllowDrain", false);
		}

		public override void CompTick()
		{
			base.CompTick();
			Blocked = sewageBuffer >= Props.capacity;
			if (WorkingNow && sewageBuffer > 0f)
			{
				sewageBuffer = Mathf.Max(0f, sewageBuffer - TreatedPerTick);
			}
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			base.PostDestroy(mode, previousMap);
			if (parent == null || previousMap == null)
			{
				return;
			}
			MapComponent_SewagePollution pollution = previousMap.GetComponent<MapComponent_SewagePollution>();
			if (pollution == null)
			{
				return;
			}
			float perCell = sewageBuffer / Mathf.Max(1, parent.OccupiedRect().Area);
			foreach (IntVec3 cell in parent.OccupiedRect())
			{
				pollution.AddPollution(cell, perCell);
			}
		}

		public override string CompInspectStringExtra()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.CompInspectStringExtra();
			}
			StringBuilder builder = new StringBuilder();
			builder.Append("CK.Hygiene.SewageProcessing".Translate(
				TreatedPerDay.ToString("F0"), sewageBuffer.ToString("F0"), Pcnt.ToStringPercent("0.0")));
			if (!AllowDrain)
			{
				builder.AppendLine();
				builder.Append("CK.Hygiene.DoNotDrainTank".Translate());
			}
			else
			{
				builder.AppendLine();
				builder.Append("CK.Hygiene.DrainSepticTankAt".Translate(PcntToEmpty.ToStringPercent("0.0")));
			}
			return builder.ToString().TrimEndNewlines();
		}

		public void SetPcntToEmpty(float value)
		{
			PcntToEmpty = Mathf.Clamp(value, 0.1f, 1f);
		}

		public void SetAllow(bool value)
		{
			AllowDrain = value;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			yield return new Command_SetTargetDrainLevel
			{
				comp = this,
				defaultLabel = "CK.Hygiene.DrainSepticTank".Translate(),
				defaultDesc = "CK.Hygiene.DrainSepticTankDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel")
			};
			yield return new Command_Toggle
			{
				defaultLabel = "CK.Hygiene.DoNotDrainTank".Translate(),
				defaultDesc = "CK.Hygiene.DoNotDrainTank".Translate(),
				hotKey = KeyBindingDefOf.Command_ItemForbid,
				icon = AllowDrain ? TexCommand.ForbidOff : TexCommand.ForbidOn,
				isActive = () => !AllowDrain,
				toggleAction = () => SetAllow(!AllowDrain)
			};
		}
	}

	/// <summary>设置化粪池/处理池的排水阈值（DBH Command_SetTargetDrainLevel 的移植）。</summary>
	[StaticConstructorOnStartup]
	public class Command_SetTargetDrainLevel : Command
	{
		public CompSepticTank comp;

		private List<CompSepticTank> comps;

		public override void ProcessInput(UnityEngine.Event ev)
		{
			base.ProcessInput(ev);
			comps = comps ?? new List<CompSepticTank>();
			if (!comps.Contains(comp))
			{
				comps.Add(comp);
			}
			Dialog_Slider window = new Dialog_Slider(
				TextGetter,
				10,
				100,
				value =>
				{
					for (int i = 0; i < comps.Count; i++)
					{
						comps[i].SetPcntToEmpty((float)value / 100f);
					}
				},
				(int)(comp.PcntToEmpty * 100f));
			Find.WindowStack.Add(window);
		}

		private static string TextGetter(int x)
		{
			// 复用原版的「{0}%」滑块文案，和 DBH 一致。
			return "SetTargetFuelLevel".Translate(x);
		}

		public override bool InheritInteractionsFrom(Gizmo other)
		{
			comps = comps ?? new List<CompSepticTank>();
			if (other is Command_SetTargetDrainLevel command && command.comp != null)
			{
				comps.Add(command.comp);
			}
			return false;
		}
	}
}
