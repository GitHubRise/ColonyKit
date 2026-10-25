using Verse;
using RimWorld;
using Multiplayer.API;
using System;
using System.Collections.Generic;

namespace rjw
{
	public sealed class CompRJW : ThingComp
	{
		/// <summary>
		/// 用于追踪生殖器和性取向的核心组件。
		/// </summary>
		public CompRJW() { }

		public Orientation orientation = Orientation.None;
		public int NextHookupTick;
		private bool BootStrapTriggered = false;
		public Need_Sex? sexNeed;
		public bool drawNude = false;
		public bool keep_hat_on = false;
		private PawnData? pawnData = null;
		public PawnData PawnData => pawnData!;
		/// <summary>
		/// <para>获取该组件所属的小人。</para>
		/// <para>如果组件的父对象以某种方式不是小人，则可能返回 null。</para>
		/// </summary>
		private Pawn? pawn;

		/// <summary>
		/// Initialize 在父对象作为 ThingWithComps 初始化之后、但作为 Pawn 初始化之前调用。
		/// 预计大多数情况下 Pawn 为 null/空。
		/// </summary>
		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			pawn = parent as Pawn;

			if (pawn == null)
			{
				ModLog.Error($"Tried to initialize {nameof(CompRJW)} for the {parent}. {parent} is not a pawn");
				return;
			}
		}

		public override void CompTickInterval(int delta)
		{
			base.CompTickInterval(delta);

			if (pawn == null) return;
			
			if (pawn.IsHashIntervalTick(1500, delta) && !pawn.health.Dead)
			// `NeedInterval` 函数每 150 tick 调用一次，但以前
			// 频率被限制为原来的 1/10。  这会让维护逻辑以
			// 相同的间隔执行。
			{
				if (sexNeed == null) 
				{
					sexNeed = pawn.needs?.TryGetNeed<Need_Sex>();
				}
				if (sexNeed != null)
				{
					DoUpkeep(pawn, sexNeed);
				}
			}
		}

		/// <summary>
		/// 执行一些原先由 <c>Need_Sex.NeedInterval</c> 处理的维护任务。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="sexNeed">性需求阈值</param>
		private void DoUpkeep(Pawn pawn, Need_Sex sexNeed)
		{
			if (pawn.Map == null) return;
			if (xxx.is_asexual(pawn)) return;

			var curLevel = sexNeed.CurLevel;

			// 若小人清醒则更新灵能。
			if (!RJWSettings.Disable_MeditationFocusDrain && pawn.Awake() && curLevel < sexNeed.thresh_frustrated())
				SexUtility.OffsetPsyfocus(pawn, -0.01f);
			//if (curLevel < sexNeed.thresh_horny())
			//	SexUtility.OffsetPsyfocus(pawn, -0.01f);
			//if (curLevel < sexNeed.thresh_frustrated() || curLevel > sexNeed.thresh_ahegao())
			//	SexUtility.OffsetPsyfocus(pawn, -0.05f);

			if (curLevel < sexNeed.thresh_horny() && (pawn.mindState.canLovinTick - Find.TickManager.TicksGame > 300))
				pawn.mindState.canLovinTick = Find.TickManager.TicksGame + 300;

			// 这些大概都可以直接删掉，因为现在已是空操作。
			// 但我不知道有没有模组在给 `xxx.bootstrap` 打补丁之类的。
			if (!BootStrapTriggered)
			{
				//--ModLog.Message("CompRJW::DoUpkeep::calling boostrap - pawn is " + xxx.get_pawnname(pawn));
				xxx.bootstrap(pawn.Map);
				BootStrapTriggered = true;
			}
			if (drawNude) {
				bool isFucking = pawn.jobs?.curDriver is JobDriver_Sex;
				if(!isFucking)
				{
					drawNude = false;
					keep_hat_on = false;
				}
			}
			
		}

		public override void PostExposeData()
		{
			base.PostExposeData();

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				if (pawn == null) return;
			}
			else if (pawn == null && parent is Pawn parentPawn)
			{
				// 加载分几个阶段进行。第一次可用时直接获取父对象即可。
				pawn = parentPawn;
			}

			Scribe_Values.Look(ref orientation, "RJW_Orientation");
			Scribe_Values.Look(ref NextHookupTick, "RJW_NextHookupTick");
			Scribe_Values.Look(ref drawNude, "RJW_drawNude");
			Scribe_Values.Look(ref keep_hat_on, "RJW_keep_hat_on");
			Scribe_Deep.Look(ref pawnData, "pawnData", [parent as Pawn]);
			//ModLog.Message("PostExposeData for " + pawn?.Name);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (pawnData == null)
				{
					// RJW 是在存档中途加入的
					Sexualize();
				}
				else if (pawn != null)
				{
					// 以某种意外方式从性向来源变更中恢复
					Orientation loaded = orientation;
					UpdateOrientation(pawn);
					if (loaded != orientation) ModLog.Warning($"Updated orientation for {xxx.get_pawnname(pawn)} from {loaded} to {orientation}");
				}

				if (pawn != null && pawn.health.Dead && drawNude)
				{
					drawNude = false;
					keep_hat_on = false;
					pawn.Drawer.renderer.SetAllGraphicsDirty();
				}
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (pawn == null)
			{
				yield break;
			}

			if (ModsConfig.RoyaltyActive)
			{
				if (pawn.jobs?.curDriver is JobDriver_Sex)
					yield return new SexGizmo(pawn) { Order = 6 };
			}

			// 群交邀请入口：不依赖 Royalty / 名人模式 / 右键菜单开关。
			// 发起者与接受者都可以邀请：双飞时选中男方（接受者）就能拉第二个女方进来。
			if (pawn.jobs?.curDriver is JobDriver_SexBaseInitiator
				|| pawn.jobs?.curDriver is JobDriver_SexBaseReciever
				|| pawn.jobs?.curDriver is JobDriver_Lovin)
			{
				yield return new Gizmo_InviteJoin(pawn) { Order = 5 };
			}

			if (RJWSettings.show_RJW_designation_box && Find.Selector.NumSelected == 1)
			{
				// 包含小人身上 rjw 标记的紧凑按钮组
				if (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony)
					yield return new RJWdesignations(pawn) { Order = 6 };
			}

			if (RJWSettings.submit_button_enabled &&
				pawn.IsColonistPlayerControlled &&
				pawn.Drafted &&
				pawn.CanChangeDesignationColonist())
			{
				if (!(pawn.kindDef.race.defName.Contains("Droid") && !AndroidsCompatibility.IsAndroid(pawn)))
					yield return new SubmitGizmo(pawn) { Order = 6 };
			}
		}

		/// <summary>
		/// 根据 Psychology 添加的金赛评分获取 <paramref name="pawn"/> 的性向
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		public static Orientation? GetPsychologyOrientation(Pawn pawn)
		{
			CompRJW? compRJW = pawn.GetCompRJW();

			if (compRJW == null)
			{
				ModLog.Error($"GetPsychologyOrientation was called for {pawn}, pawn without the CompRJW");
				return null;
			}

			try
			{
				// 软依赖：未加载 Psychology 时回退为 null。
				if (!ExternalSexuality.TryGetKinseyRating(pawn, out int kinsey))
					return null;

				if (!Genital_Helper.has_genitals(pawn) && (pawn.kindDef.race.defName.ToLower().Contains("droid") || pawn.kindDef.race.defName.ToLower().Contains("drone")))
					return Orientation.Asexual;
				else if (kinsey < 0 || kinsey > 6)
				{
					if (RJWSettings.DevMode) ModLog.Warning($"Unknown kinsey scale value {kinsey} on pawn {xxx.get_pawnname(pawn)}");
					return Orientation.Asexual;
				}
				else
					return ExternalSexuality.MapKinsey(kinsey);
			}
			catch
			{
				if (!pawn.IsAnimal())
					ModLog.Warning("GetPsychologyOrientation " + pawn?.Name + ", def: " + pawn?.def?.defName + ", kindDef: " + pawn?.kindDef?.race.defName);
			}
			return null;
		}

		/// <summary>
		/// 根据 Individuality 添加的新性取向特质获取 <paramref name="pawn"/> 的性向
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns></returns>
		public static Orientation? GetIndividualityOrientation(Pawn pawn)
		{
			CompRJW? compRJW = pawn.GetCompRJW();

			if (compRJW == null)
			{
				ModLog.Error($"GetIndividualityOrientation was called for {pawn}, pawn without the CompRJW");
				return null;
			}

			try
			{
				// 软依赖：未加载 [SYR] Individuality 时回退为 null。
				if (!ExternalSexuality.TryGetIndividualitySexuality(pawn, out string individualitySexuality))
					return null;

				if (individualitySexuality == "Asexual")
					return Orientation.Asexual;
				else if (!Genital_Helper.has_genitals(pawn) && (pawn.kindDef.race.defName.ToLower().Contains("droid") || pawn.kindDef.race.defName.ToLower().Contains("drone")))
					return Orientation.Asexual;
				else if (individualitySexuality == "Straight")
					return Orientation.Heterosexual;
				else if (individualitySexuality == "Bisexual")
					return Orientation.Bisexual;
				else if (individualitySexuality == "Gay")
					return Orientation.Homosexual;
				else
				{
					if (RJWSettings.DevMode) ModLog.Warning($"Pawn {xxx.get_pawnname(pawn)} had an odd sexuality: {individualitySexuality}");
					return Orientation.Asexual;
				}
			}
			catch
			{
				if (!pawn.IsAnimal())
					ModLog.Warning("GetIndividualityOrientation " + pawn?.Name + ", def: " + pawn?.def?.defName + ", kindDef: " + pawn?.kindDef?.race.defName);
			}
			return null;
		}

		/// <summary>
		/// 根据原版特质获取 <paramref name="pawn"/> 的性向
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="fallback">当不存在相关特质时返回的性向</param>
		/// <returns></returns>
		public static Orientation? GetVanillaOrientation(Pawn pawn, Orientation? fallback = Orientation.Heterosexual)
		{
			CompRJW? compRJW = pawn.GetCompRJW();

			if (compRJW == null)
			{
				ModLog.Error($"GetVanillaOrientation was called for {pawn}, pawn without the CompRJW");
				return null;
			}

			if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
				return Orientation.Asexual;
			else if (!Genital_Helper.has_genitals(pawn) && (pawn.kindDef.race.defName.ToLower().Contains("droid") || pawn.kindDef.race.defName.ToLower().Contains("drone")))
				return Orientation.Asexual;
			else if (pawn.story.traits.HasTrait(TraitDefOf.Gay))
				return Orientation.Homosexual;
			else if (pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
				return Orientation.Bisexual;
			return fallback;
		}

		/// <summary>
		/// 重新计算并应用 <paramref name="pawn"/> 的性取向，依据选定的性取向分布来源（<see cref="RJWPreferenceSettings.sexuality_distribution"/>）。
		/// </summary>
		/// <remarks>
		/// 通常在 <paramref name="pawn"/> 的特质发生变化时调用（例如小人获得 Gay 特质时）。
		/// </remarks>
		/// <param name="pawn">应更新性向的小人。</param>
		public static void UpdateOrientation(Pawn pawn)
		{
			if (pawn.IsAnimal())
			{
				// 动物另行处理
				return;
			}

			if (!xxx.has_traits(pawn))
			{
				return;
			}

			CompRJW? compRJW = pawn.GetCompRJW();
			if (compRJW == null)
			{
				ModLog.Error($"UpdateOrientation was called for {pawn}, pawn without the CompRJW");
				return;
			}

			if (compRJW.pawnData?.sexualized == false)
			{
				if (RJWSettings.DevMode) ModLog.Warning($"UpdateOrientation was called for unsexualized pawn {pawn}");
				return;
			}

			Orientation? orientation = null;
			switch (RJWPreferenceSettings.sexuality_distribution)
			{
				case RJWPreferenceSettings.Rjw_sexuality.Vanilla:
					orientation = GetVanillaOrientation(pawn);
					break;
				case RJWPreferenceSettings.Rjw_sexuality.Psychology:
					orientation = GetPsychologyOrientation(pawn);
					break;
				case RJWPreferenceSettings.Rjw_sexuality.SYRIndividuality:
					orientation = GetIndividualityOrientation(pawn);
					break;
				case RJWPreferenceSettings.Rjw_sexuality.RimPsyche:
					//Rimpsyche 无论如何都会预补丁 UpdateOrientation。
					break;
				case RJWPreferenceSettings.Rjw_sexuality.RimJobWorld:
					// 先获取特质决定的性取向
					orientation = GetVanillaOrientation(pawn, fallback: null);
					// 如果性向尚未设定（例如小人刚创建时）且未找到相关特质，则随机抽取
					if (compRJW.orientation == Orientation.None && orientation == null)
					{
						if (RJWSettings.DevMode) ModLog.Message($"[RJWdist: {xxx.get_pawnname(pawn)}] Rolling new orientation");
						orientation = compRJW.RollOrientation(pawn);
					}
					// 如果性向之前已设定且与特质一致，则保留
					else if (compRJW.orientation != Orientation.None && orientation != null && orientation == compRJW.orientation)
					{
						if (RJWSettings.DevMode) ModLog.Message($"[RJWdist: {xxx.get_pawnname(pawn)}] Preserving orientation which agrees with traits");
						return;
					}
					// 如果性向之前已设定且没有相关特质，则保留
					// 否则，对没有相关特质的小人，每次调用都会重新随机
					else if (compRJW.orientation != Orientation.None && orientation == null)
					{
						if (RJWSettings.DevMode) ModLog.Message($"[RJWdist: {xxx.get_pawnname(pawn)}] No relevant traits and orientation already set, skipping orientation update");
						return;
					}
					// 如果性向之前已设定但特质与之不一致，则更新为原版性向
					else if (compRJW.orientation != Orientation.None && orientation != null && orientation != compRJW.orientation)
					{
						if (RJWSettings.DevMode) ModLog.Message($"[RJWdist: {xxx.get_pawnname(pawn)}] Traits disagree with orientation, updating for consistency");
					}
					break;
				default:
					if (RJWSettings.DevMode) ModLog.Error($"Unknown sexuality source. Orientation was not updated.");
					break;
			}
			if (orientation == null)
			{
				if (RJWSettings.DevMode) ModLog.Error($"Unable to update orientation for {xxx.get_pawnname(pawn)}");
				return;
			}
			compRJW.orientation = (Orientation)orientation;
		}

		/// <summary>
		/// 添加生殖器和性向的主要方法。
		/// 选定的性向基于所选的性向来源（<see cref="RJWPreferenceSettings.sexuality_distribution"/>）
		/// </summary>
		/// <param name="reroll">重置 <see cref="pawn"/> 的性向并强制重新随机。</param>
		public void Sexualize(bool reroll = false)
		{
			if (pawn == null)
			{
				ModLog.Error($"Tried to sexualize {parent}, it is not a pawn");
				return;
			}

			// 不能放在 Initialize 或 PostPostMake 中，因为那时小人尚未完全初始化
			pawnData ??= new PawnData(pawn);
			pawnData.Initialize();

			if (pawnData.sexualized && !reroll)
			{
				return;
			}

			if (reroll)
			{
				orientation = Orientation.None;
			}
			else if (orientation != Orientation.None)
			{
				return;
			}

			//ModLog.Message("Sexualizing pawn " + pawn?.Name + ", def: " + pawn?.def?.defName);

			if (!reroll)
				Sexualizer.sexualize_pawn(pawn);

			pawnData.sexualized = true;

			// 新小人生成时顺手给阴毛密度；已经有则跳过。
			PubicHairHelper.Ensure(pawn);

			if (xxx.is_animal(pawn))
			{
				orientation = RollAnimalOrientation(pawn);
				//ModLog.Message($"Orientation for {pawn} is {orientation}, has_genitals = {Genital_Helper.has_genitals(pawn)}");
			}
			else if (xxx.has_traits(pawn) && Genital_Helper.has_genitals(pawn) && !(pawn.kindDef.race.defName.ToLower().Contains("droid") && !AndroidsCompatibility.IsAndroid(pawn)))
			{
				UpdateOrientation(pawn);
			}
			else if ((pawn.kindDef.race.defName.ToLower().Contains("droid") && !AndroidsCompatibility.IsAndroid(pawn)) || !Genital_Helper.has_genitals(pawn))
			{
				// 没有生殖器的机器人会被设为无性恋。
				// 如果玩家之后给机器人添加了生殖器，其“性取向”会被重新随机。
				orientation = Orientation.Asexual;
			}
		}

		/// <summary>
		/// 为 <paramref name="pawn"/> 随机抽取性取向。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>该性向</returns>
		[SyncMethod]
		public Orientation RollOrientation(Pawn pawn)
		{
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			float random = Rand.Range(0f, 1f);
			float checkpoint = RJWPreferenceSettings.asexual_ratio / RJWPreferenceSettings.GetRatioTotal();

			float checkpoint_pan = checkpoint + (RJWPreferenceSettings.pansexual_ratio / RJWPreferenceSettings.GetRatioTotal());
			float checkpoint_het = checkpoint_pan + (RJWPreferenceSettings.heterosexual_ratio / RJWPreferenceSettings.GetRatioTotal());
			float checkpoint_bi = checkpoint_het + (RJWPreferenceSettings.bisexual_ratio / RJWPreferenceSettings.GetRatioTotal());
			float checkpoint_gay = checkpoint_bi + (RJWPreferenceSettings.homosexual_ratio / RJWPreferenceSettings.GetRatioTotal());

			if (random < checkpoint || !Genital_Helper.has_genitals(pawn))
				return Orientation.Asexual;
			else if (random < checkpoint_pan)
				return Orientation.Pansexual;
			else if (random < checkpoint_het)
				return Orientation.Heterosexual;
			else if (random < checkpoint_het + ((checkpoint_bi - checkpoint_het) * 0.33f))
				return Orientation.MostlyHeterosexual;
			else if (random < checkpoint_het + ((checkpoint_bi - checkpoint_het) * 0.66f))
				return Orientation.LeaningHeterosexual;
			else if (random < checkpoint_bi)
				return Orientation.Bisexual;
			else if (random < checkpoint_bi + ((checkpoint_gay - checkpoint_bi) * 0.33f))
				return Orientation.LeaningHomosexual;
			else if (random < checkpoint_bi + ((checkpoint_gay - checkpoint_bi) * 0.66f))
				return Orientation.MostlyHomosexual;
			else
				return Orientation.Homosexual;
		}

		/// <summary>
		/// 为 <paramref name="pawn"/> 随机抽取性取向。
		/// </summary>
		/// <remarks>
		/// 这是用于动物（<paramref name="pawn"/>）的简化系统，其中大多数为异性恋。
		/// 不想因为加入太多同性恋动物而干扰玩家的繁殖计划。
		/// </remarks>
		/// <param name="pawn">要检查的动物。</param>
		/// <returns>该性向</returns>
		[SyncMethod]
		public Orientation RollAnimalOrientation(Pawn pawn)
		{
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			float random = Rand.Range(0f, 1f);

			if (random < 0.03f || !Genital_Helper.has_genitals(pawn))
				return Orientation.Asexual;
			else if (random < 0.85f)
				return Orientation.Heterosexual;
			else if (random < 0.96f)
				return Orientation.Bisexual;
			else
				return Orientation.Homosexual;
		}
	}
}
