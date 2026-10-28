using System.Text;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_washbucket（水盆 / water tub）的移植：摆在地上的简易洗漱盆。
	///　- 一盆水 30 次用量，用一次减 1；
	///　- 露天时下雨会自动接水（+1/次，上限 30）；
	///　- 接了水网就自动补水（DBH 的浮球阀逻辑，低于 10 次时每次 +1）；
	///　- 没接水网也能用便携脸盆往里倒水（右键「倒入水盆」）。
	/// 洗漱动作复用 Building_Basin 的 JobDriver_WashHands。
	/// </summary>
	public class Building_WaterTub : Building_Basin
	{
		public const int MaxUses = 30;

		/// <summary>低于这个次数就该补水了（和 DBH 的 NeedsRefill 一致）。</summary>
		public const int RefillThreshold = 5;

		public int usesRemaining = MaxUses;

		public ContaminationLevel contamination = ContaminationLevel.Untreated;

		/// <summary>是否允许自动补水（接水网时的浮球阀）。</summary>
		public bool allowAutoRefuel = true;

		private bool ballcock;

		private Graphic waterGraphicCache;

		public override FixtureQuality Quality => FixtureQuality.shabby;

		/// <summary>水盆不靠水网也能用：盆里有水就行。</summary>
		public override bool HasWaterSupply => usesRemaining > 0 || HasNetWater;

		public bool HasNetWater => pipe?.pipeNet != null && pipe.pipeNet.WaterStorage > 0f;

		public bool NeedsRefill => usesRemaining < RefillThreshold;

		public bool Empty => usesRemaining <= 0;

		private Graphic WaterGraphic
		{
			get
			{
				if (waterGraphicCache == null)
				{
					waterGraphicCache = GraphicDatabase.Get<Graphic_Single>(
						def.graphicData.texPath + "_water",
						ShaderDatabase.CutoutComplex,
						def.graphicData.drawSize,
						Color.white,
						Color.white);
				}
				return waterGraphicCache;
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			waterGraphicCache = null;
		}

		public override AcceptanceReport Working(float WaterUsed = 0f)
		{
			if (Empty && !HasNetWater)
			{
				return "CK.Hygiene.TubEmpty".Translate();
			}
			if (blockageComp != null && blockageComp.blocked)
			{
				return "CK.Hygiene.Blocked".Translate();
			}
			return true;
		}

		public override bool TryUseWater(out ContaminationLevel contam)
		{
			contam = contamination;
			if (Empty && !HasNetWater)
			{
				return false;
			}
			// 优先喝盆里的存水；只有接水网时才从网里抽（抽到的水质覆盖盆里的）。
			if (pipe?.pipeNet != null)
			{
				float amount = waterUsedPerTick * HygieneSettings.Current.waterUseMultiplier;
				if (pipe.pipeNet.PullWater(amount, out ContaminationLevel netContam))
				{
					contam = netContam;
					contamination = netContam;
					pipe.pipeNet.PullHotWater(HeatUsedPerTick);
				}
				else if (Empty)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>洗完一次扣一次用量（JobDriver_WashHands 收尾时调用）。</summary>
		public void UseOnce()
		{
			if (usesRemaining > 0)
			{
				usesRemaining--;
			}
		}

		/// <summary>往盆里加水（便携脸盆倒水用），返回实际加进去的次数。</summary>
		public int AddWater(int uses, ContaminationLevel level)
		{
			int added = Mathf.Clamp(uses, 0, MaxUses - usesRemaining);
			if (added <= 0)
			{
				return 0;
			}
			if (Empty || level > contamination)
			{
				contamination = level;
			}
			usesRemaining += added;
			return added;
		}

		public override void TickRare()
		{
			base.TickRare();
			if (ParentHolder is MinifiedThing || Map == null)
			{
				return;
			}
			// 下雨接水：DBH 同款，每次 +1，把水冲成未处理水。
			if (Map.weatherManager.RainRate > 0.2f && !Position.Roofed(Map) && usesRemaining < MaxUses)
			{
				usesRemaining++;
				contamination = ContaminationLevel.Untreated;
			}
			// 接了水网就自动补水，补到满为止。
			if (allowAutoRefuel && pipe?.pipeNet != null)
			{
				if (usesRemaining < 10)
				{
					ballcock = true;
				}
				if (ballcock)
				{
					if (pipe.pipeNet.PullWater(1f, out ContaminationLevel netContam))
					{
						contamination = netContam;
						usesRemaining = Mathf.Min(MaxUses, usesRemaining + 1);
					}
					if (usesRemaining >= MaxUses)
					{
						ballcock = false;
					}
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref usesRemaining, "usesRemaining", MaxUses);
			Scribe_Values.Look(ref contamination, "tubContamination", ContaminationLevel.Untreated);
			Scribe_Values.Look(ref allowAutoRefuel, "allowAutoRefuel", true);
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (usesRemaining <= 0)
			{
				return;
			}
			Vector3 pos = DrawPos;
			pos.y += 0.01f;
			Color tint = Color.white;
			tint.a *= contamination == ContaminationLevel.Treated ? 0.3f
				: contamination == ContaminationLevel.Untreated ? 0.6f : 0.85f;
			Material material = WaterGraphic.MatAt(Rotation);
			material = MaterialPool.MatFrom((Texture2D)material.mainTexture, ShaderDatabase.Transparent, tint);
			Graphics.DrawMesh(WaterGraphic.MeshAt(Rotation), pos, Quaternion.identity, material, 0);
		}

		public override string GetInspectString()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.GetInspectString();
			}
			StringBuilder builder = new StringBuilder(base.GetInspectString());
			if (builder.Length != 0)
			{
				builder.AppendLine();
			}
			builder.Append("CK.Hygiene.TubWater".Translate(usesRemaining, MaxUses));
			builder.AppendLine();
			builder.Append(contamination == ContaminationLevel.Treated
				? "CK.Hygiene.WaterTreated".Translate()
				: contamination == ContaminationLevel.Untreated
					? "CK.Hygiene.WaterUntreated".Translate()
					: "CK.Hygiene.WaterContaminated".Translate());
			if (!allowAutoRefuel && pipe?.pipeNet != null)
			{
				builder.AppendLine();
				builder.Append("CK.Hygiene.AutoRefillOff".Translate());
			}
			return builder.ToString().TrimEndNewlines();
		}

		public override System.Collections.Generic.IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			yield return new Command_Toggle
			{
				defaultLabel = "CK.Hygiene.AutoRefill".Translate(),
				defaultDesc = "CK.Hygiene.AutoRefillDesc".Translate(),
				icon = allowAutoRefuel ? TexCommand.ForbidOff : TexCommand.ForbidOn,
				isActive = () => allowAutoRefuel,
				toggleAction = () => allowAutoRefuel = !allowAutoRefuel
			};
		}
	}
}
