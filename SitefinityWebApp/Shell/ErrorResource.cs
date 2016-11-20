using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SitefinitySupport.Logs;

namespace SitefinitySupport.Shell
{
	public class ErrorResource : Resource
	{
		protected string[] log;
		protected ErrorLog logs;

		public ErrorResource(IShellService theSvc)
			: base(theSvc, "Errors")
		{
			display = new HashSet<string> { "timestamp", "message", "url", "stack" };
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data") + "\\Sitefinity\\Logs\\";
			logs = new ErrorLog(path, args.ContainsKey("all"));
		}

		public override void CMD_filter(Arguments args)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			foreach (string key in args.Keys)
				dict.Add(key, args[key]);

			logs.Filter(dict);
		}

		public override string Serialize_Result()
		{
			if (summary != null) return summary;

			if (logs == null) return "";

			logs.SetDisplayFields(display);
			return logs.Display();
		}

		public override void CMD_summary(Arguments args)
		{
			string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data") + "\\Sitefinity\\Logs\\";
			summary = ErrorLog.Summary(path, args.ContainsKey("url"));
		}

		public override void CMD_help()
		{
			summary =
				"list [all]: displays the errors from Error.log\n" +
				"filter [message|url]=<substring>: filters the errors whose URL contains <substring>\n" +
				"summary [url]: displays the most common errors messages (or URLs) found in all the Error log files\n" +
				"display [timestamp] [message] [url] [stack] [fullstack]: selects what error fields to display\n" +
				"\n" +
				"Use a comma to chain multiple commands, e.g.\n" +
				"list all, filter message:empty guid, display timestamp fullstack\n";

			base.CMD_help();
		}
	}
}
