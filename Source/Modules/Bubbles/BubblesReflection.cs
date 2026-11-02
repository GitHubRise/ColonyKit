using System.Reflection;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Bubbles
{
	/// <summary>
	/// 原版私有字段的入口：气泡需要知道互动的发起者/接收者，以及相机的当前高度。
	/// </summary>
	/// <remarks>
	/// 字段名沿用 Interaction Bubbles 4.2 在 1.6 上的做法；每个字段都可能因为游戏版本变化而找不到，
	/// 所以调用方一律判空，找不到就少画气泡，而不是让补丁抛异常。
	/// </remarks>
	internal static class BubblesReflection
	{
		public static readonly FieldInfo PlayLogEntryInteractionInitiator =
			AccessTools.Field(typeof(PlayLogEntry_Interaction), "initiator");

		public static readonly FieldInfo PlayLogEntryInteractionRecipient =
			AccessTools.Field(typeof(PlayLogEntry_Interaction), "recipient");

		public static readonly FieldInfo PlayLogEntryInteractionSinglePawnInitiator =
			AccessTools.Field(typeof(PlayLogEntry_InteractionSinglePawn), "initiator");

		public static readonly FieldInfo CameraDriverRootSize =
			AccessTools.Field(typeof(CameraDriver), "rootSize");
	}
}
