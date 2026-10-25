#nullable disable

using System.Collections.Generic;
using Verse;
using UnityEngine;
using rjw.MainTab;

namespace rjw
{
	/// <summary>
	/// 处理为 GUI 创建 RJWdesignations 按钮组
	/// </summary>
	public class RJWdesignations : Command
	{
		//实际上是一组四个按钮，但我希望它们小一些，所以基本绕过了原版按钮系统
	
		private const float ContentPadding = 5f;
		private const float IconSize = 32f;
		private const float IconGap = 1f;

		private readonly Pawn parent;
		private Rect gizmoRect;
		/// <summary>
		/// 这里应记录最后按下的伪按钮。它在伪按钮回调中设置。 
		/// 随后由游戏执行所归属按钮的回调，并用该字段判断究竟按下的是哪个按钮
		/// 回调会在检测按钮点击的循环之后立即由 Tynan 的代码调用， 
		/// 因此它只会在应当调用的时候调用（不会有无关事件）。 
		/// 需要事件处理这套东西，才能把设置应用到所有选中的小人。
		/// </summary>
		private static SubIcon lastClicked;

		static readonly List<SubIcon> subIcons = new List<SubIcon> {
			new Comfort(),
			//new Service(),
			//new BreederHuman(),
			new BreedingHuman(),
			new BreedingAnimal(),
			new Breeder(),
			//new Milking(),
			new Hero(),
			new HelpImStupid()
		};

		public RJWdesignations(Pawn pawn)
		{
			parent = pawn;
			defaultLabel = "RJWdesignations";
			defaultDesc = "RJWdesignations";
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, 75f, 75f);
			gizmoRect = rect.ContractedBy(ContentPadding);
			Widgets.DrawWindowBackground(rect);

			//ModLog.Message("RJWgizmo");
			foreach (SubIcon icon in subIcons)
			{
				if (DrawSubIcon(icon))
				{
					lastClicked = icon;
					icon.state = icon.applied(parent);
					return new GizmoResult(GizmoState.Interacted, Event.current);
				}
			}
			return new GizmoResult(GizmoState.Clear);
		}

		//这里和下面那堆类本是为了不用在 GizmoOnGUI 里写四段重复代码的快捷方式
		private bool DrawSubIcon(SubIcon icon)
		{
			if (!icon.applicable(parent)) { return false; }
			//ModLog.Message("sub gizmo");
			Rect iconRect = new Rect(gizmoRect.x + icon.offset.x, gizmoRect.y + icon.offset.y, IconSize, IconSize);
			TooltipHandler.TipRegion(iconRect, icon.desc(parent).Translate());
			bool applied = icon.applied(parent);
			Texture2D texture = applied ? icon.cancel : icon.texture(parent);
			GUI.DrawTexture(iconRect, texture);
			//GUI.color = Color.white;

			return Widgets.ButtonInvisible(iconRect, false);
		}

		public override void ProcessInput(Event ev)
		{
			SubIcon icon = lastClicked;
			if (icon.state)
				icon.unapply(parent);
			else
				icon.apply(parent);
		}

		[StaticConstructorOnStartup]//这对贴图是必需的
		public abstract class SubIcon
		{
			public abstract Texture2D texture(Pawn pawn);
			public abstract Vector2 offset { get; }
			public abstract string desc(Pawn pawn);
			public abstract bool applicable(Pawn pawn);
			public abstract bool applied(Pawn pawn);
			public abstract void apply(Pawn pawn);
			public abstract void unapply(Pawn pawn);

			static readonly Texture2D cancellText = ContentFinder<Texture2D>.Get("UI/Commands/cancel");
			public virtual Texture2D cancel => cancellText;

			public bool state;
		}
		[StaticConstructorOnStartup]
		public class Comfort : SubIcon
		{
			//泄欲强暴
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_off");
			static readonly Texture2D iconRefuse = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_Refuse");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/ComfortPrisoner_on");
			public override Texture2D texture(Pawn pawn) => (pawn.CanDesignateComfort() || pawn.IsDesignatedComfort()) && xxx.is_human(pawn) ? iconAccept : iconRefuse;
			public override Texture2D cancel { get; } = iconCancel;

			static readonly Vector2 posComf = new Vector2(IconGap + IconSize, 0);
			public override Vector2 offset => posComf;

			public override string desc(Pawn pawn) => pawn.CanDesignateComfort() ? "ForComfortDesc" : 
														!pawn.CanChangeDesignationPrisoner() ? "ForHeroRefuse2Desc" : 
														!pawn.CanChangeDesignationColonist() ? "ForHeroRefuse1Desc" : "ForComfortRefuseDesc";

			public override bool applicable(Pawn pawn) => RJWSettings.rape_enabled && xxx.is_human(pawn);
			public override bool applied(Pawn pawn) => pawn.IsDesignatedComfort();
			public override void apply(Pawn pawn) => pawn.ToggleComfort();
			public override void unapply(Pawn pawn) => pawn.ToggleComfort();
		}
		[StaticConstructorOnStartup]
		public class Service : SubIcon
		{
			//卖淫
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/Service_off");
			static readonly Texture2D iconRefuse = ContentFinder<Texture2D>.Get("UI/Commands/Service_Refuse");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/Service_on");
			public override Texture2D texture(Pawn pawn) => (pawn.CanDesignateService() || pawn.IsDesignatedService()) && xxx.is_human(pawn) ? iconAccept : iconRefuse;
			public override Texture2D cancel { get; } = iconCancel;

			public override Vector2 offset { get; } = new Vector2(0, 0);

			public override string desc(Pawn pawn) => pawn.CanDesignateService() ? "ForServiceDesc" :
														!pawn.CanChangeDesignationPrisoner() ? "ForHeroRefuse2Desc" : 
														!pawn.CanChangeDesignationColonist() ? "ForHeroRefuse1Desc" : "ForServiceRefuseDesc";

			public override bool applicable(Pawn pawn) => xxx.is_human(pawn);
			public override bool applied(Pawn pawn) => pawn.IsDesignatedService() && xxx.is_human(pawn);
			public override void apply(Pawn pawn) => pawn.ToggleService();
			public override void unapply(Pawn pawn) => pawn.ToggleService();
		}
		[StaticConstructorOnStartup]
		public class BreedingHuman : SubIcon
		{
			//人形配种
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_off");
			static readonly Texture2D iconRefuse = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_Refuse");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Pawn_on");
			public override Texture2D texture(Pawn pawn) => (pawn.CanDesignateBreeding() || pawn.IsDesignatedBreeding()) && xxx.is_human(pawn) ? iconAccept : iconRefuse;
			public override Texture2D cancel { get; } = iconCancel;

			public override Vector2 offset { get; } = new Vector2(0, IconSize + IconGap);

			public override string desc(Pawn pawn) => pawn.CanDesignateBreeding() && xxx.is_human(pawn) ? "ForBreedingDesc" :
														!pawn.CanChangeDesignationPrisoner() ? "ForHeroRefuse2Desc" : 
														!pawn.CanChangeDesignationColonist() ? "ForHeroRefuse1Desc" : "ForBreedingRefuseDesc";

			public override bool applicable(Pawn pawn) =>  RJWSettings.bestiality_enabled && xxx.is_human(pawn);
			public override bool applied(Pawn pawn) => pawn.IsDesignatedBreeding() && xxx.is_human(pawn);
			public override void apply(Pawn pawn) => pawn.ToggleBreeding();
			public override void unapply(Pawn pawn) => pawn.ToggleBreeding();
		}

		[StaticConstructorOnStartup]
		public class BreedingAnimal : SubIcon
		{
			//动物配种
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Animal_off");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/Breeding_Animal_on");
			public override Texture2D texture(Pawn pawn) => iconAccept;
			public override Texture2D cancel { get; } = iconCancel;

			public override Vector2 offset { get; } = new Vector2(0, IconSize + IconGap);

			public override string desc(Pawn pawn) => !pawn.CanChangeDesignationPrisoner() ? "ForHeroRefuse2Desc" : "ForBreedingDesc";

			public override bool applicable(Pawn pawn) => (pawn.CanDesignateBreeding() || pawn.IsDesignatedBreeding()) && xxx.is_animal(pawn);
			public override bool applied(Pawn pawn) => pawn.IsDesignatedBreeding() && xxx.is_animal(pawn);
			public override void apply(Pawn pawn) => pawn.ToggleBreeding();
			public override void unapply(Pawn pawn) => pawn.ToggleBreeding();
		}
		[StaticConstructorOnStartup]
		public class Breeder : SubIcon
		{
			//配种者（插入方）动物
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/Breeder_Animal_off");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/Breeder_Animal_on");
			public override Texture2D texture(Pawn pawn) => iconAccept;
			public override Texture2D cancel { get; } = iconCancel;

			public override Vector2 offset { get; } = new Vector2(0, 0);

			public override string desc(Pawn pawn) => !pawn.CanChangeDesignationPrisoner() ? "ForHeroRefuse2Desc" : "ForBreedingAnimalDesc";

			public override bool applicable(Pawn pawn) => pawn.CanDesignateBreedingAnimal() || pawn.IsDesignatedBreedingAnimal();
			public override bool applied(Pawn pawn) => pawn.IsDesignatedBreedingAnimal();
			public override void apply(Pawn pawn) => pawn.ToggleBreedingAnimal();
			public override void unapply(Pawn pawn) => pawn.ToggleBreedingAnimal();
		}
		[StaticConstructorOnStartup]
		public class Milking : SubIcon
		{
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/Milking_off");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/Milking_on");
			public override Texture2D texture(Pawn pawn) => iconAccept;
			public override Texture2D cancel { get; } = iconCancel;

			public override Vector2 offset { get; } = new Vector2(IconGap + IconSize, IconSize + IconGap);

			public override string desc(Pawn pawn) => !pawn.CanChangeDesignationPrisoner() ? "ForHeroRefuse2Desc" : "ForMilkingDesc";

			public override bool applicable(Pawn pawn) => pawn.CanDesignateMilking() || pawn.IsDesignatedMilking();
			public override bool applied(Pawn pawn) => pawn.IsDesignatedMilking();
			public override void apply(Pawn pawn) => pawn.ToggleMilking();
			public override void unapply(Pawn pawn) => pawn.ToggleMilking();
		}
		[StaticConstructorOnStartup]
		public class Hero : SubIcon
		{
			static readonly Texture2D iconAccept = ContentFinder<Texture2D>.Get("UI/Commands/Hero_off");
			static readonly Texture2D iconCancel = ContentFinder<Texture2D>.Get("UI/Commands/Hero_on");
			public override Texture2D texture(Pawn pawn) => iconAccept;
			public override Texture2D cancel { get; } = iconCancel;

			public override Vector2 offset { get; } = new Vector2(IconGap + IconSize, IconSize + IconGap);

			//public override string desc(Pawn pawn) => pawn.CanDesignateHero() || (pawn.IsDesignatedHero() && pawn.IsHeroOwner()) ? "ForHeroDesc" : "Hero of " + pawn.GetPawnData().HeroOwner;
			public override string desc(Pawn pawn) => pawn.CanDesignateHero() ? "ForHeroDesc" :
														pawn.IsDesignatedHero() ? "ForHeroIsHero" : "ForHeroDesc";

			public override bool applicable(Pawn pawn) => pawn.CanDesignateHero() || pawn.IsDesignatedHero();
			public override bool applied(Pawn pawn) => pawn.IsDesignatedHero();
			public override void apply(Pawn pawn) => Find.WindowStack.Add(new Dialog_DesignateHero(pawn));
			public override void unapply(Pawn pawn) => pawn.ToggleHero(); // 基本上是个空操作？
		}
		[StaticConstructorOnStartup]
		public class HelpImStupid : SubIcon
		{
			// 当没有其他图标显示时显示的问号；提示去查看模组设置
			static readonly Texture2D myicon = ContentFinder<Texture2D>.Get("UI/Commands/HelpImStupid");
			public override Texture2D texture(Pawn pawn) => myicon;
			public override Texture2D cancel { get; } = myicon;

			static readonly Vector2 posComf = new Vector2(IconGap + IconSize, 0);
			public override Vector2 offset => posComf;

			public override string desc(Pawn pawn) => "GoToModSettings";

			public override bool applicable(Pawn pawn) => !(RJWSettings.rape_enabled && xxx.is_human(pawn)) && // 泄欲
														!(pawn.CanDesignateHero() || pawn.IsDesignatedHero()) && // 主角
														//!(pawn.CanDesignateMilking() || pawn.IsDesignatedMilking()) && // 产乳
														!(pawn.CanDesignateBreedingAnimal() || pawn.IsDesignatedBreedingAnimal()) && // 配种者
														!((pawn.CanDesignateBreeding() || pawn.IsDesignatedBreeding()) && xxx.is_animal(pawn)) && // 动物配种
														!(RJWSettings.bestiality_enabled && xxx.is_human(pawn));// && // 人形配种
														//!(pawn.IsDesignatedService() && xxx.is_human(pawn)); // 服务

			public override bool applied(Pawn pawn) => true;
			public override void apply(Pawn pawn) { }
			public override void unapply(Pawn pawn) { }
		}
	}
}
