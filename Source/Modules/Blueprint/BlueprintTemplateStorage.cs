using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>磁盘上的一张蓝图模板。</summary>
	public sealed class BlueprintTemplate
	{
		public string path;
		public BlueprintRecord record;
		public string error;
	}

	/// <summary>
	/// 蓝图模板持久化：写到 <c>Config/ColonyKit/Blueprints/*.xml</c>，与存档无关，
	/// 可跨存档/跨殖民地复用。
	/// </summary>
	/// <remarks>
	/// 用 XML 而不是 JSON：RimWorld 自带的 <c>System.Xml</c> 足够，且模板要人可读、可手改；
	/// 字段与存档里的 Scribe 字段一一对应，schema 版本单独写在根节点。
	/// </remarks>
	public static class BlueprintTemplateStorage
	{
		private const int SchemaVersion = 1;
		private const int MaxNameLength = 64;

		public static string FolderPath =>
			Path.Combine(GenFilePaths.ConfigFolderPath, "ColonyKit", "Blueprints");

		/// <summary>把蓝图保存为模板；返回写入的文件路径。</summary>
		public static string Save(BlueprintRecord record, out string error)
		{
			error = null;
			if (record == null)
			{
				error = "CK.Blueprint.Template.Nothing".Translate();
				return null;
			}

			try
			{
				Directory.CreateDirectory(FolderPath);
				string path = UniquePath(Path.Combine(FolderPath, SanitizeFileName(record.name) + ".xml"));

				XmlWriterSettings settings = new XmlWriterSettings
				{
					Indent = true,
					Encoding = new UTF8Encoding(false),
					CloseOutput = true
				};

				using (XmlWriter writer = XmlWriter.Create(path, settings))
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("ColonyKitBlueprint");
					writer.WriteAttributeString("schema", SchemaVersion.ToString(CultureInfo.InvariantCulture));

					writer.WriteElementString("name", record.name ?? "");
					writer.WriteElementString("sizeX", record.sizeX.ToString(CultureInfo.InvariantCulture));
					writer.WriteElementString("sizeZ", record.sizeZ.ToString(CultureInfo.InvariantCulture));
					writer.WriteElementString("createdTick", record.createdTick.ToString(CultureInfo.InvariantCulture));

					writer.WriteStartElement("entries");
					foreach (BlueprintEntry entry in record.entries)
					{
						if (entry == null || entry.defName.NullOrEmpty())
						{
							continue;
						}

						writer.WriteStartElement("entry");
						writer.WriteAttributeString("def", entry.defName);
						if (!entry.stuffDefName.NullOrEmpty())
						{
							writer.WriteAttributeString("stuff", entry.stuffDefName);
						}
						writer.WriteAttributeString("rot", entry.rot.ToString(CultureInfo.InvariantCulture));
						writer.WriteAttributeString("x", entry.x.ToString(CultureInfo.InvariantCulture));
						writer.WriteAttributeString("z", entry.z.ToString(CultureInfo.InvariantCulture));
						writer.WriteEndElement();
					}
					writer.WriteEndElement(); // entries

					writer.WriteEndElement(); // ColonyKitBlueprint
					writer.WriteEndDocument();
				}

				return path;
			}
			catch (Exception exception)
			{
				error = "CK.Blueprint.Template.SaveFailed".Translate(exception.Message);
				ModLog.Error("Blueprint", "failed to save template", exception);
				return null;
			}
		}

		/// <summary>读取模板目录里的全部模板；坏文件会带 <see cref="BlueprintTemplate.error"/> 返回，不抛出。</summary>
		public static List<BlueprintTemplate> LoadAll()
		{
			List<BlueprintTemplate> templates = new List<BlueprintTemplate>();
			try
			{
				if (!Directory.Exists(FolderPath))
				{
					return templates;
				}

				foreach (string path in Directory.GetFiles(FolderPath, "*.xml"))
				{
					templates.Add(Load(path));
				}
			}
			catch (Exception exception)
			{
				ModLog.Error("Blueprint", "failed to scan template folder", exception);
			}

			templates.Sort((left, right) =>
			{
				string leftName = left.record?.name ?? Path.GetFileName(left.path);
				string rightName = right.record?.name ?? Path.GetFileName(right.path);
				return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
			});
			return templates;
		}

		public static BlueprintTemplate Load(string path)
		{
			BlueprintTemplate template = new BlueprintTemplate { path = path };
			try
			{
				XmlDocument document = new XmlDocument();
				document.Load(path);

				XmlElement root = document.DocumentElement;
				if (root == null || root.Name != "ColonyKitBlueprint")
				{
					template.error = "CK.Blueprint.Template.BadRoot".Translate();
					return template;
				}

				BlueprintRecord record = new BlueprintRecord
				{
					name = root["name"]?.InnerText,
					sizeX = ParseInt(root["sizeX"]?.InnerText, 1),
					sizeZ = ParseInt(root["sizeZ"]?.InnerText, 1),
					createdTick = ParseInt(root["createdTick"]?.InnerText, 0)
				};

				XmlNode entriesNode = root["entries"];
				if (entriesNode != null)
				{
					foreach (XmlNode node in entriesNode.ChildNodes)
					{
						if (!(node is XmlElement element) || element.Name != "entry")
						{
							continue;
						}

						string defName = element.GetAttribute("def");
						if (defName.NullOrEmpty())
						{
							continue;
						}

						record.entries.Add(new BlueprintEntry
						{
							defName = defName,
							stuffDefName = element.GetAttribute("stuff"),
							rot = ParseInt(element.GetAttribute("rot"), 0),
							x = ParseInt(element.GetAttribute("x"), 0),
							z = ParseInt(element.GetAttribute("z"), 0)
						});
					}
				}

				record.Normalize();
				if (record.entries.Count == 0)
				{
					template.error = "CK.Blueprint.Template.Empty".Translate();
					return template;
				}

				template.record = record;
				return template;
			}
			catch (Exception exception)
			{
				template.error = "CK.Blueprint.Template.LoadFailed".Translate(exception.Message);
				ModLog.Warn("Blueprint", $"failed to load template '{path}': {exception.Message}");
				return template;
			}
		}

		public static bool Delete(BlueprintTemplate template, out string error)
		{
			error = null;
			if (template == null || template.path.NullOrEmpty())
			{
				return false;
			}

			try
			{
				File.Delete(template.path);
				return true;
			}
			catch (Exception exception)
			{
				error = "CK.Blueprint.Template.DeleteFailed".Translate(exception.Message);
				ModLog.Error("Blueprint", "failed to delete template", exception);
				return false;
			}
		}

		private static int ParseInt(string text, int fallback)
		{
			return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
				? value
				: fallback;
		}

		private static string SanitizeFileName(string name)
		{
			if (name.NullOrEmpty())
			{
				return "blueprint";
			}

			StringBuilder builder = new StringBuilder(name.Length);
			char[] invalid = Path.GetInvalidFileNameChars();
			foreach (char c in name.Trim())
			{
				builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
			}

			string result = builder.ToString().Trim(' ', '.', '_');
			if (result.NullOrEmpty())
			{
				result = "blueprint";
			}
			if (result.Length > MaxNameLength)
			{
				result = result.Substring(0, MaxNameLength);
			}
			return result;
		}

		private static string UniquePath(string path)
		{
			if (!File.Exists(path))
			{
				return path;
			}

			string directory = Path.GetDirectoryName(path);
			string fileName = Path.GetFileNameWithoutExtension(path);
			for (int index = 2; index < 1000; index++)
			{
				string candidate = Path.Combine(directory, fileName + " (" + index + ").xml");
				if (!File.Exists(candidate))
				{
					return candidate;
				}
			}
			return path;
		}
	}
}
