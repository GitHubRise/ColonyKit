using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>蓝图重命名对话框；复用原版 <see cref="Dialog_Rename{T}"/> 的输入/焦点处理。</summary>
	public class Dialog_RenameBlueprint : Dialog_Rename<BlueprintRecord>
	{
		public Dialog_RenameBlueprint(BlueprintRecord record) : base(record)
		{
		}

		protected override AcceptanceReport NameIsValid(string name)
		{
			if (name.NullOrEmpty())
			{
				return false;
			}
			return base.NameIsValid(name);
		}
	}
}
