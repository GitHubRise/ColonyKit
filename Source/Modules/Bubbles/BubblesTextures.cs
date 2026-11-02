using System;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 气泡用到的三张图：开关图标、内层（背景）、外层（边框）。
	/// </summary>
	/// <remarks>
	/// 路径刻意放在 <c>Textures/ColonyKit/Bubbles</c> 下，而不是原 Mod 的 <c>Bubbles/</c>：
	/// 两者同时加载时 <see cref="ContentFinder{T}"/> 会按同一路径命中同一份贴图，
	/// 换到独立路径后即使玩家没关原 Mod 也不会互相顶掉。
	/// </remarks>
	[StaticConstructorOnStartup]
	internal static class BubblesTextures
	{
		public static readonly Texture2D Icon = Load("Icon");
		public static readonly Texture2D Inner = Load("Inner");
		public static readonly Texture2D Outer = Load("Outer");

		private static Texture2D Load(string name)
		{
			try
			{
				return ContentFinder<Texture2D>.Get("ColonyKit/Bubbles/" + name);
			}
			catch (Exception exception)
			{
				ModLog.Error("Bubbles", "missing texture ColonyKit/Bubbles/" + name, exception);
				return BaseContent.WhiteTex;
			}
		}
	}
}
