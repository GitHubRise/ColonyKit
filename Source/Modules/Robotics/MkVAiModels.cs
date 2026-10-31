namespace ColonyKit.Modules.Robotics
{
	/// <summary>给 AI 的候选动作；id 是唯一可执行标识（kind:thingID 或固定动作）。</summary>
	internal class AiCandidate
	{
		public string id;
		public string kind;
		public string label;
		public int dist;
	}

	/// <summary>发给模型的局势快照。</summary>
	internal class AiPrompt
	{
		public string name;
		public float hp;
		public float energy;
		public string pos;
		public bool drafted;
		public int hostiles;
		public int colonists;
		public int downed;
		public int fires;
		public AiCandidate[] candidates;
	}

	/// <summary>HTTP 线程回传的结果；只在主线程读取。</summary>
	internal class AiRequestResult
	{
		public bool ok;
		public string action;
		public string say;
		public string error;

		public static AiRequestResult Success(string action, string say)
		{
			return new AiRequestResult { ok = true, action = action, say = say };
		}

		public static AiRequestResult Failure(string error)
		{
			return new AiRequestResult { ok = false, error = error };
		}
	}
}
