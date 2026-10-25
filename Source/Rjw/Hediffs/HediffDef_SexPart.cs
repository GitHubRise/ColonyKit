#nullable disable

﻿using System.Linq;
using Verse;
using RimWorld;
using System.Text;
using Multiplayer.API;
using UnityEngine;
using System.Collections.Generic;
using rjw.Modules.Genitals.Enums;
using RimWorld.QuestGen;
using System;

namespace rjw
{
//TODO：弄清这东西怎么运作，并把卵移到组件中

	[StaticConstructorOnStartup]
	public class HediffDef_SexPart : HediffDef
	{
		public const float BaseFluidAmount = 20f;
		private static readonly FloatRange FluidAmountVariation = new(.75f, 1.25f);

		public SexFluidDef fluid;					//精液/奶 - 虫胶/蜂蜜 等
		public float fluidMultiplier = 1;           //奶/射精/湿润度的数量
		public bool produceFluidOnOrgasm;
		public string defaultBodyPart = "";			//在与角色创建或其他模组搞乱之后，把此部件移到的身体部位
		public List<string> defaultBodyPartList;    //在与角色创建或其他模组搞乱之后，把此部件移到的身体部位列表

		public float sizeMultiplier = 1f;

		public PartSizeConfigDef sizeProfile;
		public List<string> partTags = new();
		public GenitalFamily genitalFamily;
		public List<GenitalTag> genitalTags = new();

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string err in base.ConfigErrors())
			{
				yield return err;
			}
			if (sizeProfile == null && genitalFamily != GenitalFamily.Undefined)
			{
				yield return $"Missing required <{nameof(sizeProfile)}> field. Pick a default from "
				+ "<RJW Defs folder>/ConfigDefs/PartSizeConfigs.xml.";
			}
		}

		[SyncMethod]
		public float GetRandomPartFluidMultiplier()
		{
			return FluidAmountVariation.RandomInRange;
		}

		public float GetFluidAmount(float totalFluidMultiplier)
		{
			return BaseFluidAmount * totalFluidMultiplier;
		}

		public float GetFluidMultiplier(float partSeverity, float partFluidMultipler, float pawnBodySize, float pawnAge=40)
		{
			// 普通人类应在 ~0.5 - 2 之间
			// 原版最大动物及其最大部件，~8

			var m = this.fluidMultiplier; // 部件的基础倍率（阴道/乳房 等）
			m *= Math.Min(50 / Math.Max(1, pawnAge), 1.0f); // 年龄 50 岁以上时降低
			m *= pawnBodySize; // 生物体型。通常取种族 baseBodySize，因为严重度已按体型缩放。

			// 部件尺寸。高于平均值则增大，低于平均值则减小
			// (0 -> 0.5), (0.5 -> 1), (1 -> 2), (2 -> 4)
			m *= Math.Max(1f, partSeverity * 2f); // 高于平均值，不设上限
			m *= Math.Min(1f, partSeverity + 0.5f); // 低于平均值，上限为 0.5

			m *= partFluidMultipler; // 部件的永久倍率，包含种族部件倍率。
			return m;
		}

		[SyncMethod]
		public float GetRandomSize()
		{
			float sample = Rand.Gaussian(0.5f, 0.125f);
			return Mathf.Clamp(sample, 0.01f, 1f) * sizeMultiplier;
		}

		public string GetStandardSizeLabel(float size)
		{
			return stages.FindLast(stage => stage.minSeverity <= size)?.label;
		}
	}
}
