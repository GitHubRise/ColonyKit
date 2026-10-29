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
			if (parms.pawn != null
				&& NannyBotUtility.IsMkV(parms.pawn)
				&& node?.PrimaryGraphic is Graphic_Multi graphic)
			{
				switch (parms.pawn.Rotation.AsInt)
				{
					case 0:
						return graphic.MatNorth;
					case 1:
						return graphic.MatEast;
					case 2:
						return graphic.MatSouth;
					case 3:
						return graphic.MatWest;
				}
			}
			return base.GetMaterial(node, parms);
		}
	}
}
