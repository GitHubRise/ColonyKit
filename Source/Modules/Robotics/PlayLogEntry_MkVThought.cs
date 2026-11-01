using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 的一次 AI 想法，作为一条 PlayLog 条目写进游戏日志。
	/// 选中机器人后左下角「日志」页按 <see cref="Concerns"/> 过滤，就能看到它每次决策时的第一人称独白。
	/// </summary>
	public class PlayLogEntry_MkVThought : LogEntry
	{
		private Pawn pawn;

		private string thought;

		public PlayLogEntry_MkVThought()
		{
		}

		public PlayLogEntry_MkVThought(Pawn pawn, string thought, LogEntryDef def)
			: base(def)
		{
			this.pawn = pawn;
			this.thought = thought;
		}

		public override bool Concerns(Thing t)
		{
			return pawn != null && t == pawn;
		}

		public override IEnumerable<Thing> GetConcerns()
		{
			if (pawn != null)
			{
				yield return pawn;
			}
		}

		protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
		{
			// 机器人被销毁后条目可能还在（PlayLog 只在 pawn 被丢弃时清理），这时别让整页空引用。
			if (pawn == null)
			{
				return "CK.Robotics.Log.Thought.NoPawn".Translate(thought ?? "");
			}
			return "CK.Robotics.Log.Thought".Translate(pawn.LabelShort, thought ?? "");
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref pawn, "pawn", saveDestroyedThings: true);
			Scribe_Values.Look(ref thought, "thought");
		}
	}
}
