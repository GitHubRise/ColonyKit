#nullable disable

﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	//TODO：这里需要重写，以考虑反向和群体性行为
	public class CondomUtility
	{
		public static readonly RecordDef CountOfCondomsUsed = DefDatabase<RecordDef>.GetNamed("CountOfCondomsUsed");
		public static readonly ThoughtDef SexWithCondom = DefDatabase<ThoughtDef>.GetNamed("SexWithCondom");
		public static readonly ThingDef Condom = DefDatabase<ThingDef>.GetNamedSilentFail("Condom");
		public static readonly ThingDef UsedCondom = DefDatabase<ThingDef>.GetNamedSilentFail("UsedCondom");

		/// <summary>
		/// 返回是否成功从小人身上取下一个避孕套
		/// </summary>
		/// <param name="pawn"></param>
		/// <returns></returns>
		public static bool TryUseCondom(Pawn pawn)
		{
			if (Condom == null) return false;
			if (!xxx.is_human(pawn)) return false;
			List<Thing> pawn_condoms = pawn.inventory.innerContainer.ToList().FindAll(obj => obj.def == Condom);
			if (pawn_condoms.Any())
			{
				var stack = pawn_condoms.Pop();
				stack.stackCount--;
				if (stack.stackCount <= 0)
				{
					stack.Destroy();
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// 应用使用过避孕套后的效果。
		/// </summary>
		/// <param name="pawn"></param>
		public static void useCondom(Pawn pawn)
		{
			if (Condom == null) return;
			if (!xxx.is_human(pawn)) return;
			pawn.records.Increment(CountOfCondomsUsed);
			pawn.needs.mood.thoughts.memories.TryGainMemory(SexWithCondom);
		}

		/// <summary>
		/// 尝试在性交地点附近获取避孕套
		/// </summary>
		/// <param name="pawn"></param>
		public static void GetCondomFromRoom(Pawn pawn)
		{
			if (Condom == null) return;
			if (!xxx.is_human(pawn)) return;
			List<Thing> condoms_in_room = pawn.GetRoom().ContainedAndAdjacentThings.FindAll(obj => obj.def == Condom && pawn.Position.DistanceToSquared(obj.Position) < 100);
			//List<Thing> condoms_in_room = pawn.ownership.OwnedRoom?.ContainedAndAdjacentThings.FindAll(obj => obj.def == Condom);
			if (condoms_in_room.Any())
			{
				pawn.inventory.innerContainer.TryAdd(condoms_in_room.Pop().SplitOff(1));
			}
		}
	}
}
