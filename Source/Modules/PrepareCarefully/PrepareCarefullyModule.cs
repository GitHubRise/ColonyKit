using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules
{
	/// <summary>
	/// 内置「精心准备」（EdB Prepare Carefully）模块。
	/// </summary>
	/// <remarks>
	/// 程序集 <c>Assemblies\EdBPrepareCarefully.dll</c> 与内容（Defs / Textures / Languages）随 ColonyKit 一起分发，
	/// 但补丁是否生效由模块开关决定：关掉后 <see cref="PrepareCarefullyBridge"/> 会把它的两个 Postfix 卸掉，
	/// 开局页不再出现「精心准备」按钮。外部装了创意工坊版时只告警、不接管它的补丁。
	/// </remarks>
	public sealed class PrepareCarefullyModule : ColonyKitModule
	{
		public override string Id => "prepare-carefully";

		public override string DisplayNameKey => "ColonyKit.Module.PrepareCarefully.Name";

		public override string DescriptionKey => "ColonyKit.Module.PrepareCarefully.Desc";

		public override int InitOrder => 135;

		public override void OnDefsLoaded()
		{
			bool externalPc = ModsConfig.IsActive("EdB.PrepareCarefully");
			ModLog.Info("PC", $"bundled Prepare Carefully 1.6 loaded; external EdB.PrepareCarefully active={externalPc}");

			if (externalPc)
			{
				ModLog.Warn(
					"PC",
					"external EdB Prepare Carefully is active together with the bundled one; " +
					"disable one to avoid duplicate patches.");
				return;
			}

			PrepareCarefullyBridge.Request(true);
		}

		public override void ResetRuntimeState()
		{
			if (ModsConfig.IsActive("EdB.PrepareCarefully"))
			{
				return;
			}

			PrepareCarefullyBridge.Request(ModuleRegistry.IsEnabled(Id));
		}
	}

	/// <summary>
	/// 控制内置精心准备的 Harmony 补丁：在「启用 / 禁用」之间装卸它的两个 Postfix。
	/// </summary>
	/// <remarks>
	/// 不在 <c>OnDefsLoaded</c> 里直接装卸，是因为 Bootstrap 与精心准备自己的
	/// <c>[StaticConstructorOnStartup]</c> 谁先执行没有保证；改为记下目标状态，
	/// 由 <see cref="Patch_Root_Update_PrepareCarefully"/> 在游戏主循环里对齐一次。
	/// </remarks>
	internal static class PrepareCarefullyBridge
	{
		private const string PcHarmonyId = "EdB.PrepareCarefully";
		private const string GateHarmonyId = "local.colonykit.pc-gate";

		private static bool? desired;
		private static bool? applied;

		public static void Request(bool enabled)
		{
			desired = enabled;
		}

		public static void Tick()
		{
			if (!desired.HasValue || applied == desired)
			{
				return;
			}

			Apply(desired.Value);
		}

		private static void Apply(bool enabled)
		{
			Type buttonPatch = FindType("EdB.PrepareCarefully.HarmonyPatches.PrepareCarefullyButtonPatch");
			Type scenarioPatch = FindType("EdB.PrepareCarefully.HarmonyPatches.ReplaceScenarioPatch");
			if (buttonPatch == null || scenarioPatch == null)
			{
				// 没有内置程序集（例如只装了外部版），什么都不用做。
				applied = enabled;
				return;
			}

			MethodInfo pageMethod = AccessTools.Method(
				typeof(Page_ConfigureStartingPawns), "DoWindowContents", new[] { typeof(Rect) });
			MethodInfo newGameMethod = AccessTools.Method(typeof(Game), "InitNewGame", Type.EmptyTypes);
			if (pageMethod == null || newGameMethod == null)
			{
				ModLog.Warn("PC", "cannot locate Page_ConfigureStartingPawns.DoWindowContents / Game.InitNewGame");
				applied = enabled;
				return;
			}

			Harmony harmony = new Harmony(GateHarmonyId);
			bool pagePatched = HasPcPostfix(pageMethod);
			bool newGamePatched = HasPcPostfix(newGameMethod);

			if (enabled)
			{
				if (!pagePatched)
				{
					MethodInfo postfix = AccessTools.Method(buttonPatch, "Postfix");
					if (postfix != null) harmony.Patch(pageMethod, postfix: new HarmonyMethod(postfix));
				}
				if (!newGamePatched)
				{
					MethodInfo postfix = AccessTools.Method(scenarioPatch, "Postfix");
					if (postfix != null) harmony.Patch(newGameMethod, postfix: new HarmonyMethod(postfix));
				}
				applied = true;
				ModLog.Info("PC", "bundled Prepare Carefully patches enabled");
			}
			else
			{
				harmony.Unpatch(pageMethod, HarmonyPatchType.Postfix, PcHarmonyId);
				harmony.Unpatch(newGameMethod, HarmonyPatchType.Postfix, PcHarmonyId);
				applied = false;
				ModLog.Info("PC", "bundled Prepare Carefully patches disabled");
			}
		}

		private static bool HasPcPostfix(MethodBase method)
		{
			Patches info = Harmony.GetPatchInfo(method);
			return info != null
				&& info.Postfixes != null
				&& info.Postfixes.Any(patch => patch.owner == PcHarmonyId);
		}

		private static Type FindType(string fullName)
		{
			Type type = Type.GetType(fullName + ", EdBPrepareCarefully");
			if (type != null)
			{
				return type;
			}

			return AppDomain.CurrentDomain.GetAssemblies()
				.Select(assembly => assembly.GetType(fullName, false))
				.FirstOrDefault(found => found != null);
		}
	}

	/// <summary>每帧把内置精心准备的补丁状态对齐到模块开关（只在状态变化时动手）。</summary>
	[HarmonyPatch(typeof(Root), "Update")]
	internal static class Patch_Root_Update_PrepareCarefully
	{
		private static void Prefix()
		{
			PrepareCarefullyBridge.Tick();
		}
	}
}

