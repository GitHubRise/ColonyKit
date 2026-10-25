using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>卫生需求：0–1，缓慢下降；洗澡恢复。</summary>
	public class Need_Hygiene : Need
	{
		private const float FallPerInterval = 0.00075f;

		public override int GUIChangeArrow => IsFrozen ? 0 : -1;

		public Need_Hygiene(Pawn pawn) : base(pawn)
		{
			threshPercents = new System.Collections.Generic.List<float> { 0.1f, 0.4f };
		}

		public override void SetInitialLevel()
		{
			CurLevel = 0.8f;
		}

		public override void NeedInterval()
		{
			if (IsFrozen)
			{
				return;
			}
			CurLevel = Mathf.Clamp01(CurLevel - FallPerInterval);
		}

		public void Clean(float amount)
		{
			CurLevel = Mathf.Clamp01(CurLevel + amount);
		}
	}
}
