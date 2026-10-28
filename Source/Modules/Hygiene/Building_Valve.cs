using System.Text;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_Valve 的移植：管道阀门。
	/// 关掉时贴图切成 *_Off，并且 RegeneratePipeGrids 会跳过关闭的管件——管网真的在这里断开。
	/// </summary>
	public class Building_Valve : Building
	{
		private CompFlickable flicker;

		private CompPipe pipeComp;

		private bool lastOn = true;

		public override Graphic Graphic => flicker != null ? flicker.CurrentGraphic : base.Graphic;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			flicker = GetComp<CompFlickable>();
			pipeComp = GetComp<CompPipe>();
			lastOn = flicker == null || flicker.SwitchIsOn;
		}

		protected override void Tick()
		{
			base.Tick();
			if (flicker == null || flicker.SwitchIsOn == lastOn)
			{
				return;
			}
			lastOn = flicker.SwitchIsOn;
			// 开关状态变了，管网要重新按「断开的管件」切分。
			pipeComp?.MapComp?.PipeComp?.RegenPipeGrids();
		}

		public override string GetInspectString()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.GetInspectString();
			}
			StringBuilder builder = new StringBuilder(base.GetInspectString());
			if (!FlickUtility.WantsToBeOn(this))
			{
				if (builder.Length > 0)
				{
					builder.AppendLine();
				}
				builder.Append("CK.Hygiene.ValveClosed".Translate());
			}
			return builder.ToString().TrimEndNewlines();
		}
	}
}
