using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 底部主菜单（MainButtonsRoot）的统一控制：显示/隐藏、调整顺序、自定义按钮。
	/// </summary>
	/// <remarks>
	/// 原版在 <c>MainButtonsRoot</c> 构造时把 <c>DefDatabase&lt;MainButtonDef&gt;</c> 按 order 排好，
	/// 之后只读这个私有列表；这里用 Harmony 在构造后重排，并在 Worker.Visible 上追加可见性规则。
	/// 只影响底部按钮，不动按钮自己的 Worker 行为。
	/// </remarks>
	public static class MainButtonService
	{
		/// <summary>模板自带的底部演示按钮 defName。</summary>
		public const string DemoButtonDefName = "ModTemplate_Demo";

		private static readonly Dictionary<string, bool> visibleOverrides =
			new Dictionary<string, bool>();

		private static readonly Dictionary<string, int> orderOverrides =
			new Dictionary<string, int>();

		private static readonly FieldInfo ButtonsField =
			AccessTools.Field(typeof(MainButtonsRoot), "allButtonsInOrder");

		/// <summary>强制显示/隐藏某个按钮（覆盖 Def 的 buttonVisible）。</summary>
		public static void SetVisible(string defName, bool visible)
		{
			if (defName.NullOrEmpty())
			{
				return;
			}
			visibleOverrides[defName] = visible;
		}

		public static void ClearVisible(string defName)
		{
			if (!defName.NullOrEmpty())
			{
				visibleOverrides.Remove(defName);
			}
		}

		/// <summary>调整按钮顺序；游戏内会立即重排当前按钮条。</summary>
		public static void SetOrder(string defName, int order)
		{
			if (defName.NullOrEmpty())
			{
				return;
			}
			orderOverrides[defName] = order;
			if (Current.ProgramState == ProgramState.Playing)
			{
				ApplyOrder(Find.MainButtonsRoot);
			}
		}

		public static void ClearOrder(string defName)
		{
			if (!defName.NullOrEmpty())
			{
				orderOverrides.Remove(defName);
			}
		}

		/// <summary>综合 Def 的 buttonVisible、显式覆盖和模板设置，决定按钮是否可见。</summary>
		public static bool IsVisible(MainButtonDef def)
		{
			if (def == null || !def.buttonVisible)
			{
				return false;
			}

			if (def.defName == DemoButtonDefName
				&& !(ModTemplateMod.Settings?.general?.showDemoButton ?? true))
			{
				return false;
			}

			return !visibleOverrides.TryGetValue(def.defName, out bool visible) || visible;
		}

		public static int OrderOf(MainButtonDef def)
		{
			if (def != null && orderOverrides.TryGetValue(def.defName, out int order))
			{
				return order;
			}
			return def?.order ?? 0;
		}

		internal static void ApplyOrder(MainButtonsRoot root)
		{
			if (root == null)
			{
				return;
			}

			if (ButtonsField == null)
			{
				ModLog.Once(
					"mainbutton.field-missing",
					ModLogLevel.Warn,
					"MainButton",
					"allButtonsInOrder not found; order overrides are ignored.");
				return;
			}

			if (!(ButtonsField.GetValue(root) is List<MainButtonDef> buttons))
			{
				ModLog.Once(
					"mainbutton.field-type",
					ModLogLevel.Warn,
					"MainButton",
					"allButtonsInOrder is not List<MainButtonDef>; order overrides are ignored.");
				return;
			}

			buttons.Sort((left, right) => OrderOf(left).CompareTo(OrderOf(right)));
		}
	}
}
