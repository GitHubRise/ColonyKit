using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	public class CompProperties_Composter : CompProperties
	{
		/// <summary>鎶曞叆鐨勫師鏂欙紝榛樿绮福銆?/summary>
		public ThingDef material;

		/// <summary>浜у嚭鐨勬垚鍝侊紝榛樿鏈夋満鑲ャ€?/summary>
		public ThingDef product;

		public CompProperties_Composter()
		{
			compClass = typeof(CompComposter);
		}
	}

	/// <summary>鍙槸缁?Building_Composter 鎸?props 鐨勫崰浣?comp銆?/summary>
	public class CompComposter : ThingComp
	{
		public CompProperties_Composter Props => (CompProperties_Composter)props;
	}

	/// <summary>
	/// DBH Building_Composter 鐨勭Щ妞嶏細鎶婄勃娓ｈ厫鐔熸垚鏈夋満鑲ャ€?	/// 250 瀹归噺銆佺害 3 澶╄厫鐔熷畬锛坧ertick 3.33e-6 鈫?30 涓?tick锛夛紝娓╁害浣庝簬 19鈩?浼氬彉鎱€?	/// </summary>
	[StaticConstructorOnStartup]
	public class Building_Composter : Building
	{
		public const int MaxCapacity = 250;

		public const float MinIdealTemperature = 19f;

		private static readonly Vector2 BarSize = new Vector2(0.55f, 0.1f);

		private static readonly Color BarZeroProgressColor = new Color(0.9f, 0.27f, 0.22f);

		private static readonly Color BarFermentedColor = new Color(0.9f, 0.85f, 0.2f);

		private static readonly Material BarUnfilledMat =
			SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

		private Material barFilledCachedMat;

		public float pertick = 3.3333333E-06f;

		private float progressInt;

		private int materialCount;

		public float Progress
		{
			get
			{
				return progressInt;
			}
			set
			{
				if (value != progressInt)
				{
					progressInt = value;
					barFilledCachedMat = null;
				}
			}
		}

		private Material BarFilledMat
		{
			get
			{
				if (barFilledCachedMat == null)
				{
					barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(
						Color.Lerp(BarZeroProgressColor, BarFermentedColor, Progress));
				}
				return barFilledCachedMat;
			}
		}

		public CompProperties_Composter CompProps => def.GetCompProperties<CompProperties_Composter>();

		public ThingDef MaterialDef => CompProps?.material;

		public ThingDef ProductDef => CompProps?.product;

		public int SpaceLeft => Fermented ? 0 : MaxCapacity - materialCount;

		private bool Empty => materialCount <= 0;

		public bool Fermented => !Empty && Progress >= 1f;

		private float CurrentTempProgressSpeedFactor
		{
			get
			{
				CompProperties_TemperatureRuinable props = def.GetCompProperties<CompProperties_TemperatureRuinable>();
				if (props == null)
				{
					return 1f;
				}
				float ambient = AmbientTemperature;
				if (ambient < props.minSafeTemperature)
				{
					return 0.1f;
				}
				if (ambient < MinIdealTemperature)
				{
					return GenMath.LerpDouble(props.minSafeTemperature, MinIdealTemperature, 0.1f, 1f, ambient);
				}
				return 1f;
			}
		}

		private float ProgressPerTick => pertick * CurrentTempProgressSpeedFactor;

		private int EstimatedTicksLeft => Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTick), 0);

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref materialCount, "materialCount", 0);
			Scribe_Values.Look(ref progressInt, "progress", 0f);
		}

		public override void TickRare()
		{
			base.TickRare();
			if (!Empty)
			{
				Progress = Mathf.Min(Progress + 250f * ProgressPerTick, 1f);
			}
		}

		public void AddMaterial(Thing material)
		{
			if (material == null || material.Destroyed)
			{
				return;
			}
			int amount = Mathf.Min(material.stackCount, MaxCapacity - materialCount);
			if (amount <= 0)
			{
				return;
			}
			GetComp<CompTemperatureRuinable>()?.Reset();
			Progress = GenMath.WeightedAverage(0f, amount, Progress, materialCount);
			materialCount += amount;
			material.SplitOff(amount).Destroy();
		}

		protected override void ReceiveCompSignal(string signal)
		{
			if (signal == "RuinedByTemperature")
			{
				materialCount = 0;
				Progress = 0f;
			}
		}

		public Thing TakeOutProduct()
		{
			if (!Fermented || ProductDef == null)
			{
				return null;
			}
			Thing thing = ThingMaker.MakeThing(ProductDef);
			thing.stackCount = materialCount;
			materialCount = 0;
			Progress = 0f;
			return thing;
		}

		public override string GetInspectString()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.GetInspectString();
			}
			StringBuilder builder = new StringBuilder(base.GetInspectString());
			CompTemperatureRuinable ruinable = GetComp<CompTemperatureRuinable>();
			if (!Empty && ruinable != null && !ruinable.Ruined)
			{
				if (builder.Length != 0)
				{
					builder.AppendLine();
				}
				builder.Append(Fermented
					? "CK.Agriculture.ComposterHasCompost".Translate(materialCount, MaxCapacity)
					: "CK.Agriculture.ComposterHasSludge".Translate(materialCount, MaxCapacity));
			}
			if (!Empty)
			{
				builder.AppendLine();
				if (Fermented)
				{
					builder.Append("CK.Agriculture.CompostReady".Translate());
				}
				else
				{
					builder.Append("CK.Agriculture.CompostingProgress".Translate(
						Progress.ToStringPercent(),
						EstimatedTicksLeft.ToStringTicksToPeriodVerbose()));
					if (!Mathf.Approximately(CurrentTempProgressSpeedFactor, 1f))
					{
						builder.AppendLine();
						builder.Append("CK.Agriculture.ComposterTemperature".Translate(
							CurrentTempProgressSpeedFactor.ToStringPercent()));
					}
				}
			}
			return builder.ToString().TrimEndNewlines();
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (Empty)
			{
				return;
			}
			Vector3 pos = DrawPos;
			pos.y += 3f / 64f;
			pos.z += 0.25f;
			GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
			{
				center = pos,
				size = BarSize,
				fillPercent = (float)materialCount / MaxCapacity,
				filledMat = BarFilledMat,
				unfilledMat = BarUnfilledMat,
				margin = 0.1f,
				rotation = Rot4.North
			});
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (Prefs.DevMode && !Empty && !Fermented)
			{
				yield return new Command_Action
				{
					defaultLabel = "Debug: finish composting",
					action = () => Progress = 1f
				};
			}
		}
	}
}
