using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>
	/// 地面覆盖层：给施过肥的格子铺一层土褐色、给灌溉过的格子铺一层淡蓝色。
	/// 和 DBH 的 SectionLayer_FertilizerGrid 一个思路，但自己拼四边形，
	/// 一个 section 一次画完，不建 Thing、不改地形。
	/// </summary>
	[StaticConstructorOnStartup]
	public class SectionLayer_Agriculture : SectionLayer
	{
		private static readonly Material OverlayMat = new Material(MatBases.Snow);

		private static readonly Color FertilizerColor = new Color(0.52f, 0.4f, 0.18f);

		private static readonly Color IrrigationColor = new Color(0.28f, 0.5f, 0.8f);

		private const float MaxFertilizerAlpha = 0.5f;

		private const float MaxIrrigationAlpha = 0.35f;

		public SectionLayer_Agriculture(Section section) : base(section)
		{
			relevantChangeTypes = MapMeshFlagDefOf.Terrain;
		}

		public override bool Visible => HygieneSettings.Current.showAgricultureOverlay;

		private MapComponent_Agriculture Comp => MapComponent_Agriculture.Get(Map);

		public override void Regenerate()
		{
			LayerSubMesh subMesh = GetSubMesh(OverlayMat);
			subMesh.Clear(MeshParts.Verts | MeshParts.Tris | MeshParts.Colors);

			MapComponent_Agriculture comp = Comp;
			if (comp == null)
			{
				subMesh.disabled = true;
				return;
			}

			CellRect rect = section.CellRect;
			float y = AltitudeLayer.Terrain.AltitudeFor() + 0.01f;
			bool any = false;
			for (int x = rect.minX; x <= rect.maxX; x++)
			{
				for (int z = rect.minZ; z <= rect.maxZ; z++)
				{
					IntVec3 cell = new IntVec3(x, 0, z);
					float fertilizer = comp.FertilizerAt(cell);
					float irrigation = comp.IrrigationAt(cell);
					if (fertilizer <= 0f && irrigation <= 0f)
					{
						continue;
					}
					AddQuad(subMesh, x - rect.minX, z - rect.minZ, y, OverlayColor(fertilizer, irrigation));
					any = true;
				}
			}

			subMesh.disabled = !any;
			if (any)
			{
				subMesh.FinalizeMesh(MeshParts.Verts | MeshParts.Tris | MeshParts.Colors);
			}
		}

		private static Color OverlayColor(float fertilizer, float irrigation)
		{
			// 两种都有时按强度混色：肥料偏土黄、灌溉偏蓝，叠出来像湿土。
			float fertAlpha = fertilizer > 0f ? Mathf.Lerp(0.15f, MaxFertilizerAlpha, fertilizer / 100f) : 0f;
			float irrAlpha = irrigation > 0f ? Mathf.Lerp(0.1f, MaxIrrigationAlpha, irrigation / 100f) : 0f;
			if (fertAlpha > 0f && irrAlpha > 0f)
			{
				float total = fertAlpha + irrAlpha;
				return new Color(
					(FertilizerColor.r * fertAlpha + IrrigationColor.r * irrAlpha) / total,
					(FertilizerColor.g * fertAlpha + IrrigationColor.g * irrAlpha) / total,
					(FertilizerColor.b * fertAlpha + IrrigationColor.b * irrAlpha) / total,
					Mathf.Min(0.6f, total));
			}
			return fertAlpha > 0f
				? new Color(FertilizerColor.r, FertilizerColor.g, FertilizerColor.b, fertAlpha)
				: new Color(IrrigationColor.r, IrrigationColor.g, IrrigationColor.b, irrAlpha);
		}

		private static void AddQuad(LayerSubMesh subMesh, int x, int z, float y, Color color)
		{
			int baseIndex = subMesh.verts.Count;
			subMesh.verts.Add(new Vector3(x, y, z));
			subMesh.verts.Add(new Vector3(x + 1, y, z));
			subMesh.verts.Add(new Vector3(x + 1, y, z + 1));
			subMesh.verts.Add(new Vector3(x, y, z + 1));
			Color32 c = color;
			subMesh.colors.Add(c);
			subMesh.colors.Add(c);
			subMesh.colors.Add(c);
			subMesh.colors.Add(c);
			// 正反两面都画，免得剔除把背面翻掉。
			subMesh.tris.Add(baseIndex);
			subMesh.tris.Add(baseIndex + 1);
			subMesh.tris.Add(baseIndex + 2);
			subMesh.tris.Add(baseIndex);
			subMesh.tris.Add(baseIndex + 2);
			subMesh.tris.Add(baseIndex + 3);
			subMesh.tris.Add(baseIndex + 3);
			subMesh.tris.Add(baseIndex + 2);
			subMesh.tris.Add(baseIndex + 1);
			subMesh.tris.Add(baseIndex + 3);
			subMesh.tris.Add(baseIndex + 1);
			subMesh.tris.Add(baseIndex);
		}
	}
}
