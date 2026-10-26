using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	/// <summary>
	/// 铺设管道时把已经建好的同类型管网画成半透明铺地覆盖层。
	/// </summary>
	/// <remarks>
	/// 原版 <c>Designator_Place</c> 只对 <c>rotatable</c> 的建筑画旋转控件，水管本身不需要旋转；
	/// 这里只做“看得见”的部分：选中水管/污水管的建造设计器时，把同模式的已安装管件
	/// 按 4×4 连接图集画在 <see cref="AltitudeLayer.MetaOverlays"/>，颜色由子类决定。
	/// </remarks>
	internal abstract class SectionLayer_PipeOverlay : SectionLayer
	{
		private const string PipeAtlasPath = "Things/Building/Hygiene/CK_WaterPipe_Atlas";

		private readonly PipeType mode;
		private readonly Material material;

		protected SectionLayer_PipeOverlay(Section section, PipeType mode, Color color)
			: base(section)
		{
			this.mode = mode;
			relevantChangeTypes = MapMeshFlagDefOf.Buildings;
			material = MaterialPool.MatFrom(PipeAtlasPath, ShaderDatabase.MetaOverlay, color);
		}

		public override void DrawLayer()
		{
			// 只有正在放同模式管道时才显示；网格本身随建筑变化重建，避免选中设计器时还没有缓存。
			if (!ShouldDraw())
			{
				return;
			}
			base.DrawLayer();
		}

		public override void Regenerate()
		{
			ClearSubMeshes(MeshParts.All);

			// 整张图还没有管道时直接跳过：地图加载会重建所有 section，没必要逐个扫格。
			HygienePipeMapComp pipes = Map.GetComponent<HygienePipeMapComp>();
			if (pipes == null || pipes.cachedPipes.Count == 0)
			{
				return;
			}

			foreach (IntVec3 cell in section.CellRect)
			{
				if (!HasPipeAt(cell))
				{
					continue;
				}
				Material subMaterial = MaterialAtlasPool.SubMaterialFromAtlas(material, LinkSetAt(cell));
				Printer_Plane.PrintPlane(
					this,
					cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays),
					Vector2.one,
					subMaterial);
			}

			FinalizeMesh(MeshParts.All);
		}

		/// <summary>当前选中的建造设计器是不是这种管道。</summary>
		private bool ShouldDraw()
		{
			return PipeDesignators.IsPlacing(mode);
		}

		/// <summary>与 <see cref="Graphic_Linked"/> 同样的位序：北 1、东 2、南 4、西 8。</summary>
		private LinkDirections LinkSetAt(IntVec3 cell)
		{
			int linkSet = 0;
			int bit = 1;
			for (int i = 0; i < 4; i++)
			{
				if (HasPipeAt(cell + GenAdj.CardinalDirections[i]))
				{
					linkSet += bit;
				}
				bit *= 2;
			}
			return (LinkDirections)linkSet;
		}

		private bool HasPipeAt(IntVec3 cell)
		{
			if (!cell.InBounds(Map))
			{
				return false;
			}
			List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
			for (int i = 0; i < things.Count; i++)
			{
				CompPipe pipe = things[i].TryGetComp<CompPipe>();
				if (pipe != null && pipe.mode == mode && !pipe.closed)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>水管（蓝色）覆盖层。</summary>
	internal class SectionLayer_WaterPipeOverlay : SectionLayer_PipeOverlay
	{
		public SectionLayer_WaterPipeOverlay(Section section)
			: base(section, PipeType.Water, new Color(0.32f, 0.62f, 1f, 0.85f))
		{
		}
	}

	/// <summary>污水管（青绿色，与水管区分）覆盖层。</summary>
	internal class SectionLayer_SewagePipeOverlay : SectionLayer_PipeOverlay
	{
		public SectionLayer_SewagePipeOverlay(Section section)
			: base(section, PipeType.Sewage, new Color(0.35f, 0.85f, 0.55f, 0.85f))
		{
		}
	}

	/// <summary>
	/// 「当前选中的建造设计器会不会放某种管道」：管道覆盖层和水箱水位条共用。
	/// </summary>
	internal static class PipeDesignators
	{
		/// <param name="mode">null 表示任意管道模式（水/污水/空气/供暖）。</param>
		public static bool IsPlacing(PipeType? mode = null)
		{
			if (!(Find.DesignatorManager?.SelectedDesignator is Designator_Build designator))
			{
				return false;
			}
			if (!(designator.PlacingDef is ThingDef def) || def.comps == null)
			{
				return false;
			}
			// 不用 GetCompProperties<T>()：它会 Alloc，而这里每个可见 section 每帧都会问一次。
			for (int i = 0; i < def.comps.Count; i++)
			{
				if (def.comps[i] is CompProperties_Pipe props && (mode == null || props.mode == mode.Value))
				{
					return true;
				}
			}
			return false;
		}
	}
}
