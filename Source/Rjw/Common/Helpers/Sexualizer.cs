#nullable enable

using Multiplayer.API;
using Verse;

namespace rjw
{
	public static class Sexualizer
	{
		/// <summary>
		/// 单一性别就是扶她，但不转换为女性性别。
		/// </summary>
		static void SexualizeSingleGenderPawn(Pawn pawn)
		{
			if (RJWSettings.DevMode)
				ModLog.Message(" SexualizeSingleGenderPawn(), treating pawn As Futa" + xxx.get_pawnname(pawn));

			SexPartAdder.add_genitals(pawn, null, Gender.Male);
			SexPartAdder.add_genitals(pawn, null, Gender.Female);
			SexPartAdder.add_breasts(pawn, null, Gender.Female);
			SexPartAdder.add_anus(pawn, null, Gender.Female);
		}

		/// <summary>
		/// 单一性别就是扶她，并转换为女性性别。
		/// </summary>
		static void SexualizeGenderlessPawn(Pawn pawn)
		{
			if (RJWSettings.GenderlessAsFuta && !xxx.is_mechanoid(pawn) && (pawn.RaceProps.Animal || pawn.RaceProps.Humanlike))
			{
				if (RJWSettings.DevMode)
					ModLog.Message(" SexualizeGenderlessPawn() - genderless pawn, treating Genderless pawn As Futa " + xxx.get_pawnname(pawn));
				//将扶她的性别设为女性
				pawn.gender = Gender.Female;
				SexPartAdder.add_genitals(pawn, null, Gender.Male);
				SexPartAdder.add_genitals(pawn, null, Gender.Female);
				SexPartAdder.add_breasts(pawn, null, Gender.Female);
				SexPartAdder.add_anus(pawn, null, Gender.Female);
			}
			else
			{
				if (RJWSettings.DevMode)
					ModLog.Message(" SexualizeGenderlessPawn() - unable to sexualize genderless pawn " + xxx.get_pawnname(pawn) + " gender: " + pawn.gender);
			}
		}

		[SyncMethod]
		static void SexualizeGenderedPawn(Pawn pawn)
		{
			//应用正常性别
			SexPartAdder.add_genitals(pawn, null, pawn.gender);

			//应用扶她性别
			//if (pawn.gender == Gender.Female) // 把男性改为扶她会因关系问题破坏小人生成

			if (!xxx.is_animal(pawn))
			{
				int techLevel = (int)(pawn.Faction?.def.techLevel ?? 0);

				//ModLog.Message(" SexualizeGenderedPawn( " + xxx.get_pawnname(pawn) + " ) techLevel: " + techLevel);
				//ModLog.Message(" SexualizeGenderedPawn( " + xxx.get_pawnname(pawn) + " ) techLevel: " + (pawn.Faction?.Name != null ? pawn.Faction.name : "No faction"));

				//土著/太空时代扶她
				float chance = techLevel < 5 ? RJWSettings.futa_natives_chance : RJWSettings.futa_spacers_chance;
				//花痴扶她性别
				chance = xxx.is_nympho(pawn) ? RJWSettings.futa_nymph_chance : chance;

				//ModLog.Message($"SexualizeGenderedPawn {chance} from {RJWSettings.futa_nymph_chance} {RJWSettings.futa_natives_chance} {RJWSettings.futa_spacers_chance}");
				if (Rand.Chance(chance))
				{
					//生成扶她
					if (pawn.gender == Gender.Female && RJWSettings.FemaleFuta)
						SexPartAdder.add_genitals(pawn, null, Gender.Male);
					//生成伪娘
					else if (pawn.gender == Gender.Male && RJWSettings.MaleTrap)
					{
						SexPartAdder.add_breasts(pawn, null, Gender.Female);
						SexPartAdder.add_anus(pawn, null, pawn.gender);
						return;
					}
				}
			}
			SexPartAdder.add_breasts(pawn, null, pawn.gender);
			SexPartAdder.add_anus(pawn, null, pawn.gender);
		}

		[SyncMethod]
		public static void sexualize_pawn(Pawn pawn)
		{
			//ModLog.Message(" sexualize_pawn( " + xxx.get_pawnname(pawn) + " ) called");
			if (pawn == null)
			{
				ModLog.Error("Called with pawn = null");
				return;
			}

			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out RaceGroupDef raceGroupDef))
			{
				//ModLog.Message($" sexualize_pawn( {xxx.get_pawnname(pawn)} ) RaceGroupDef {raceGroupDef}");
				//覆盖小人
				if (RacePartDef_Helper.TryRacePartDef_partAdders(pawn, raceGroupDef))
				{
					//ModLog.Message($" sexualize_pawn( {xxx.get_pawnname(pawn)} ) TryRacePartDef_partAdders returned true");
					return;
				}

				if (raceGroupDef.hasSingleGender)
				{
					if (RJWSettings.DevMode)
						ModLog.Message($"sexualize_pawn() - sexualizing single gender pawn {xxx.get_pawnname(pawn)}  race: {raceGroupDef.defName}");
					SexualizeSingleGenderPawn(pawn);
					return;
				}
			}

			if (pawn.RaceProps.hasGenders)
			{
				//ModLog.Message($"sexualize_pawn() - sexualizing gendered pawn {xxx.get_pawnname(pawn)}  def: {pawn.def.defName}");
				SexualizeGenderedPawn(pawn);
			}
			else
			{
				if (Current.ProgramState == ProgramState.Playing) // 不要在世界生成时运行，会在生成关系内容时抛出错误
				{
					if (RJWSettings.DevMode)
						ModLog.Message($"sexualize_pawn() - sexualizing genderless pawn {xxx.get_pawnname(pawn)}  def: {pawn.def.defName}");
					SexualizeGenderlessPawn(pawn);
					return;
				}
			}

			if (!pawn.Dead)
			{
				//在生成时增加 tick，这样小人生成后不会立刻开始做爱（尤其是在剧本开始时）。
				//if (xxx.RoMIsActive && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_ShapeshiftHD"))) // Rimworld of Magic's polymorph/shapeshift
				//	pawn.mindState.canLovinTick = Find.TickManager.TicksGame + (int)(SexUtility.GenerateMinTicksToNextLovin(pawn) * Rand.Range(0.01f, 0.05f));
				if (!xxx.is_insect(pawn))
					pawn.mindState.canLovinTick = Find.TickManager.TicksGame + (int)(SexUtility.GenerateMinTicksToNextLovin(pawn) * Rand.Range(0.1f, 1.0f));
				else
					pawn.mindState.canLovinTick = Find.TickManager.TicksGame + (int)(SexUtility.GenerateMinTicksToNextLovin(pawn) * Rand.Range(0.01f, 0.2f));

				//ModLog.Message(" sexualize_pawn( " + xxx.get_pawnname(pawn) + " ) add sexneed");
				if (pawn.RaceProps.Humanlike)
				{
					var sex_need = pawn.needs.TryGetNeed<Need_Sex>();
					if (pawn.Faction != null && !(pawn.Faction?.IsPlayer ?? false) && sex_need != null)
					{
						sex_need.CurLevel = Rand.Range(0.01f, 0.75f);
					}
				}
			}
		}
	}
}
