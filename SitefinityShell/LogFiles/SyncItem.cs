using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace SitefinitySupport.Logs
{
	[DataContract]
	[KnownType(typeof(SyncItem))]
	[KnownType(typeof(DestSyncItem))]
	public class SyncItem
	{
		public string name;
		[DataMember] public DateTime timestamp;
		[DataMember] public bool success;
		public string item;
		[DataMember] public string error;
		[DataMember] public string id;
		public string type;

		public SyncItem()
		{

		}

		public SyncItem(string descr)
		{
			int i = descr.IndexOf("]");
			timestamp = DateTime.Parse(descr.Substring(1, i - 1));
			error = "";
		}

		public void SetId(string line)
		{
			Regex regex = new Regex(@"^Item information: id = '(.*?)'; type = '(.*?)'");
			Match match = regex.Match(line);
			if (match.Success) {
				id = match.Groups[1].Value;
				type = match.Groups[2].Value;
			}
		}

		public virtual void AddError(string line)
		{
			if (error == "") error = line;
			else error += "\n" + line;
		}
	}

	public class SourceSyncItem : SyncItem
	{
		public static bool IsNewItem(string line)
		{
			return line.Contains("] Item ");
		}

		public SourceSyncItem(string descr)
			: base(descr)
		{
			success = descr.EndsWith(" was successfully sent.");

			int j, i = descr.IndexOf("] Item '");
			if (i >= 0)
			{
				i += 8;
				j = descr.IndexOf("'", i);
				name = descr.Substring(i, j - i);
			}
			else name = "";
		}

		public override void AddError(string line)
		{
			if (error != "") return;
			
			error = line.StartsWith("Error details:Microsoft.Http.HttpStageProcessingException: GetResponse timed out") ? "Error details:Network timeout" : line;
		}
	}

	[Serializable]
	public class DestSyncItem : SyncItem
	{
		public static bool IsNewItem(string line)
		{
			return (line.Contains("] Error importing item") ||
					line.Contains("] Imported an item") ||
					line.Contains("] Removed an item"));
		}

		public DestSyncItem(string descr)
			: base(descr)
		{
			success = descr.EndsWith("] Imported an item.") || descr.EndsWith("] Removed an item.");
		}

		public override void AddError(string line)
		{
			if (error == "") error = line;
			else error += "\n" + line;
		}
	}
}
