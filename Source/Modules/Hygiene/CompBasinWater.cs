using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class CompProperties_BasinWater : CompProperties
	{
		public float Capacity = 30f;

		public CompProperties_BasinWater()
		{
			compClass = typeof(CompBasinWater);
		}
	}

	/// <summary>
	/// 便携水容器的水量（升）。脸盆 30 L、水桶 60 L 共用这个 Comp：
	/// 物品栏里随身携带，右键水井/取水点打水、右键水箱倒水，也能直接喝。
	/// </summary>
	public class CompBasinWater : ThingComp
	{
		public float Water;

		public ContaminationLevel Contamination = ContaminationLevel.Untreated;

		public CompProperties_BasinWater Props => (CompProperties_BasinWater)props;

		public bool IsFull => Water >= Props.Capacity - 0.01f;

		public bool IsEmpty => Water <= 0.01f;

		public float Space => Mathf.Max(0f, Props.Capacity - Water);

		/// <summary>往容器里加水；返回实际加进去的升数。混入更脏的水时按更脏的算。</summary>
		public float AddWater(float liters, ContaminationLevel quality)
		{
			float added = Mathf.Min(liters, Space);
			if (added <= 0.0001f)
			{
				return 0f;
			}
			if (IsEmpty || quality > Contamination)
			{
				Contamination = quality;
			}
			Water += added;
			return added;
		}

		/// <summary>喝掉/倒掉一些水；返回实际减少的升数。</summary>
		public float Drain(float liters)
		{
			float taken = Mathf.Min(liters, Water);
			Water = Mathf.Max(0f, Water - taken);
			return taken;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref Water, "Water", 0f);
			Scribe_Values.Look(ref Contamination, "Contamination", ContaminationLevel.Untreated);
		}

		public override string CompInspectStringExtra()
		{
			string text = "CK.Hygiene.WaterLevel".Translate(Water.ToString("F0"), Props.Capacity.ToString("F0"));
			string quality = Contamination == ContaminationLevel.Treated
				? "CK.Hygiene.WaterTreated".Translate()
				: Contamination == ContaminationLevel.Untreated
					? "CK.Hygiene.WaterUntreated".Translate()
					: "CK.Hygiene.WaterContaminated".Translate();
			return text + "\n" + quality;
		}
	}
}
