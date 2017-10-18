using SitefinitySupport.Logs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SitefinityShell.LogFiles
{

	class Event
	{
		public string timestamp;
		public string type;
		public string updates;
		public string username;
		public string itemtitle;
		public string itemid;
		public string itemtype;
		public string itemurl;
	}

	public class AuditTrail
	{
		List<Event> events;
		HashSet<string> display;

		public AuditTrail(string path)
		{
			display = new HashSet<string>() { "timestamp", "username", "itemtitle", "itemid", "itemtype", "itemurl", "updates" };
			Read(path);
		}

		public void Filter(Dictionary<string, string> args)
		{
			if (args.ContainsKey("timestamp"))
			{
				string msgMatch = args["timestamp"];
				if (msgMatch != "") events = events.Where(e => e.timestamp != null && e.timestamp.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("username"))
			{
				string msgMatch = args["username"];
				if (msgMatch != "") events = events.Where(e => e.username != null && e.username.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("type"))
			{
				string msgMatch = args["type"];
				if (msgMatch != "") events = events.Where(e => e.type != null && e.type.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("itemtitle"))
			{
				string msgMatch = args["itemtitle"];
				if (msgMatch != "") events = events.Where(e => e.itemtitle != null && e.itemtitle.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("itemurl"))
			{
				string msgMatch = args["itemurl"];
				if (msgMatch != "") events = events.Where(e => e.itemurl != null && e.itemurl.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("itemid"))
			{
				string msgMatch = args["itemid"];
				if (msgMatch != "") events = events.Where(e => e.itemid != null && e.itemid.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("itemtype"))
			{
				string msgMatch = args["itemtype"];
				if (msgMatch != "") events = events.Where(e => e.itemtype != null && e.itemtype.ToLower().Contains(msgMatch)).ToList();
			}
			if (args.ContainsKey("updates"))
			{
				string msgMatch = args["updates"];
				if (msgMatch != "") events = events.Where(e => e.updates != null && e.updates.ToLower().Contains(msgMatch)).ToList();
			}

		}

		public void SetDisplayFields(HashSet<string> fields)
		{
			display = fields;
		}

		public string Display()
		{
			if (events == null) return "";

			string results = "";

			foreach (Event e in events)
			{
				if (display.Contains("timestamp")) results += "Timestamp: " + e.timestamp + "\n";
				if (display.Contains("username")) results += "Username: " + e.username + "\n";
				if (display.Contains("type")) results += "Event type: " + e.type + "\n";
				if (display.Contains("itemtitle")) results += "Item: " + e.itemtitle + "\n";
				if (display.Contains("itemtype")) results += "Item Type: " + e.itemtype + "\n";
				if (display.Contains("itemid")) results += "Item Id: " + e.itemid + "\n";
				if (display.Contains("itemurl")) results += "Item URL: " + e.itemurl + "\n";
				if (display.Contains("item") && e.itemid != "") results += String.Format("Item: {0} ({1}) - {2}\n", e.itemtitle, e.itemid, e.itemtype);
				if (display.Contains("updates") && e.updates != "") results += "Updates: " + e.updates + "\n";
				results += "\n";
			}

			return results;
		}

		// Helper functions
		public void Read(string path)
		{
			events = new List<Event>();
			Event evt;
			string[] files;

			files = Directory.GetFiles(path, "Audit*.log");
			List<string> messages = new List<string>();

			foreach (string file in files)
			{
				try
				{
					if (File.Exists(file))
					{
						foreach (string line in Util.ReadLines(file))
						{
							if (line.StartsWith("----------------------------------")) continue;
							evt = new Event();
							events.Add(evt);

							evt.timestamp = GetFieldValue(line, "Timestamp");
							evt.username = GetFieldValue(line, "UserName");
							evt.type = GetFieldValue(line, "EventType");
							evt.itemtitle = GetFieldValue(line, "ItemTitle");
							evt.itemtype = GetFieldValue(line, "ItemType");
							evt.itemid = GetFieldValue(line, "ItemId");
							evt.itemurl = GetFieldValue(line, "ItemUrl");
							evt.updates = "";

							// Check if there is an Updates section
							Regex regex = new Regex("\\\\\"Updates\\\\\":{(.*)}}");
							Match match = regex.Match(line);
							if (match.Success && match.Groups.Count >= 2)
							{
								string update = match.Groups[1].Value;
								update = Regex.Replace(update, "\\\\\"\\$type\\\\\":\\\\\"([^\"]*)\\\\\",", "").Replace("\\", "");
								evt.updates = update;
							}

//							line = Regex.Replace(line, "\"\\$type\":\"([^\"]*)\",", "");
						}
					}
				}
				catch (Exception) { }
			}
		}

		private string GetFieldValue(string content, string fieldName)
		{
			Regex regex = new Regex(String.Format("\\\\\"{0}\\\\\":\\\\\"([^\"]*)\\\\\"", fieldName));
			Match match = regex.Match(content);
			if (!match.Success) return "";
			if (match.Groups.Count < 2) return "";
			return match.Groups[1].Value;
		}
	}
}
