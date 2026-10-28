using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// DBH Building_BurnPit 的移植：烧粪渣当燃料的焚烧坑。
	/// 本质是个带燃料的 WorkTable（配方：烧尸体/烧衣物/烧毒品），粪渣既是燃料也是处理对象；
	/// 燃烧时会冒烟发光，附近的小人有概率被熏得不卫生。
	/// </summary>
	[StaticConstructorOnStartup]
	public class Building_BurnPit : Building_WorkTable
	{
		private static readonly Graphic FireGraphic = GraphicDatabase.Get<Graphic_Flicker>(
			"Things/Special/Fire",
			ShaderDatabase.TransparentPostLight,
			Vector2.one,
			Color.white);

		public CompRefuelable fuelComp;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			fuelComp = GetComp<CompRefuelable>();
		}

		public override void UsedThisTick()
		{
			base.UsedThisTick();
			if (this.IsHashIntervalTick(250))
			{
				FleckMaker.ThrowSmoke(DrawPos, Map, 1f);
				FleckMaker.ThrowFireGlow(DrawPos, Map, 1f);
			}
			if (!this.IsHashIntervalTick(1500) || def.specialDisplayRadius <= 0f)
			{
				return;
			}
			foreach (Thing thing in GenRadial.RadialDistinctThingsAround(
				Position, Map, def.specialDisplayRadius, useCenter: false))
			{
				if (thing is Pawn pawn && pawn.needs != null && Rand.Chance(0.4f))
				{
					pawn.needs.TryGetNeed<Need_Hygiene>()?.Contaminated();
				}
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (fuelComp != null && fuelComp.HasFuel)
			{
				Vector3 pos = DrawPos;
				pos.y += 3f / 64f;
				FireGraphic.Draw(pos, Rot4.North, this);
			}
		}
	}
}
