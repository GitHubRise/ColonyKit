#nullable disable

using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using rjw.Modules.Interactions;
using rjw.RJWUI;

namespace rjw
{
	//TODO：检查史莱姆相关功能在多人游戏中是否正常
	//TODO：把各个菜单拆分到子脚本里
	//TODO：添加恶魔切换部件
	//TODO：搞清楚并做个界面？
	//TODO：为仿生部件添加振动开关（+50% 伴侣满意度？）


	[Obsolete]
	public class Dialog_Partcard : Window
	{

		private readonly Pawn pawn;
		private readonly List<ISexPartHediff> parts;
		private readonly string label;
		private int index;
		private static readonly StringBuilder sb = new();

		public Dialog_Partcard(Pawn pawn, List<ISexPartHediff> parts, string label)
		{
			this.pawn = pawn;
			this.parts = parts;
			this.label = label;
			this.index = 0;
		}

		public override void DoWindowContents(Rect inRect)
		{
			bool closeFlag = false;
			soundClose = SoundDefOf.InfoCard_Close;
			closeOnClickedOutside = true;
			absorbInputAroundWindow = false;
			forcePause = true;
			preventCameraMotion = false;
			if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
			{
				closeFlag = true;
				Event.current.Use();
			}
			Rect windowRect = inRect.ContractedBy(17f);
			Rect mainRect = new Rect(windowRect.x, windowRect.y, windowRect.width, windowRect.height - 20f);
			Rect okRect = new Rect(inRect.width / 3, mainRect.yMax + 10f, inRect.width / 3f, 30f);

			bool devMode = RJWSettings.DevMode || Prefs.DevMode;

			{
				var rect = mainRect;
				Text.Font = GameFont.Medium;
				Rect rect1 = new Rect(8f, 4f, rect.width - 8f, rect.height - 20f);
				Widgets.Label(rect1, "PartsEditLabel".Translate(label));

				Text.Font = GameFont.Tiny;
				float num = rect1.y + 40f;
				Rect row1 = new Rect(10f, num, rect.width - 8f, 24f);
				Rect row2 = new Rect(10f, num + 35, rect.width - 8f, 30f);
				Rect row3 = new Rect(10f, num + 70, rect.width - 8f, 30f);
				Rect row4 = new Rect(10f, num + 105, rect.width - 8f, 30f);
				Rect row5 = new Rect(10f, num + 160, rect.width - 8f, 0f);
				row5.y -= devMode ? 0f : 50f;
				row5.height = mainRect.yMax - row5.y;

				// 部件下拉菜单
				{
					bool duplicates = parts.GroupBy(x => x.Def).Any(g => g.Count() > 1);
					Func<int, ISexPartHediff> getPart = (int i) => parts[i];
					Func<int, string> getLabel = (int i) =>
					{
						return $"{getPart(i).AsHediff.LabelBaseCap}" + (duplicates ? $" ({i+1})" : "");
					};
					Func<int, List<Widgets.DropdownMenuElement<ISexPartHediff>>> getMenu = (int _) =>
					{
						var menu = new List<Widgets.DropdownMenuElement<ISexPartHediff>>();
						for (int i_ = 0; i_ < parts.Count; i_++)
						{
							int i = i_;
							Widgets.DropdownMenuElement<ISexPartHediff> element;
							element.payload = getPart(i);
							element.option = new FloatMenuOption(getLabel(i), () =>
							{
								this.index = i;
							});
							menu.Add(element);
						}
						return menu;
					};
					Widgets.Dropdown(row1, this.index, getPart, getMenu, getLabel(this.index));
				}

				var part = parts[this.index];
				var def = part.Def;
				var comp = part.AsHediff.TryGetComp<HediffComp_SexPart>();
				var min = def.stages.First().minSeverity;
				var max = def.stages.Last().minSeverity;

				// 尺寸滑块
				float sevResult = Widgets.HorizontalSlider(
					row2, comp.GetSeverity(), min, max,
					label: $"{part.AsHediff.LabelInBrackets}",
					leftAlignedLabel: $"{min:F2}", rightAlignedLabel: $"{max:F2}"
				);
				if(sevResult != comp.GetSeverity())
				{
					comp.SetSeverity(sevResult);
				}

				// 液体倍率滑块
				float currentFluid = comp.partFluidMultiplier;
				float maxFluid = devMode ? (currentFluid < 10f ? 10f : 100f) : 2f;
				float fluidResult = Widgets.HorizontalSlider(
					row3, comp.partFluidMultiplier, 0f, maxFluid,
					label: "PartsEditFluidMultiplierLabel".Translate(),
					leftAlignedLabel: $"{0.0f:F2}", rightAlignedLabel: $"{maxFluid:F2}"
				);
				if(fluidResult != comp.partFluidMultiplier)
				{
					comp.partFluidMultiplier = fluidResult;
				}

				// 液体类型下拉菜单
				if(devMode)
				{
					var fluids = DefDatabase<SexFluidDef>.AllDefs.ToList();
					fluids.Insert(0, null);

					Func<int, SexFluidDef> getFluid = (int i) => fluids[i];
					Func<SexFluidDef, string> getLabel = (SexFluidDef fluid) =>
					{
						return fluid?.LabelCap ?? "None".Translate();
					};
					Func<int, string> getDropdownLabel = (int i) =>
					{
						var fluid = getFluid(i);
						return getLabel(fluid) + (fluid == comp.Def.fluid ? "SC_FluidTypeDefault".Translate() : "");
					};
					Func<int, List<Widgets.DropdownMenuElement<SexFluidDef>>> getMenu = (int _) =>
					{
						var menu = new List<Widgets.DropdownMenuElement<SexFluidDef>>();
						for (int i_ = 0; i_ < fluids.Count; i_++)
						{
							int i = i_;
							Widgets.DropdownMenuElement<SexFluidDef> element;
							element.payload = getFluid(i);
							element.option = new FloatMenuOption(getDropdownLabel(i), () =>
							{
								comp.Fluid = getFluid(i);
							});
							menu.Add(element);
						}
						return menu;
					};

					using (new UiScope(font: GameFont.Small, anchor: TextAnchor.UpperCenter))
					{
						var fluidLabel = new Rect(row4.x, row4.y - 4f, row4.width, row4.height);
						Widgets.Label(fluidLabel, "SC_FluidType".Translate());
					}
					var fluidDropdown = new Rect(row4.x, row4.y + 20f, row4.width, 24f);
					Widgets.Dropdown(fluidDropdown, fluids.IndexOf(comp.Fluid), getFluid, getMenu, getLabel(comp.Fluid));
				}

				// 提示信息区域
				sb.Clear();
				foreach(string line in comp.GetTipString(extended: true))
				{
					sb.AppendLine(line);
				}
				sb.AppendLine("RJW_PartInfo_severity".Translate($"{comp.GetSeverity():F2}") );
				sb.AppendLine();
				if (!RJWSettings.DevMode) // 已在提示信息中
				{
					sb.AppendLine("RJW_PartInfo_fluidMultiplier".Translate($"{comp.partFluidMultiplier:F2}"));
				}
				string tip = sb.ToString();

				var scroll = new Vector2(0,0);

				using (new UiScope(font: GameFont.Small))
				{
					Widgets.LabelScrollable(row5, tip, ref scroll);
				}
				Text.Font = GameFont.Tiny;
			}

			
			if (Widgets.ButtonText(okRect, "CloseButton".Translate()) || closeFlag)
			{
				Close();
			}
		}
	}

	[Obsolete]
	public class Dialog_Sexcard : Window
	{
		private readonly Pawn pawn;

		public Dialog_Sexcard(Pawn editFor)
		{
			pawn = editFor;
		}

		[Obsolete]
		public static breasts Breasts;
		[Obsolete]
		public enum breasts
		{
			selectone,
			none,
			featureless_chest,
			flat_breasts,
			small_breasts,
			average_breasts,
			large_breasts,
			huge_breasts,
			slime_breasts,
		};

		[Obsolete]
		public static anuses Anuses;
		[Obsolete]
		public enum anuses
		{
			selectone,
			none,
			micro_anus,
			tight_anus,
			average_anus,
			loose_anus,
			gaping_anus,
			slime_anus,
		};

		[Obsolete]
		public static vaginas Vaginas;
		[Obsolete]
		public enum vaginas
		{
			selectone,
			none,
			micro_vagina,
			tight_vagina,
			average_vagina,
			loose_vagina,
			gaping_vagina,
			slime_vagina,
			feline_vagina,
			canine_vagina,
			equine_vagina,
			dragon_vagina,
		};

		[Obsolete]
		public static penises Penises;
		[Obsolete]
		public enum penises
		{
			selectone,
			none,
			micro_penis,
			small_penis,
			average_penis,
			big_penis,
			huge_penis,
			slime_penis,
			feline_penis,
			canine_penis,
			equine_penis,
			dragon_penis,
			raccoon_penis,
			hemipenis,
			crocodilian_penis,
		};

		public void SexualityCard(Rect rect, Pawn pawn)
		{
			CompRJW comp = pawn.GetCompRJW();
			if (pawn == null || comp == null) return;

			Rect rect1 = new Rect(8f, 4f, rect.width - 8f, rect.height - 20f);

			using (new UiScope(font: GameFont.Medium, anchor: TextAnchor.UpperCenter, color: Color.yellow))
			{
				Widgets.Label(rect1, "SC_title".Translate());
			}

			Text.Font = GameFont.Tiny;
			float num = rect1.y + 40f;
			Rect sexuality_row = new Rect(10f, num, rect.width - 8f, 24f);//性取向
			Rect pubic_row = new Rect(10f, num + 24f, rect.width - 8f, 24f);//阴毛密度
			//Rect row2 = new Rect(10f, num + 24, rect.width - 8f, 24f);//怪癖
			//Rect row3 = new Rect(10f, num + 48, rect.width - 8f, 24f);//卖淫价格

			Rect button1 = new Rect(10f, rect1.height - 10f, rect.width - 8f, 24f);//重新性化
			Rect button2 = new Rect(10f, rect1.height - 34f, rect.width - 8f, 24f);//远古科技开关
			Rect button3 = new Rect(10f, rect1.height - 58f, rect.width - 8f, 24f);//乳房
			Rect button4 = new Rect(10f, rect1.height - 82f, rect.width - 8f, 24f);//肛门
			Rect button5 = new Rect(10f, rect1.height - 106f, rect.width - 8f, 24f);//阴道
			Rect button6 = new Rect(10f, rect1.height - 130f, rect.width - 8f, 24f);//阴茎 1

			using (new UiScope(font: GameFont.Small))
			{
				DrawSexuality(pawn, sexuality_row);
			}

			using (new UiScope(font: GameFont.Small))
			{
				DrawPubicHair(pawn, pubic_row);
			}
			//DrawWhoring(pawn, row3);

			if (RJWSettings.DevMode || Current.ProgramState != ProgramState.Playing)
			{
				if (Widgets.ButtonText(button1, Current.ProgramState != ProgramState.Playing ? "SC_Reroll".Translate() : $"[DEV] {"SC_Reroll".Translate()}"))
				{
					Re_sexualize(pawn);
				}
			}

			//远古科技生殖器的生育力开关
			var GenitalBPR = Genital_Helper.get_genitalsBPR(pawn);
			IEnumerable<ISexPartHediff> parts = Genital_Helper.get_PartsHediffList(pawn, GenitalBPR).Cast<ISexPartHediff>();

			if (parts.Any())
			{
				foreach (var p in parts)
				{
					if (p.Def.partTags.Contains("FertilityToggle"))
					{
						if (pawn.health.hediffSet.HasHediff(HediffDef.Named("ImpregnationBlocker")))
						{
							if (Widgets.ButtonText(button2, "SC_Fertility_Enable".Translate()))
							{
								Change_Fertility(pawn);
							}
						}
						else if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("FertilityEnhancer")))
						{
							if (Widgets.ButtonText(button2, "SC_Fertility_Enchance".Translate()))
							{
								Change_Fertility(pawn);
							}
						}
						else
						{
							if (Widgets.ButtonText(button2, "SC_Fertility_Disable".Translate()))
							{
								Change_Fertility(pawn);
							}
						}
						break;
					}
				}
			}

			// TODO：添加多人游戏同步器
			// TODO：把这堆烂摊子清理一下
			// TODO：添加恶魔开关
			if (MP.IsInMultiplayer)
				return;

			if (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || RJWSettings.DevMode || Current.ProgramState != ProgramState.Playing)
			{
				var pawnParts = pawn.GetLewdParts();
				pawnParts.PopulateAll();

				Make_button(button6, pawn, pawnParts.Breasts.AsSexPartHediffs(), HediffDef.Named("Breasts").LabelCap);
				Make_button(button5, pawn, pawnParts.Vaginas.AsSexPartHediffs(), HediffDef.Named("Vagina").LabelCap);
				Make_button(button4, pawn, pawnParts.Anuses.AsSexPartHediffs(), HediffDef.Named("Anus").LabelCap);
				Make_button(button3, pawn, pawnParts.Penises.AsSexPartHediffs(), HediffDef.Named("Penis").LabelCap);
			}
		}

		/// <summary>
		/// 绘制阴毛密度一行。老存档里的小人还没有这个 hediff，所以先补建再读。
		/// Dev 模式下点击可循环切换档位。
		/// </summary>
		/// <param name="pawn">要展示的小人。</param>
		/// <param name="row">绘制区域。</param>
		static void DrawPubicHair(Pawn pawn, Rect row)
		{
			PubicHairHelper.Ensure(pawn);

			if (!PubicHairHelper.TryGetDensity(pawn, out _))
			{
				return;
			}

			Hediff_PubicHair hediff = PubicHairHelper.Get(pawn);
			string label = "SC_PubicHair".Translate() + ": " + hediff.LabelBaseCap;

			bool canCycle = RJWSettings.DevMode && !MP.IsInMultiplayer;
			if (canCycle && Widgets.ButtonText(row, $"[DEV] {label}"))
			{
				PubicHairHelper.CycleDensity(pawn);
			}
			else
			{
				Widgets.Label(row, label);
			}
		}

		static void DrawSexuality(Pawn pawn, Rect row)
		{
			string sexuality;

			switch (pawn.GetCompRJW().orientation)
			{
				case Orientation.Asexual:
					sexuality = "SC_Sexuality_Asexual".Translate();
					break;
				case Orientation.Bisexual:
					sexuality = "SC_Sexuality_Bisexual".Translate();
					break;
				case Orientation.Heterosexual:
					sexuality = "SC_Sexuality_Heterosexual".Translate();
					break;
				case Orientation.Homosexual:
					sexuality = "SC_Sexuality_Homosexual".Translate();
					break;
				case Orientation.LeaningHeterosexual:
					sexuality = "SC_Sexuality_LeaningHeterosexual".Translate();
					break;
				case Orientation.LeaningHomosexual:
					sexuality = "SC_Sexuality_LeaningHomosexual".Translate();
					break;
				case Orientation.MostlyHeterosexual:
					sexuality = "SC_Sexuality_MostlyHeterosexual".Translate();
					break;
				case Orientation.MostlyHomosexual:
					sexuality = "SC_Sexuality_MostlyHomosexual".Translate();
					break;
				case Orientation.Pansexual:
					sexuality = "SC_Sexuality_Pansexual".Translate();
					break;
				default:
					sexuality = "None".Translate();
					break;
			}
			
			//允许在以下情况更改小人的性取向：
			//自己的英雄、游戏开局
			if (RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.RimJobWorld &&
				((Current.ProgramState == ProgramState.Playing &&
				pawn.IsDesignatedHero() && pawn.IsHeroOwner()) ||
				Prefs.DevMode ||
				Current.ProgramState == ProgramState.Entry))

			{
				if (Widgets.ButtonText(row, "SC_Sexuality".Translate(sexuality), false))
				{
					Find.WindowStack.Add(new FloatMenu(
					//在 1.1 中配合原版性向特质的话，这里需要修
					[
						new FloatMenuOption("SC_Sexuality_Asexual".Translate(), () => Change_orientation(pawn, Orientation.Asexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_Pansexual".Translate(), () => Change_orientation(pawn, Orientation.Pansexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_Heterosexual".Translate(), () => Change_orientation(pawn, Orientation.Heterosexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_MostlyHeterosexual".Translate(), () => Change_orientation(pawn, Orientation.MostlyHeterosexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_LeaningHeterosexual".Translate(), () => Change_orientation(pawn, Orientation.LeaningHeterosexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_Bisexual".Translate(), () => Change_orientation(pawn, Orientation.Bisexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_LeaningHomosexual".Translate(), () => Change_orientation(pawn, Orientation.LeaningHomosexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_MostlyHomosexual".Translate(), () => Change_orientation(pawn, Orientation.MostlyHomosexual), MenuOptionPriority.Default),
						new FloatMenuOption("SC_Sexuality_Homosexual".Translate(), () => Change_orientation(pawn, Orientation.Homosexual), MenuOptionPriority.Default),
					]));
				}
			}
			else
			{
				Widgets.Label(row, "SC_Sexuality".Translate(sexuality));
				if (Mouse.IsOver(row))
					Widgets.DrawHighlight(row);
			}
		}

		[SyncMethod]
		static void Change_orientation(Pawn pawn, Orientation orientation)
		{
			pawn.GetCompRJW().orientation = orientation;
		}

		[SyncMethod]
		static void Change_Fertility(Pawn pawn)
		{
			BodyPartRecord genitalia = Genital_Helper.get_genitalsBPR(pawn);
			Hediff blocker = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ImpregnationBlocker"));
			Hediff enhancer = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FertilityEnhancer"));

			if (blocker != null)
			{
				pawn.health.RemoveHediff(blocker);
			}
			else if (enhancer == null)
			{
				pawn.health.AddHediff(HediffDef.Named("FertilityEnhancer"), genitalia);
			}
			else 
			{
				if (enhancer != null)
					pawn.health.RemoveHediff(enhancer);
				pawn.health.AddHediff(HediffDef.Named("ImpregnationBlocker"), genitalia);
			}
		}

		[SyncMethod]
		static void Re_sexualize(Pawn pawn)
		{
			pawn.GetCompRJW().Sexualize(true);
		}

		static void Make_button(Rect rect, Pawn pawn, List<ISexPartHediff> parts, string label)
		{
			var ogCount = parts.Count;
			parts = [.. parts.Where(part => {
				return part.Def.partTags.Contains("Resizable") || RJWSettings.DevMode || Current.ProgramState != ProgramState.Playing;
			})];
			
			if (!parts.NullOrEmpty())
			{
				if (Widgets.ButtonText(rect, "PartsEditLabel".Translate(label)))
				{
					Find.WindowStack.Add(new Dialog_Partcard(pawn, parts, label));
				}
			}
		}
		public override void DoWindowContents(Rect inRect)
		{
			bool closeFlag = false;
			soundClose = SoundDefOf.InfoCard_Close;
			closeOnClickedOutside = true;
			absorbInputAroundWindow = false;
			forcePause = true;
			preventCameraMotion = false;
			if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
			{
				closeFlag = true;
				Event.current.Use();
			}
			Rect windowRect = inRect.ContractedBy(17f);
			Rect mainRect = new Rect(windowRect.x, windowRect.y, windowRect.width, windowRect.height - 20f);
			Rect okRect = new Rect(inRect.width / 3, mainRect.yMax + 10f, inRect.width / 3f, 30f);
			SexualityCard(mainRect, pawn);
			if (Widgets.ButtonText(okRect, "CloseButton".Translate()) || closeFlag)
			{
				Close();
			}
		}
	}
}
