using Verse;

namespace rjw;

public static class ModLog
{
	/// <summary>
	/// 记录给定消息，并在其后附加 [SaveStorage.ModId]。
	/// </summary>
	public static void Error(string message)
	{
		Log.Error($"[{SaveStorage.ModId}] {message}");
	}

	/// <summary>
	/// 记录给定消息，并在其后附加 [SaveStorage.ModId]。
	/// </summary>
	public static void Message(string message)
	{
		Log.Message($"[{SaveStorage.ModId}] {message}");
	}

	/// <summary>
	/// 记录给定消息，并在其后附加 [SaveStorage.ModId]。
	/// </summary>
	public static void Warning(string message)
	{
		Log.Warning($"[{SaveStorage.ModId}] {message}");
	}

	/// <summary>
	/// 仅记录一次给定消息，并在其后附加 [SaveStorage.ModId]。
	/// </summary>
	public static void WarningOnce(string message, int key)
	{
		Log.WarningOnce($"[{SaveStorage.ModId}] {message}", key);
	}

	/// <summary>
	/// 仅记录一次给定消息，并在其后附加 [SaveStorage.ModId]。
	/// </summary>
	public static void ErrorOnce(string message, int key)
	{
		Log.ErrorOnce($"[{SaveStorage.ModId}] {message}", key);
	}
}

