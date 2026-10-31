using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 的「武器配置」gizmo：玩家手动把背包里的备用武器换到手上，或者把手上的武器收回背包。
	/// 只有玩家点选才会执行，Mk-V 自己不会换装；背包容量完全由真实质量决定（能塞几把算几把）。
	/// </summary>
	internal static class MkVLoadoutGizmo
	{
		public static IEnumerable<Gizmo> GetGizmos(Pawn pawn)
		{
			if (pawn == null || !NannyBotUtility.IsMkV(pawn) || pawn.equipment == null)
			{
				yield break;
			}
			List<ThingWithComps> spares = MkVLoadout.SpareWeapons(pawn);
			ThingWithComps primary = MkVLoadout.Primary(pawn);
			if (spares.Count == 0 && primary == null)
			{
				yield break;
			}
			ThingDef iconDef = spares.Count > 0 ? spares[0].def : primary?.def;
			yield return new Command_Action
			{
				defaultLabel = "CK.Robotics.Loadout.Title".Translate(),
				defaultDesc = "CK.Robotics.Loadout.Title.Desc".Translate(),
				icon = iconDef?.uiIcon ?? BaseContent.BadTex,
				action = () => OpenMenu(pawn)
			};
		}

		private static void OpenMenu(Pawn pawn)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			List<ThingWithComps> spares = MkVLoadout.SpareWeapons(pawn);
			ThingWithComps primary = MkVLoadout.Primary(pawn);
			ThingWithComps offhand = MkVLoadout.Offhand(pawn);

			for (int i = 0; i < spares.Count; i++)
			{
				ThingWithComps spare = spares[i];
				if (spare == null)
				{
					continue;
				}
				options.Add(new FloatMenuOption(
					"CK.Robotics.Loadout.EquipSpare".Translate(spare.LabelShort),
					() => MkVLoadout.TryEquipSpare(pawn, spare)));
			}
			if (primary != null)
			{
				options.Add(new FloatMenuOption(
					"CK.Robotics.Loadout.MainToBag".Translate(primary.LabelShort),
					() => MkVLoadout.TryStashToBag(pawn, primary)));
			}
			if (offhand != null)
			{
				options.Add(new FloatMenuOption(
					"CK.Robotics.Loadout.OffhandToBag".Translate(offhand.LabelShort),
					() => MkVLoadout.TryStashToBag(pawn, offhand)));
			}
			if (options.Count == 0)
			{
				options.Add(new FloatMenuOption("CK.Robotics.Loadout.Empty".Translate(), null));
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}
	}
}
