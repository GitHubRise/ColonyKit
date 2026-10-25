#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;
using System.Linq;
using UnityEngine;
using Multiplayer.API;


namespace rjw
{
	///<summary>
	///此状态类模拟与机械族的怀孕，母亲可能是人类。本设计并不追求合理。
	///与兽交怀孕的区别在于……这会致命
	///TODO：能否扩展出比 Mech_Scyther 更“友好”的东西……两个 Mech_Scyther？哈哈哈
	///</summary>	
	[RJWAssociatedHediff("RJW_pregnancy_mech")]
	public class Hediff_MechanoidPregnancy : Hediff_BasePregnancy
	{
		// 由于出生时父亲的机械单位不保证存在，我们保存父亲的种类和派系，以便生成婴儿机械。
		private PawnKindDef backupFatherKind;
		private FactionDef backupFatherFaction;

		public override bool canBeAborted
		{
			get
			{
				return false;
			}
		}

		public override bool canMiscarry
		{
			get
			{
				return false;
			}
		}

		public override void PregnancyMessage()
		{
			string message_title = "RJW_PregnantTitle".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
			string message_text1 = "RJW_PregnantText".Translate(pawn.LabelIndefinite()).CapitalizeFirst();
			string message_text2 = "RJW_PregnantMechStrange".Translate();
			Find.LetterStack.ReceiveLetter(message_title, message_text1 + "\n" + message_text2, LetterDefOf.ThreatBig, pawn);
		}

		public override void PostAdd(DamageInfo? dinfo)
		{
			base.PostAdd(dinfo);
			backupFatherKind = father?.kindDef;
			backupFatherFaction = father?.Faction?.def;
		}

		public void Hack()
		{
			is_hacked = true;
		}

		public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
		{
			base.Notify_PawnDied(dinfo, culprit);
			GiveBirth();
		}

		//处理小人的生成
		[SyncMethod]
		public override void GiveBirth()
		{
			Pawn mother = pawn;
			if (mother == null)
				return;

			if (!babies.NullOrEmpty())
			{
				foreach (Pawn baby in babies)
				{
					baby.Destroy();
					baby.Discard(true);
				}
				babies.Clear();
			}

			Faction spawn_faction = null;

			if (!is_hacked)
			{
				spawn_faction = father?.Faction;
				spawn_faction ??= Find.FactionManager.FirstFactionOfDef(backupFatherFaction);
				spawn_faction ??= Faction.OfMechanoids;
			}
			else if (ModsConfig.BiotechActive)
				spawn_faction = Faction.OfPlayer;

			PawnKindDef children = null;

			if (RJWSettings.DevMode) ModLog.Message(xxx.get_pawnname(pawn) + " birth:" + this.ToString());

			var fatherKindDef = father?.kindDef ?? backupFatherKind;

			foreach (HediffDef_MechImplants implant in DefDatabase<HediffDef_MechImplants>.AllDefs.Where(x => x.parentDefs.Contains(fatherKindDef.ToString())))         //尝试查找预定义
			{
				string childrendef;																			//尝试查找预定义
				List<string> childlist = new List<string>();
				if (!implant.childrenDefs.NullOrEmpty())
				{
					foreach (var child in (implant.childrenDefs))
					{
						if (DefDatabase<PawnKindDef>.GetNamedSilentFail(child) != null)
							childlist.AddDistinct(child);
					}
					childrendef = childlist.RandomElement();												//尝试查找预定义
					children = DefDatabase<PawnKindDef>.GetNamedSilentFail(childrendef);
					if (children != null)
						continue;
				}
			}

			if (children == null)                                                                           //回退，使用 fatherDef
				children = fatherKindDef;

			PawnGenerationRequest request = new PawnGenerationRequest(
				kind: children,
				faction: spawn_faction,
				forceGenerateNewPawn: true,
				developmentalStages: DevelopmentalStage.Newborn
				);

			Pawn mech = PawnGenerator.GeneratePawn(request);
			PawnUtility.TrySpawnHatchedOrBornPawn(mech, mother);
			if (!is_hacked)
			{
				LordJob_MechanoidsDefend lordJob = new LordJob_MechanoidsDefend();
				Lord lord = LordMaker.MakeNewLord(mech.Faction, lordJob, mech.Map);
				lord.AddPawn(mech);
			}
			FilthMaker.TryMakeFilth(mech.PositionHeld, mech.MapHeld, mother.RaceProps.BloodDef, mother.LabelIndefinite());

			IEnumerable<BodyPartRecord> source = from x in mother.health.hediffSet.GetNotMissingParts() where 
												x.IsInGroup(BodyPartGroupDefOf.Torso)
												&& !x.IsCorePart
												//&& x.groups.Contains(BodyPartGroupDefOf.Torso)
												//&& x.depth == BodyPartDepth.Inside
												//&& x.height == BodyPartHeight.Bottom
												//以后加入深度过滤
												//以免切掉外部器官（乳房）？
												//阴道是生殖器部件，而生殖器属于外部
												//肛门属于内部
												//把独立的部件也算作阴道的一部分？
												//&& x.depth == BodyPartDepth.Inside
												select x;

			mother.health.RemoveHediff(this);
			if (source.Any())
			{
				foreach (BodyPartRecord part in source)
				{
					mother.health.DropBloodFilth();
				}
				if (RJWPregnancySettings.safer_mechanoid_pregnancy && is_checked)
				{
					foreach (BodyPartRecord part in source)
					{
						DamageDef surgicalCut = DamageDefOf.SurgicalCut;
						float amount = 5f;
						float armorPenetration = 999f;
						mother.TakeDamage(new DamageInfo(surgicalCut, amount, armorPenetration, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
					}
				}
				else
				{
					foreach (BodyPartRecord part in source)
					{
						Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, mother, part);
						hediff_MissingPart.lastInjury = HediffDefOf.Cut;
						hediff_MissingPart.IsFresh = true;
						mother.health.AddHediff(hediff_MissingPart);
					}
				}
			}
			
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref backupFatherFaction, "backupFatherFaction");
			Scribe_Defs.Look(ref backupFatherKind, "backupFatherKind");
		}
	}
}
