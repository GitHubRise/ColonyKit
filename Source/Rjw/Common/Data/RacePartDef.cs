#nullable disable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace rjw
{
	/// <summary>
	/// 定义如何在新创建的小人身上生成单个性部位。
	/// </summary>
	public class RacePartDef : Def
	{
		public bool IsNone => string.IsNullOrEmpty(hediffName);

		public SexFluidDef FluidDef { get; private set; }

		public static readonly RacePartDef None = new RacePartDef();

		/// <summary>
		/// 要创建的状态名称。
		/// 不直接使用 hediff，因为当其他模组想要创建并引用
		/// 继承自 rjw 基础定义的定制部位时，那会引发问题。
		/// </summary>
		public string hediffName;

		#pragma warning disable CS0649 // 'Unused' private variable - Set on def creation
		private string fluidType;
		#pragma warning restore

		public float? fluidModifier;
		public SimpleCurve severityCurve;

		public bool TryGetHediffDef(out HediffDef hediffDef)
		{
			hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
			if (hediffDef == null)
			{
				ModLog.Error($"Could not find a HediffDef named {hediffName} referenced by RacePartDef named {defName}.");
				return false;
			}
			else
			{
				return true;
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			if (!fluidType.NullOrEmpty())
			{
				FluidDef = DefDatabase<SexFluidDef>.GetNamedSilentFail(fluidType);
			}
		}
	}
}
