using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>
	/// 蓝图属性对话框：名称、分组、标签与置顶。
	/// 标签用逗号/分号分隔，保存时去空、去重并截断到上限。
	/// </summary>
	public class Dialog_BlueprintProperties : Window
	{
		private readonly BlueprintRecord record;

		private string name;
		private string group;
		private string tagsText;
		private bool pinned;

		public override Vector2 InitialSize => new Vector2(460f, 340f);

		public Dialog_BlueprintProperties(BlueprintRecord record)
		{
			this.record = record;
			name = record.name ?? "";
			group = record.group ?? "";
			tagsText = record.tags == null ? "" : string.Join(", ", record.tags);
			pinned = record.pinned;

			doCloseX = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = false;
			forcePause = false;
		}

		public override void DoWindowContents(Rect inRect)
		{
			float y = 0f;

			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0f, y, inRect.width, 30f), "CK.Blueprint.Properties.Title".Translate());
			Text.Font = GameFont.Small;
			y += 36f;

			Widgets.Label(new Rect(0f, y, inRect.width, 20f), "CK.Blueprint.Properties.Name".Translate());
			y += 22f;
			name = Widgets.TextField(new Rect(0f, y, inRect.width, 28f), name);
			y += 36f;

			Widgets.Label(new Rect(0f, y, inRect.width, 20f), "CK.Blueprint.Properties.Group".Translate());
			y += 22f;
			group = Widgets.TextField(new Rect(0f, y, inRect.width, 28f), group);
			y += 36f;

			Widgets.Label(new Rect(0f, y, inRect.width, 20f), "CK.Blueprint.Properties.Tags".Translate());
			y += 22f;
			tagsText = Widgets.TextField(new Rect(0f, y, inRect.width, 28f), tagsText);
			y += 34f;

			Widgets.CheckboxLabeled(
				new Rect(0f, y, inRect.width, 24f),
				"CK.Blueprint.Properties.Pinned".Translate(),
				ref pinned);
			y += 32f;

			Widgets.Label(new Rect(0f, y, inRect.width, 34f), "CK.Blueprint.Properties.Hint".Translate());

			float buttonY = inRect.height - 38f;
			float buttonWidth = 110f;
			if (Widgets.ButtonText(
				new Rect(inRect.width - buttonWidth * 2f - 8f, buttonY, buttonWidth, 32f),
				"CK.Blueprint.Properties.Cancel".Translate()))
			{
				Close();
			}

			if (Widgets.ButtonText(
				new Rect(inRect.width - buttonWidth, buttonY, buttonWidth, 32f),
				"CK.Blueprint.Properties.OK".Translate()))
			{
				Apply();
			}
		}

		private void Apply()
		{
			string trimmed = name?.Trim();
			if (trimmed.NullOrEmpty())
			{
				Messages.Message(
					"CK.Blueprint.Properties.NameEmpty".Translate(),
					MessageTypeDefOf.RejectInput,
					false);
				return;
			}

			record.name = trimmed;
			record.group = group;
			record.tags = ParseTags(tagsText);
			record.pinned = pinned;
			record.Normalize();
			Close();
		}

		/// <summary>逗号/分号分隔；去空、去重、截断到记录上限。</summary>
		public static List<string> ParseTags(string text)
		{
			List<string> result = new List<string>();
			if (text.NullOrEmpty())
			{
				return result;
			}

			string[] parts = text.Split(
				new[] { ',', '，', ';', '；' },
				StringSplitOptions.RemoveEmptyEntries);
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string part in parts)
			{
				string tag = part.Trim();
				if (tag.Length > BlueprintRecord.MaxTagLength)
				{
					tag = tag.Substring(0, BlueprintRecord.MaxTagLength);
				}
				if (tag.Length == 0 || !seen.Add(tag))
				{
					continue;
				}

				result.Add(tag);
				if (result.Count >= BlueprintRecord.MaxTags)
				{
					break;
				}
			}
			return result;
		}
	}
}
