namespace ColonyKit.Modules.Robotics
{
	/// <summary>给 AI 的候选动作；id 是唯一可执行标识（kind:thingID 或固定动作）。</summary>
	internal class AiCandidate
	{
		public string id;
		public string kind;
		public string label;
		public int dist;

		/// <summary>威胁分（越大越该先打）；只有攻击候选才有。</summary>
		public int threat;

		/// <summary>威胁标签：范围 / 高伤；普通目标留空。</summary>
		public string tag;
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

		/// <summary>刚收到敌袭预警：该放下家务主动出击。</summary>
		public bool alert;

		public AiCandidate[] candidates;
	}

	/// <summary>HTTP 线程回传的结果；只在主线程读取。</summary>
	internal class AiRequestResult
	{
		public bool ok;
		public string action;

		/// <summary>嘴上说的一句短话，显示在头顶。</summary>
		public string say;

		/// <summary>第一人称的内心独白，写进「日志」页。</summary>
		public string think;

		public string error;

		public static AiRequestResult Success(string action, string say, string think = null)
		{
			return new AiRequestResult { ok = true, action = action, say = say, think = think };
		}

		public static AiRequestResult Failure(string error)
		{
			return new AiRequestResult { ok = false, error = error };
		}
	}
}
