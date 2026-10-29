using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 专用的身体渲染节点。
	///
	/// 原版 Misc 渲染树的 Body 节点在机械体上只负责把 PawnKindLifeStage 的
	/// Graphic_Multi 塞进绘制请求；一旦绘制请求被缓存，身体方向就可能停留在
	/// 旧的一侧。这里把 Body 节点独立出来，由 MkVBodyWorker 在真正取材质时
	/// 直接按 pawn.Rotation 选择 MatNorth / MatEast / MatSouth / MatWest。
	///
	/// 这样做的目的不是“再补一层 Harmony”，而是彻底绕开原版 Misc Body 节点
	/// 对 facing 的处理，避免以后再出现“日志已经是 West，画面还是 East”。
	/// </summary>
	public class PawnRenderNode_MkVBody : PawnRenderNode
	{
		public PawnRenderNode_MkVBody(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
			: base(pawn, props, tree)
		{
		}

		/// <summary>
		/// 身体 Graphic 完全由 MkVSkinUtility 提供：
		/// - 读取设置里的贴图路径、绘制大小、宽高比例
		/// - 返回四向 Graphic_Multi
		/// - 路径无效时返回 null，再由 worker 回退到原版逻辑
		/// </summary>
		public override Graphic GraphicFor(Pawn pawn)
		{
			// 第一优先级：MkVSkinUtility 根据设置生成的四向 Graphic_Multi。
			// 它会处理贴图路径、drawSize、宽高比例，并保证四张图都来自同一个路径。
			//
			// 第二优先级：如果设置路径无效，或者该 Pawn 不是 Mk-V，则调用 base，
			// 让原版按 PawnKindLifeStage.bodyGraphicData 取默认贴图，而不是把
			// 身体直接渲染成透明/缺失。
			//
			// 注意：这里的 Graphic 只负责“使用哪组四向图”；具体某一次绘制选
			// MatNorth/East/South/West，由下面的 Worker.GetMaterial 决定。
			return MkVSkinUtility.GraphicFor(pawn) ?? base.GraphicFor(pawn);
		}
	}

	/// <summary>
	/// Mk-V 身体材质 worker。
	///
	/// 关键点：材质选择只看 pawn.Rotation，不看 PawnDrawParms.facing。
	/// 原因是 Misc 渲染树在站立/躺下/缓存重建时可能给出不同的 parms.facing，
	/// 但 pawn.Rotation 是移动系统实际维护的权威方向。
	/// </summary>
	public class PawnRenderNodeWorker_MkVBody : PawnRenderNodeWorker
	{
		protected override Material GetMaterial(PawnRenderNode node, PawnDrawParms parms)
		{
			// 只有同时满足以下三个条件才接管材质：
			// 1. parms.pawn 不为空，避免在尸体/特殊绘制阶段访问空 Pawn；
			// 2. 当前 Pawn 确实是 Mk-V，绝不误伤 Mk-I ~ Mk-IV 或普通机械体；
			// 3. Body 节点的 PrimaryGraphic 是 Graphic_Multi，也就是确实存在
			//    north/east/south/west 四份材质。若为 Graphic_Single 或 null，
			//    强行取 MatWest 会空引用，所以必须走 base 回退。
			if (parms.pawn != null
				&& NannyBotUtility.IsMkV(parms.pawn)
				&& node?.PrimaryGraphic is Graphic_Multi graphic)
			{
				// Rot4.AsInt 的原版约定：
				//   0 = North（北/朝上）
				//   1 = East （东/朝右）
				//   2 = South（南/朝下）
				//   3 = West （西/朝左）
				//
				// 这里刻意不使用 parms.facing：Misc 渲染树在站立、躺下、
				// 缓存重建和远程武器瞄准时可能传入不同的 facing，而
				// pawn.Rotation 才是移动系统维护的权威方向。
				switch (parms.pawn.Rotation.AsInt)
				{
					case 0:
						// 朝北：使用背面图。
						return graphic.MatNorth;
					case 1:
						// 朝东：使用朝右的侧面图。
						return graphic.MatEast;
					case 2:
						// 朝南：使用正面图。
						return graphic.MatSouth;
					case 3:
						// 朝西：使用朝左的侧面图。
						return graphic.MatWest;
				}
			}
			// 回退路径：
			// - 非 Mk-V、非 Body 节点、Graphic 不是 Graphic_Multi；
			// - 或出现未来版本新增的非法 Rot4 值。
			// 交给原版 Worker，避免自定义规则把其他 Pawn 的渲染弄坏。
			return base.GetMaterial(node, parms);
		}
	}
}
