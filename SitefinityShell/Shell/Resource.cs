using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity.Multisite;

namespace SitefinitySupport.Shell
{
	public class Resource
	{
		protected IShellService svc;
		protected string name;
		protected string summary;
		protected HashSet<string> display;
		protected string providerName;
		protected Dictionary<int, IQueryable<Telerik.Sitefinity.Security.Model.Permission>> group2Permissions;

		public Resource(IShellService theSvc, string name)
		{
			this.svc = theSvc;
			this.name = name;
			this.summary = null;
			this.providerName = this.svc.Get_Provider();
		}
		public virtual void CMD_update(Arguments args) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_cd(Arguments args, Guid rootId) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_list(Arguments args, Guid rootId) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_filter(Arguments args) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_summary(Arguments args) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_touch() { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_compare(Arguments args) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_republish(Arguments args) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_provider(Arguments args, Guid rootId) { svc.Set_Error("Command not supported for " + name); }
		public virtual void CMD_display(Arguments args)
		{
			display = args.Keys;
		}
		public virtual void CMD_help()
		{
			summary += "\n" +
				"site [Id]: list the sites / switches site\n" +
				"pages: switch to pages\n" +
				"bpages: switch to backend pages\n" +
				"errors: switch to the error logs\n" +
				"docs: switch to files & documents\n" +
				"images: switch to images\n" +
				"videos: switch to videos\n" +
				"sitesync: switch to the SiteSync logs\n" +
				"dynmod: switch to Dynamic Content\n" +
				"all: to republish all the content of a site\n";
		}
		public virtual string Serialize_Result() { return ""; }
		public virtual void Root() { }

		public virtual string PermissionText(int actions, string label) { return ""; }
	}
}