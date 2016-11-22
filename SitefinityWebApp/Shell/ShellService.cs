using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.Multisite.Model;
using SitefinitySupport.Logs;

namespace SitefinitySupport.Shell
{
	public interface IShellService
	{
		void Set_Result(Resource rsc);
		void Set_Error(string error);
		void Set_Root(Guid root);
		void Set_Path(string path);
		void Set_Resource(string rsc);
		void CMD_pages();
		void CMD_bpages();
		Site Get_Site();
		void Set_Provider(string provider);
		string Get_Provider();
	}
	public class Output
	{
		public string response;
		public string error;
		public string root;
		public string resource;
		public string path;
		public string site;
		public string provider;
	}

	public class ShellService : IShellService
	{
		protected Output output;
		protected string commands;
		protected string root;
		protected string rscName;
		protected string provider;
		protected Guid siteId;

		public ShellService(string cmd, string root, string rsc, string site, string provider)
		{
			this.commands = cmd;
			this.root = root;
			this.rscName = rsc;
			this.provider = provider;
			try
			{
				this.siteId = new Guid(site);
			}
			catch (Exception) {
				this.siteId = SiteInitializer.CurrentFrontendRootNodeId;
			}

			output = new Output();
			output.response = "";
			output.error = "";
			output.root = "";
			output.path = "";
			output.resource = "";
			output.site = "";
			output.provider = provider;
		}

		public void CMD_pages()
		{
//			Set_Root(SiteInitializer.CurrentFrontendRootNodeId);
			Set_Root(siteId);
			Set_Path("Pages");
			Set_Resource("pages");
			Set_Provider("");
		}

		public void CMD_bpages()
		{
			Set_Root(SiteInitializer.BackendRootNodeId);
			Set_Path("Backend Pages");
			Set_Resource("bpages");
			Set_Provider("");
		}

		public void CMD_errors()
		{
			Set_Root(Guid.Empty);
			Set_Path("Errors");
			Set_Resource("errors");
			Set_Provider("");
		}

		public void CMD_documents()
		{
			Set_Root(Guid.Empty);
			Set_Path("Documents");
			Set_Resource("documents");
			Set_Provider("");
		}

		public void CMD_images()
		{
			Set_Root(Guid.Empty);
			Set_Path("Images");
			Set_Resource("images");
			Set_Provider("");
		}
		public void CMD_videos()
		{
			Set_Root(Guid.Empty);
			Set_Path("Videos");
			Set_Resource("videos");
			Set_Provider("");
		}

		public void CMD_sitesync()
		{
			Set_Root(Guid.Empty);
			Set_Path("SiteSync");
			Set_Resource("sitesync");
			Set_Provider("");
		}

		public void CMD_all()
		{
			Set_Root(Guid.Empty);
			Set_Path("All");
			Set_Resource("all");
			Set_Provider("");
		}

		public void CMD_site(Resource rsc, Arguments args)
		{
			MultisiteManager siteMgr = MultisiteManager.GetManager();

			if (args.Count > 0)
			{
				try
				{
					siteId = new Guid(args.FirstKey);
					var site = siteMgr.GetSites().Where(s => s.SiteMapRootNodeId == siteId);
					Set_Site(siteId);
					rsc.Root();
				}
				catch (Exception)
				{
					Set_Error("Invalid site Id: " + args);
				}
			}

			output.response = string.Join(
				"\n",
				siteMgr.GetSites().Select<Telerik.Sitefinity.Multisite.Model.Site, string>(s => s.SiteMapRootNodeId + " - " + s.Name + (siteId == s.SiteMapRootNodeId ? " <=" : "")));
		}

		public static List<SyncItem> CMD_sitesync_dest()
		{
			SiteSyncResource rsc = new SiteSyncResource(null);
			return rsc.CMD_sitesync_dest();
		}

		public void Set_Result(Resource rsc) { output.response = rsc.Serialize_Result(); }
		public void Set_Error(string error) { output.error = error; }
		public void Set_Root(Guid root) { output.root = root.ToString(); }
		public void Set_Path(string path) { output.path = path + "> "; }
		public void Set_Resource(string rsc) { output.resource = rsc; }
		public void Set_Site(Guid siteId) { output.site = siteId.ToString(); }
		public Site Get_Site() {
			MultisiteManager siteMgr = MultisiteManager.GetManager();
			Site site = siteMgr.GetSites().Where(s => s.SiteMapRootNodeId == siteId).FirstOrDefault();
			return site;
		}
		public void Set_Provider(string provider) {	output.provider = provider; }
		public string Get_Provider() { return output.provider; }

		public Output Process_Commands()
		{
			Guid rootId;

			try
			{
				rootId = new Guid(root);
			}
			catch (Exception)
			{
				rootId = SiteInitializer.CurrentFrontendRootNodeId;
			}

			// Finds the right resource object
			Resource rsc;
			switch (rscName)
			{
				case "errors":
					rsc = new ErrorResource(this) as Resource;
					break;
				case "bpages":
					rsc = new BackendPageResource(this) as Resource;
					break;
				case "documents":
					rsc = new DocResource(this) as Resource;
					break;
				case "images":
					rsc = new ImageResource(this) as Resource;
					break;
				case "videos":
					rsc = new VideoResource(this) as Resource;
					break;
				case "sitesync":
					rsc = new SiteSyncResource(this) as Resource;
					break;
				case "all":
					rsc = new AllResource(this) as Resource;
					break;
				case null:
				case "pages":
				default:
					rsc = new FrontendPageResource(this) as Resource;
					break;
			}

			// Goes through all the commands
			foreach (string cmd in commands.Split(','))
			{
				// Finds the command and the arguments (when any)
				string command = cmd.Trim().ToLower();
				string firstWord = command.Split(' ').First();
				command = command.Substring(firstWord.Length).TrimStart();
				Arguments args = new Arguments(command);

				// Executes the command
				switch (firstWord)
				{
					case "l":
					case "list":
						rsc.CMD_list(args, rootId);
						break;
					case "filter":
						rsc.CMD_filter(args);
						break;
					case "update":
						rsc.CMD_update(args);
						break;
					case "cd":
						rsc.CMD_cd(args, rootId);
						break;
					case "touch":
						rsc.CMD_touch();
						break;
					case "bpages":
						CMD_bpages();
						break;
					case "pages":
						CMD_pages();
						break;
					case "errors":
						CMD_errors();
						break;
					case "docs":
					case "documents":
						CMD_documents();
						break;
					case "images":
						CMD_images();
						break;
					case "videos":
						CMD_videos();
						break;
					case "sitesync":
						CMD_sitesync();
						break;
					case "all":
						CMD_all();
						break;
					case "site":
						CMD_site(rsc, args);
						return output;
					case "summary":
						rsc.CMD_summary(args);
						break;
					case "help":
						rsc.CMD_help();
						break;
					case "display":
						rsc.CMD_display(args);
						break;
					case "compare":
						rsc.CMD_compare(args);
						break;
					case "republish":
						rsc.CMD_republish(args);
						break;
					case "permissions":
						rsc.CMD_permissions(args);
						break;
					default:
						Set_Error("Invalid keyword: " + firstWord);
						Set_Result(rsc);
						return output;
				}
			}

			Set_Result(rsc);

			return output;
		}

	}
}