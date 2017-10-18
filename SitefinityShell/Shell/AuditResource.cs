using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SitefinityShell.LogFiles;

namespace SitefinitySupport.Shell
{
	public class AuditResource : Resource
	{
		protected AuditTrail auditTrail;

		public AuditResource(IShellService theSvc)
			: base(theSvc, "Audits")
		{
			display = new HashSet<string> { "timestamp", "username", "item" };
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data") + "\\Sitefinity\\Logs\\";
			auditTrail = new AuditTrail(path);
		}

		public override void CMD_filter(Arguments args)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			foreach (string key in args.Keys)
				dict.Add(key, args[key]);

			auditTrail.Filter(dict);
		}

		public override string Serialize_Result()
		{
			if (!summary.IsNullOrEmpty()) return summary;
			if (auditTrail == null) return "";

			auditTrail.SetDisplayFields(display);
			return auditTrail.Display();
		}

		public override void CMD_help()
		{
			summary =
				"list: displays the events from the Audit*.log files\n" +
				"filter [timestamp|username|type|itemtitle|itemid|itemtype|itemurl|updates]=<substring>: filters the events whose <field> contains <substring>\n" +
				"display [timestamp] [username] [type] [itemtitle] [itemid] [itemtype] [itemurl] [updates]: selects what error fields to display\n" +
				"\n" +
				"Use a comma to chain multiple commands, e.g.\n" +
				"list, filter type=new, display username item type updates\n";

			base.CMD_help();
		}
	}
}
