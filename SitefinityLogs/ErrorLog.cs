using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SitefinitySupport.Logs
{
	public class Error
	{
		public string timestamp;
		public string message;
		public List<string> stacktrace;
		public string URL;

		public Error()
		{
			stacktrace = new List<string>();
		}
	}

	public class ErrorLog
	{
		List<Error> errors;
		HashSet<string> display;

		public ErrorLog(string path, bool all)
		{
			display = new HashSet<string>() { "timestamp", "message", "url", "stack" };
			Read(path, all);
		}

		public void Filter(Dictionary<string, string> args)
		{
			if (args.ContainsKey("url"))
			{
				string urlMatch = args["url"];
				if (urlMatch != "") errors = errors.Where(e => e.URL != null && e.URL.Substring(15).ToLower().Contains(urlMatch)).ToList();
			}

			if (args.ContainsKey("message"))
			{
				string msgMatch = args["message"];
				if (msgMatch != "") errors = errors.Where(e => e.message != null && e.message.Substring(10).ToLower().Contains(msgMatch)).ToList();
			}
		}

		public void SetDisplayFields(HashSet<string> fields)
		{
			display = fields;
		}

		public string Display()
		{
			if (errors == null) return "";

			string results = "";

			foreach (Error e in errors)
			{
				if (display.Contains("timestamp")) results += e.timestamp + "\n";
				if (display.Contains("message")) results += e.message + "\n";
				if (display.Contains("url")) results += e.URL + "\n";
				if (display.Contains("stack") && e.stacktrace.Count > 0) results += e.stacktrace[0] + "\n";
				if (display.Contains("fullstack") && e.stacktrace.Count > 0) results += string.Join("\n", e.stacktrace) + "\n";
				results += "\n";
			}

			return results;
		}

		public static string Summary(string path, bool groupByUrl = false)
		{
			string[] files = Directory.GetFiles(path, "Error*.log");

			List<string> messages = new List<string>();

			string filterPattern = groupByUrl ? "Requested URL : " : "Message : ";
			int filterPadding = filterPattern.Length;

			foreach (string file in files)
			{
				try
				{
					messages.AddRange(Util.ReadLines(file).Where(l => l.StartsWith(filterPattern)));
				}
				catch (Exception) { }
			}

			var sorted = messages.GroupBy(i => i).OrderBy(grp => grp.Count()).Select(grp => grp.Count().ToString() + ": " + grp.Key.Substring(filterPadding));

			return string.Join("\n", sorted);
		}

		// Helper functions
		public void Read(string path, bool all)
		{
			errors = new List<Error>();
			Error error = new Error();
			bool inStack = false;
			string[] files;

			if (all)
			{
				files = Directory.GetFiles(path, "Error*.log");
				List<string> messages = new List<string>();
			}
			else
			{
				files = new string[1] { path + "\\Error.log" };
			}

			foreach (string file in files)
			{
				try
				{
					if (File.Exists(file))
					{
						foreach (string line in Util.ReadLines(file))
						{
							if (line == "----------------------------------------") error = new Error();
							else if (line.StartsWith("Timestamp:"))
							{
								if (error.timestamp == null)
								{
									error.timestamp = line;
									errors.Add(error);
								}
							}
							else if (line.StartsWith("Requested URL :")) error.URL = line;
							else if (line.StartsWith("Message :")) error.message = line;
							else if (line.StartsWith("Stack Trace :"))
							{
								error.stacktrace.Add(line);
								inStack = true;
							}
							else if (line == "") inStack = false;
							else if (inStack) error.stacktrace.Add(line);
						}
					}
				}
				catch (Exception) { }
			}
		}
	}
}
