using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 模块开关的持久化：只保存「被禁用」的模块 Id，新模块默认启用。
	/// </summary>
	public class ModuleSettings : IExposable
	{
		private List<string> disabled = new List<string>();

		public bool IsEnabled(string id)
		{
			return !id.NullOrEmpty() && !disabled.Contains(id);
		}

		public void SetEnabled(string id, bool enabled)
		{
			if (id.NullOrEmpty())
			{
				return;
			}

			if (enabled)
			{
				disabled.Remove(id);
			}
			else if (!disabled.Contains(id))
			{
				disabled.Add(id);
			}
		}

		public void Reset()
		{
			disabled.Clear();
		}

		/// <summary>去掉空值与重复项；未知 Id 保留，卸载模块后不会误伤其他设置。</summary>
		public bool Validate()
		{
			List<string> cleaned = disabled
				.Where(id => !id.NullOrEmpty())
				.Distinct()
				.ToList();

			if (cleaned.Count == disabled.Count)
			{
				return false;
			}

			disabled = cleaned;
			return true;
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref disabled, "disabled", LookMode.Value);
			if (disabled == null)
			{
				disabled = new List<string>();
			}
		}
	}
}
