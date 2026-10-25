#nullable disable

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace rjw
{
	///<summary>
	///此状态类模拟以动物为子女的怀孕，母亲可能是人类。本设计并不追求合理。
	///与类人怀孕的区别在于：动物会获得一些训练，且亲子关系采用较轻的惩罚。
	///</summary>	
	[RJWAssociatedHediff("RJW_pregnancy_beast")]
	public class Hediff_BestialPregnancy : Hediff_BasePregnancy
	{
		private static readonly PawnRelationDef relation_birthgiver = PawnRelationDefOf.Parent;
		//static int max_train_level = TrainableUtility.TrainableDefsInListOrder.Sum(tr => tr.steps);

		public override void PregnancyMessage()
		{
			string message_title = "RJW_PregnantTitle".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
			string message_text1 = "RJW_PregnantText".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
			string message_text2 = "RJW_PregnantStrange".Translate();
			Find.LetterStack.ReceiveLetter(message_title, message_text1 + "\n" + message_text2, LetterDefOf.NeutralEvent, pawn);
		}

		//让半人婴儿有更好的起点。它们出生即听话；若母亲是人类，还会获得提升训练的状态
		protected void train(Pawn baby, Pawn mother, Pawn father)
		{
			bool _;
			if (!xxx.is_human(baby) && baby.Faction == Faction.OfPlayer)
			{
				if (xxx.is_human(mother) && baby.Faction == Faction.OfPlayer && baby.training.CanAssignToTrain(TrainableDefOf.Obedience, out _).Accepted)
				{
					baby.training.Train(TrainableDefOf.Obedience, mother);
				}
				if (xxx.is_human(mother) && baby.Faction == Faction.OfPlayer && baby.training.CanAssignToTrain(TrainableDefOf.Tameness, out _).Accepted)
				{
					baby.training.Train(TrainableDefOf.Tameness, mother);
				}
			}
			//baby.RaceProps.TrainableIntelligence.LabelCap.
			//if (xxx.is_human(mother))
			//{
			//	让这些动物出生即成为殖民地财产
			//	if (mother.IsPrisonerOfColony || mother.IsColonist)
			//	{
			//		baby.SetFaction(Faction.OfPlayer);
			//	}
			//	让它被训练到上限的一半
			//	var baby_int = baby.RaceProps.TrainableIntelligence;
			//	int max_int = TrainableUtility.TrainableDefsInListOrder.FindLastIndex(tr => (tr.requiredTrainableIntelligence == baby_int));
			//	if (max_int == -1)
			//		return;
			//	ModLog.Message("RJW training " + baby + " max_int is " + max_int);
			//	var available_tricks = TrainableUtility.TrainableDefsInListOrder.GetRange(0, max_int + 1);
			//	int max_steps = available_tricks.Sum(tr => tr.steps);
			//	ModLog.Message("RJW training " + baby + " vill do " + max_steps/2 + " steps");
			//	int t_score = Rand.Range(Mathf.RoundToInt(max_steps / 4), Mathf.RoundToInt(max_steps / 2));
			//	for (int i = 1; i <= t_score; i++)
			//	{
			//		var tr = available_tricks.Where(t => !baby.training.IsCompleted(t)). RandomElement();
			//		ModLog.Message("RJW training " + baby + " for " + tr);
			//		baby.training.Train(tr, mother);
			//	}

			//	baby.health.AddHediff(HediffDef.Named("RJW_smartPup"));
			//}
		}

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

				PawnUtility.TrySpawnHatchedOrBornPawn(baby, mother);

				Need_Sex sex_need = mother.needs?.TryGetNeed<Need_Sex>();
				if (mother.Faction != null && !(mother.Faction?.IsPlayer ?? false) && sex_need != null)
				{
					sex_need.CurLevel = 1.0f;
				}

				if (!RJWSettings.Disable_bestiality_pregnancy_relations)
				{
					baby.relations.AddDirectRelation(relation_birthgiver, mother);
					if (father != null && mother != father)
					{
						baby.relations.AddDirectRelation(relation_birthgiver, father);
					}
				}
				train(baby, mother, father);

				PostBirth(mother, father, baby);

				//备份黑色素和姓氏，以防婴儿在生成/背景故事变更时被其他模组重置
				//baby.story.melanin = skin_whiteness;
				//baby.story.birthLastName = last_name;
			}
			mother.health.RemoveHediff(this);
		}
	}
}
