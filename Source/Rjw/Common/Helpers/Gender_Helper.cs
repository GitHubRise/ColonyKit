#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using System;

//这是处理所有变性手术的辅助库。
namespace rjw
{
	public static class GenderHelper
	{
		[Flags]
		public enum Sex 
		{
			None = 0x0,
			HasPenis = 0x01,
			HasVagina = 0x02,
			HasBreasts = 0x04,
			//FeminineBody = 0x08,

			// TODO：拥抱命运吧！把剩下的组合都加上！
			Male = HasPenis, 
			Female = HasVagina | HasBreasts,
			Trap = HasPenis | HasBreasts, 
			Futa = HasPenis | HasVagina, 
		} //已经有 Verse,Gender 了，但很烂

		//这些大概最好打包进某种可枚举结构里，这样下面的函数就不会变成 if 树了，但我不懂 C#，抱歉。
		public static HediffDef was_boy = HediffDef.Named("hediff_was_boy");
		public static HediffDef was_girl = HediffDef.Named("hediff_was_girl");
		public static HediffDef was_futa = HediffDef.Named("hediff_was_futa");
		public static HediffDef was_trap = HediffDef.Named("hediff_was_trap");

		static List<HediffDef> old_sex_list = new List<HediffDef> { was_boy, was_girl, was_futa, was_trap };
		static Dictionary<Sex, HediffDef> sex_to_old_sex = new Dictionary<Sex, HediffDef>() {
			{Sex.Male, was_boy },{Sex.Female, was_girl},{Sex.Trap, was_trap},{Sex.Futa, was_futa}
		};
		static Dictionary<HediffDef, Sex> old_sex_to_sex = sex_to_old_sex.ToDictionary(x => x.Value, x => x.Key);

		public static HediffDef m2t = HediffDef.Named("hediff_male2trap");
		public static HediffDef m2f = HediffDef.Named("hediff_male2female");
		public static HediffDef m2h = HediffDef.Named("hediff_male2futa");

		public static HediffDef f2t = HediffDef.Named("hediff_female2trap");
		public static HediffDef f2m = HediffDef.Named("hediff_female2male");
		public static HediffDef f2h = HediffDef.Named("hediff_female2futa");

		public static HediffDef h2t = HediffDef.Named("hediff_futa2trap");
		public static HediffDef h2m = HediffDef.Named("hediff_futa2male");
		public static HediffDef h2f = HediffDef.Named("hediff_futa2female");

		public static HediffDef t2h = HediffDef.Named("hediff_trap2futa");
		public static HediffDef t2m = HediffDef.Named("hediff_trap2male");
		public static HediffDef t2f = HediffDef.Named("hediff_trap2female");

		static List<HediffDef> SexChangeThoughts = new List<HediffDef> { m2t, m2f, m2h, f2t, f2m, f2h, h2t, h2m, h2f };

		public static bool HasPenis(this Sex sex) => (sex & Sex.HasPenis) == Sex.HasPenis;

		public static bool HasVagina(this Sex sex) => (sex & Sex.HasVagina) == Sex.HasVagina;

		public static Sex GetSex(Pawn pawn)
		{
			var parts = pawn.GetGenitalsList();

			bool has_vagina = Genital_Helper.has_vagina(pawn, parts);
			bool has_penis = Genital_Helper.has_male_bits(pawn, parts);
			bool has_breasts = Genital_Helper.has_breasts(pawn);
			bool has_male_breasts = Genital_Helper.has_male_breasts(pawn);
			//BodyType? bt = pawn.story?.bodyType;
			//if (bt != null)
			//	bt = BodyType.Undefined;

			Sex res;
			if (has_vagina && !has_penis)
				res = Sex.Female;
			else if (has_vagina && has_penis)
				res = Sex.Futa;
			else if (has_penis && has_breasts && !has_male_breasts)
				res = Sex.Trap;
			else if (has_penis) //以后大概应该改掉这里
				res = Sex.Male;
			else if (pawn.gender == Gender.Male)
				res = Sex.Male;
			else if (pawn.gender == Gender.Female)
				res = Sex.Female;
			else
				res = Sex.None;
			return res;
		}


		/// <summary>
		/// 检查两种性别能否做异性恋的事。
		/// </summary>
		public static bool CanBeHetero(Sex pawnSex, Sex partnerSex)
		{
			return (pawnSex.HasPenis() && partnerSex.HasVagina()) 
				|| (pawnSex.HasVagina() && partnerSex.HasPenis());
		}



		/// <summary>
		/// <para>检查两个小人能否做异性恋的事。</para>
		/// <para>如果你还要检查 <c>CanBeHomo</c>，先获取性别。</para>
		/// </summary>
		public static bool CanBeHetero(Pawn pawn, Pawn partner) =>
			CanBeHetero(GetSex(pawn), GetSex(partner));

		/// <summary>
		/// 检查两种性别能否做同性恋的事。
		/// </summary>
		public static bool CanBeHomo(Sex pawnSex, Sex partnerSex) {
			return (pawnSex.HasPenis() && partnerSex.HasPenis()) 
				|| (pawnSex.HasVagina() && partnerSex.HasVagina());		
		}

		/// <summary>
		/// <para>检查两个小人能否做同性恋的事。</para>
		/// <para>如果你还要检查 <c>CanBeHetero</c>，先获取性别。</para>
		/// </summary>
		public static bool CanBeHomo(Pawn pawn, Pawn partner) =>
			CanBeHomo(GetSex(pawn), GetSex(partner));

		/*
		public static HediffDef GetReactionHediff(Sex before, Sex after)
		{
			if (before == after)
				return null;

			if (before == Sex.Male)
				return (after == Sex.Female) ? m2f : m2t;
			else if (before == Sex.Female)
			{
				if (after == Sex.Male)
					return f2m;
				else if (after == Sex.Trap)
					return f2t;
				else if (after == Sex.Futa)
					return f2h;
				else
					return null;
			}
			else if (before == Sex.Futa && (after == Sex.Female || after == Sex.none))
				return h2f;
			else//trap to anything, futa to trap; probably won't even be reached ever
				return null;
		}
		*/

		//TODO：修复反应
		public static HediffDef GetReactionHediff(Sex before, Sex after)
		{
			if (before == after)
				return null;
			else if (before == Sex.Male)
			{
				if (after == Sex.Female)
					return m2f;
				else if (after == Sex.Trap)
					return m2t;
				else if (after == Sex.Futa)
					return m2h;
				else
					return null;
			}
			else if (before == Sex.Female)
			{
				if (after == Sex.Male)
					return f2m;
				else if (after == Sex.Trap)
					return f2t;
				else if (after == Sex.Futa)
					return f2h;
				else
					return null;
			}
			else if (before == Sex.Futa)
			{
				if (after == Sex.Male)
					return h2m;
				else if (after == Sex.Female)
					return h2f;
				else if (after == Sex.Trap)
					return h2t;
				else
					return null;
			}
			else if (before == Sex.Trap)
			{
				if (after == Sex.Male)
					return t2m;
				else if (after == Sex.Female)
					return t2f;
				else if (after == Sex.Futa)
					return t2h;
				else
					return null;
			}
			else//独角兽？
				return null;
		}

		public static bool WasThisBefore(Pawn pawn, Sex after)
		{
			Hediff was = null;

			switch (after)
			{
				case Sex.Male:
					was = pawn.health.hediffSet.GetFirstHediffOfDef(was_boy);
					break;
				case Sex.Female:
					was = pawn.health.hediffSet.GetFirstHediffOfDef(was_girl);
					break;
				case Sex.Trap:
					was = pawn.health.hediffSet.GetFirstHediffOfDef(was_trap);
					break;
				case Sex.Futa:
					was = pawn.health.hediffSet.GetFirstHediffOfDef(was_futa);
					break;
			}
			return (was != null) ? true : false;
		}

		//获取该小人之前拥有的性别之一
		public static Sex GetOriginalSex(Pawn pawn)
		{
			foreach (var os in old_sex_list)
			{
				if (pawn.health.hediffSet.GetFirstHediffOfDef(os) != null)
					return old_sex_to_sex[os];
			}
			return Sex.None;//不过不应该执行到这里
		}

		public static Hediff IsInDenial(Pawn pawn)
		{
			Hediff res = null;
			foreach (var h in SexChangeThoughts)
			{
				res = pawn.health.hediffSet.GetFirstHediffOfDef(h);
				if (res != null)
					break;
			}
			return res;
		}

		//掷骰决定小人的性别流动程度。 
		//在理想情况下，这里实际上会考虑转变是从哪里到哪里等等。
		//想法类状态本身也是如此，但现在只能做到这样
		static float RollSexChangeSeverity(Pawn pawn)
		{
			float res = 1;
			if (xxx.is_bisexual(pawn))
				res *= 0.5f;
			if (pawn.story != null && (pawn.story.bodyType == BodyTypeDefOf.Thin || pawn.story.bodyType == BodyTypeDefOf.Fat))
				res *= 0.8f;
			if (!pawn.ageTracker.CurLifeStage.reproductive)
				res *= 0.2f;
			else
				res *= AgeConfigDef.Instance.rigidityByAge.Evaluate(SexUtility.ScaleToHumanAge(pawn));

			return res;
		}

		//快速判断状态是否在增加幸福度的临时手段
		static bool is_happy(this Hediff thought)
		{
			return thought.CurStageIndex == 0;
		}
		static void make_happy(this Hediff thought)
		{
			if (thought.Severity > 0.24f)
				thought.Severity = 0.24f;//这目前是该状态的最大严重度，对应正面心情
		}
		static void mix_thoughts(this Hediff newer, Hediff older)
		{
			newer.Severity = (newer.Severity + older.Severity) / 2f;
		}

		static void GiveThought(Pawn pawn, HediffDef thought, bool happy = false, Hediff old_thought = null)
		{
			pawn.health.AddHediff(thought);
			var new_thought = pawn.health.hediffSet.GetFirstHediffOfDef(thought);
			if (happy)
			{
				new_thought.make_happy();
				return;
			}
			new_thought.Severity = RollSexChangeSeverity(pawn);
			if (old_thought != null)
			{
				new_thought.Severity = (new_thought.Severity + old_thought.Severity) / 2f;
			}
		}

		/// <summary>
		/// 执行操作，然后按需改变性别。
		/// </summary>
		public static void ChangeSex(Pawn pawn, Action action)
		{
			var before = GetSex(pawn);
			action();
			var after = GetSex(pawn);

			try
			{
				ChangeSex(pawn, before, after);
			}
			catch
			{
				ModLog.Error("ChangeSex error: " + xxx.get_pawnname(pawn));
			}
		}

		public static void ChangeSex(Pawn pawn, Sex oldsex, Sex newsex)
		{
			if (pawn.GetCompRJW() == null)
			{
				ModLog.Warning(" ChangeSex error: .GetCompRJW() should not be null");
				return;
			}

			//ModLog.Message("ChangeSex 1" + oldsex);
			//ModLog.Message("ChangeSex 2" + newsex);
			// 如果小人之前没有性取向，则唤醒它
			if (!(pawn.GetCompRJW().orientation == Orientation.Asexual || pawn.GetCompRJW().orientation == Orientation.None))
				if (oldsex == newsex)
					return;

			//更新游戏内的性别
			if (newsex == Sex.None)
				return;
			else if (newsex == Sex.Male || newsex == Sex.Trap)
				pawn.gender = Gender.Male;
			else
				pawn.gender = Gender.Female;

			// 如果小人原本“不感兴趣”，在安装部件后为其赋予性取向。
			if (oldsex == Sex.None || pawn.GetCompRJW().orientation == Orientation.Asexual || pawn.GetCompRJW().orientation == Orientation.None)
				if (pawn.kindDef.race.defName.ToLower().Contains("droid") && !AndroidsCompatibility.IsAndroid(pawn))
				{
					//基础机器人，它们不在乎
					return;
				}
				else
				{
					pawn.GetCompRJW().Sexualize(true);
				}

			var old_thought = IsInDenial(pawn);
			var react = GetReactionHediff(oldsex, newsex);

			if (old_thought == null || old_thought.is_happy())//小人要么喜欢它，要么已经习惯了
			{
				//ModLog.Message("ChangeSex 1 old_thought" + old_thought);
				//ModLog.Message("ChangeSex 1 react" + react);
				if (react != null)
				{
					// 在给皇室成员添加 rjw 的义体科技状态时，IsDesignatedHero() 会让世界生成崩溃，因此假定他们对自己的植入体很满意
					try
					{
						GiveThought(pawn, react, pawn.IsDesignatedHero());//如果不是英雄，则给予不满}
					}
					catch
					{
						ModLog.Warning(" ChangeSex error " + xxx.get_pawnname(pawn) + " faction " + pawn.Faction.Name + ". Probably tried to change sex at world gen for royalty implant, skipping");
						GiveThought(pawn, react, happy: true);
					}
				}
				if (old_thought != null)
					pawn.health.RemoveHediff(old_thought);

				//添加追踪状态
				if(oldsex != Sex.None)
					pawn.health.AddHediff(sex_to_old_sex[oldsex]);
			}
			else//小人之前不满
			{
				if (WasThisBefore(pawn, newsex))//小人很高兴能回到原来的自己
				{
					GiveThought(pawn, react, happy: true);
					pawn.health.RemoveHediff(old_thought);
				}
				else//小人仍然不满，把两者的不满混合起来
				{
					react = GetReactionHediff(GetOriginalSex(pawn), newsex);//检查对原性别的反应
					if (react != null)
					{
						GiveThought(pawn, react, old_thought: old_thought);
						pawn.health.RemoveHediff(old_thought);
					}
					//否则小人保留原有的不满想法
				}
			}
		}
	}
}
