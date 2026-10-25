using ColonyKit.Modules.Storage;
using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>底部「立体库」按钮：打开仓库管理窗口。</summary>
	public sealed class MainButtonWorker_Storage : MainButtonWorker
	{
		public override void Activate()
		{
			Find.WindowStack.Add(new Window_StorageManagement());
		}
	}
}
