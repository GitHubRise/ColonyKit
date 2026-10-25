#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace rjw
{
	/// <summary>
	/// 此状态类模拟孕育出类人子女的怀孕。
	/// </summary>
	[RJWAssociatedHediff("RJW_pregnancy")]
	public class Hediff_HumanlikePregnancy : Hediff_BasePregnancy
	{
		//处理小人的生成与关系添加
		public override void GiveBirth()
		{
			Pawn mother = pawn;
			if (mother == null)
				return;

			InitializeIfNoBabies();
			List<Pawn> siblings = new List<Pawn>();
			foreach (Pawn baby in babies)
			{
				//备份黑色素和姓氏，以防婴儿在生成/背景故事变更时被其他模组重置
				//var skin_whiteness = baby.story.melanin;
				//var last_name = baby.story.birthLastName;

				//ModLog.Message("" + this.GetType().ToString() + " pre TrySpawnHatchedOrBornPawn: " + baby.story.birthLastName);
				PawnUtility.TrySpawnHatchedOrBornPawn(baby, mother);
				//ModLog.Message("" + this.GetType().ToString() + " post TrySpawnHatchedOrBornPawn: " + baby.story.birthLastName);

				var sex_need = mother.needs?.TryGetNeed<Need_Sex>();
				if (mother.Faction != null && !(mother.Faction?.IsPlayer ?? false) && sex_need != null)
				{
					sex_need.CurLevel = 1.0f;
				}
				if (mother.IsSlaveOfColony)
				{
					//ModLog.Message("mother.SlaveFaction " + mother.SlaveFaction);
					//ModLog.Message("mother.HomeFaction " + mother.HomeFaction);
					//ModLog.Message("mother.Faction " + mother.Faction);

					if (mother.SlaveFaction != null)
						baby.SetFaction(mother.SlaveFaction);
					else if (mother.HomeFaction != null)
						baby.SetFaction(mother.HomeFaction);
					else if (mother.Faction != null)
						baby.SetFaction(mother.Faction);
					else
						baby.SetFaction(Faction.OfPlayer);
					baby.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
				}
				else if (mother.IsPrisonerOfColony)
				{
					//ModLog.Message("mother.HomeFaction " + mother.HomeFaction);
					if (mother.HomeFaction != null)
						baby.SetFaction(mother.HomeFaction);
					baby.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
				}

				baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, mother);
				if (father != null && mother != father)
				{
					baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, father);
				}

				//ModLog.Message("" + this.GetType().ToString() + " pre PostBirth: " + baby.story.birthLastName);
				PostBirth(mother, father, baby);
				//ModLog.Message("" + this.GetType().ToString() + " post PostBirth: " + baby.story.birthLastName);

				//备份黑色素和姓氏，以防婴儿在生成/背景故事变更时被其他模组重置
				//baby.story.melanin = skin_whiteness;
				//baby.story.birthLastName = last_name;
			}
			mother.health.RemoveHediff(this);
		}
	}
}
