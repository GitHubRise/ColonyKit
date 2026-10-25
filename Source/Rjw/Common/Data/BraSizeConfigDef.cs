#nullable disable

﻿using System.Collections.Generic;
using Verse;
using System;
using System.ComponentModel;
using System.Linq;
using Verse.Noise;

namespace rjw
{
	/// <summary>
	/// 与单个 hediffdef 无关的部位尺寸数据。
	/// </summary>
	public class BraSizeConfigDef : Def
	{
		public float bandSizeBase;
		public List<string> cupSizeLabels;

		static readonly Lazy<BraSizeConfigDef> instance = new Lazy<BraSizeConfigDef>(() => DefDatabase<BraSizeConfigDef>.AllDefs.Single());

		public static BraSizeConfigDef Instance
		{
			get
			{
				return instance.Value;
			}
		}

		public static string GetCupSizeLabel(float size)
		{
			var i = Math.Max(0, Math.Min(Instance.cupSizeLabels.Count - 1, (int)size));
			return Instance.cupSizeLabels[i];
		}
	}
	public struct BreastSize
	{
		// 使用英国尺码（基本上与美国尺码相同）
		public float bandSize;
		public float cupSize;
		public float density;
		public float volume => CalculateVolume(cupSize, bandSize); // 升
		public float weight => CalculateWeight(cupSize, bandSize, density);  // 千克
		
		public BreastSize(float cupSize, float bandSize, float density) {
			this.cupSize = cupSize;
			this.bandSize = bandSize;
			this.density = density;
		}

		// 定义胸罩尺码 -> 体积的关系
		// bandSize + 2*cupSize = a * Volume^(1/3) + b
		private static double a = 3.4;
		private static double b = 15.4;
		public static float CalculateVolume(float cupSize, float bandSize)
		{
			var totalSize = bandSize + cupSize * 2;
			var volume = Math.Pow((totalSize - b)/a, 3);
			return (float)(volume / 1000);
		}

		public static float CalculateWeight(float cupSize, float bandSize, float density)
		{
			var totalSize = bandSize + cupSize * 2;
			var volume = Math.Pow((totalSize - b)/a, 3);
			return (float)(volume / 1000) * density;
		}

		public static float CalculateCupSize(float volume, float bandSize)
		{
			var totalSize = a*Math.Pow(volume * 1000, 1/3.0) + b;
			var cupSize = (totalSize - bandSize)/2;
			return (float)cupSize;
		}

		public static float CalculateBandSize(float volume, float cupSize)
		{
			var totalSize = a*Math.Pow(volume * 1000, 1/3.0) + b;
			var bandSize = totalSize - cupSize;
			return (float)bandSize;
		}

		public string GetCupSize()
		{
			var index = (int)Math.Round(cupSize);

			if(index < 0)
			{
				return $"-";
			}

			if(index >= BraSizeConfigDef.Instance.cupSizeLabels.Count)
			{
				return $"???";
			}

			return BraSizeConfigDef.Instance.cupSizeLabels[index];
		}

		public string GetBandSize()
		{
			return $"{(int)Math.Round(bandSize)}";
		}
		
		public override string ToString()
		{
			if(cupSize < 0)
			{
				return $"-";
			}

			return $"{(int)Math.Round(bandSize)}{GetCupSize()}";
		}
	}
}
