#nullable disable

using Verse;
using RimWorld;
using System.Collections.Generic;

namespace rjw
{
	public class PartSizeConfigDef : Def
	{
			public bool bodysizescale = false; // 根据初始所属种族的体型重新缩放部件尺寸
			
			/// <summary>
			/// 人类标准值为 1.0。为 null 则不显示权重。
			/// </summary>
			public float? density = null;

			public List<float> lengths;
			public List<float> girths;
			public List<float> cupSizes;
	}
}
