using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 两个兼容点：GUI 分组调用，以及 Camera+ 的缩放。
	/// </summary>
	/// <remarks>
	/// <c>Widgets.BeginGroup/EndGroup</c> 在部分版本里不是 public，所以用反射拿到可调用的委托；
	/// Camera+ 会改写相机 rootSize，气泡高度必须跟它一致，否则缩放会跳。
	/// 两者都允许缺席：拿不到就退回 <see cref="GUI"/> 和原值。
	/// </remarks>
	internal static class BubblesCompat
	{
		private static readonly Action<Rect> BeginGroup = ResolveGroupMethod("BeginGroup");
		private static readonly Action EndGroup = ResolveEndGroupMethod();

		private static readonly bool CameraPlusLoaded = CheckCameraPlus();
		private static FastInvokeHandler cameraPlusLerpRootSize;

		public static void BeginGroupAt(Rect rect)
		{
			if (BeginGroup != null)
			{
				BeginGroup(rect);
			}
			else
			{
				GUI.BeginGroup(rect);
			}
		}

		public static void EndGroupNow()
		{
			if (EndGroup != null)
			{
				EndGroup();
			}
			else
			{
				GUI.EndGroup();
			}
		}

		/// <summary>Camera+ 启用时，用它的 LerpRootSize 修正气泡高度。</summary>
		public static void Apply(ref float altitude)
		{
			if (!CameraPlusLoaded)
			{
				return;
			}

			try
			{
				if (cameraPlusLerpRootSize == null)
				{
					cameraPlusLerpRootSize = MethodInvoker.GetHandler(AccessTools.Method("CameraPlus.Tools:LerpRootSize"));
				}

				if (cameraPlusLerpRootSize != null)
				{
					altitude = (float)cameraPlusLerpRootSize(null, altitude);
				}
			}
			catch (Exception exception)
			{
				ModLog.Warn("Bubbles", "Camera+ compatibility failed: " + exception.Message);
			}
		}

		private static bool CheckCameraPlus()
		{
			try
			{
				return ModsConfig.IsActive("brrainz.cameraplus");
			}
			catch
			{
				return false;
			}
		}

		private static Action<Rect> ResolveGroupMethod(string name)
		{
			try
			{
				var method = typeof(Widgets).GetMethod(name, new[] { typeof(Rect) })
					?? typeof(GUI).GetMethod(name, new[] { typeof(Rect) });
				if (method == null)
				{
					return null;
				}

				FastInvokeHandler handler = MethodInvoker.GetHandler(method);
				return rect => handler(null, rect);
			}
			catch (Exception exception)
			{
				ModLog.Warn("Bubbles", "failed to bind Widgets." + name + ": " + exception.Message);
				return null;
			}
		}

		private static Action ResolveEndGroupMethod()
		{
			try
			{
				var method = typeof(Widgets).GetMethod("EndGroup", Type.EmptyTypes)
					?? typeof(GUI).GetMethod("EndGroup", Type.EmptyTypes);
				if (method == null)
				{
					return null;
				}

				FastInvokeHandler handler = MethodInvoker.GetHandler(method);
				return () => handler(null);
			}
			catch (Exception exception)
			{
				ModLog.Warn("Bubbles", "failed to bind Widgets.EndGroup: " + exception.Message);
				return null;
			}
		}
	}
}
