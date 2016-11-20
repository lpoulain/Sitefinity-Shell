using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Pages.Model;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Versioning;

namespace SitefinitySupport.Shell
{
	public class PageTree
	{
		public PageNode root;
		bool filterIn;
		public List<PageTree> children;

		// If pageMgr is passed => recursive build
		public PageTree(PageNode n, PageManager pageMgr = null)
		{
			root = n;
			filterIn = true;
			if (pageMgr != null)
				children = pageMgr.GetPageNodes().Where(p => p.ParentId == root.Id).Select<PageNode, PageTree>(pn => new PageTree(pn, pageMgr)).ToList();
			else
				children = new List<PageTree>();
		}

		// If pagrMgr is passed => recursive build
		public PageTree(IQueryable<PageNode> nodes, PageManager pageMgr = null)
		{
			root = null;
			filterIn = true;
			children = nodes.Select<PageNode, PageTree>(n => new PageTree(n, pageMgr)).ToList();
		}

		public bool Filter(Func<PageNode, bool> filter)
		{
			if (root != null) filterIn = filter(root);
			children = children.Where(p => p.Filter(filter)).ToList();

			return (filterIn || children.Count > 0);
		}

		public void Update(Action<PageNode> action) {
			if (root != null && filterIn) action(root);
			foreach (PageTree child in children)
				child.Update(action);
		}

		public string Print(HashSet<string> display, int level=0)
		{
			string result = "";
			string tab = new String('.', level * 4);

			if (root != null)
			{
				if (display.Contains("id")) result += root.Id.ToString() + " - ";
				
				result += tab;
				string itemName = (root.NodeType == NodeType.Group ? "[" + (root.Name.IsNullOrEmpty() ? root.Title.ToString() : root.Name) + "]" : (root.Name.IsNullOrEmpty() ? root.Title.ToString() : root.Name));
				if (!filterIn) itemName = "(" + string.Join(" ", itemName.Select<Char, String>(c => c.ToString())) + ")";
				result += itemName;

				PageData pdata = root.GetPageData();

				if (pdata != null)
				{
					if (display.Contains("template")) result += " - " + pdata.Template.Name;
					if (display.Contains("requiressl")) result += " - " + pdata.RequireSsl;
					if (display.Contains("cache")) result += " - " + (pdata.OutputCacheProfile == "" ? "site" : pdata.OutputCacheProfile);
				}

				result += "\n";
			}
			else level = -1;

			foreach (PageTree p in children)
				result += p.Print(display, level+1);

			return result;
		}
	}

	// Class used for frontend and backend pages
	public class PageResource : Resource
	{
		protected PageManager pageMgr;
		protected PageTree pages;

		public PageResource(IShellService theSvc, string name)
			: base(theSvc, name)
		{
			this.pageMgr = PageManager.GetManager();
			this.display = new HashSet<string>() { "id" };
		}

		protected void Add_Pages(PageNode parentPage, List<PageNode> result, bool recursive=false)
		{
			foreach (PageNode page in pageMgr.GetPageNodes().Where(p => p.ParentId == parentPage.Id))
			{
				result.Add(page);
				Add_Pages(page, result);
			}
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			pages = null;

			// Go through all the pages recursively
			if (args.ContainsKey("all"))
			{
				PageNode page = pageMgr.GetPageNode(rootId);
				pages = new PageTree(pageMgr.GetPageNodes().Where(p => p.ParentId == page.Id && !p.IsDeleted), pageMgr);
			}
			// Just get the pages whose direct parent is the root
			else
			{
				pages = new PageTree(pageMgr.GetPageNodes().Where(p => p.ParentId == rootId && !p.IsDeleted));
			}
		}

		public override void CMD_filter(Arguments args)
		{
			List<Func<PageNode, bool>> filters = new List<Func<PageNode, bool>>();
			VersionManager versionMgr = VersionManager.GetManager();

			if (args.ContainsKey("requiressl"))
			{
				if (args["requiressl"] == "true")
					filters.Add(p => p.RequireSsl);
				if (args["requiressl"] == "false")
					filters.Add(p => !p.RequireSsl);
			}
			
			if (args.ContainsKey("nbversions"))
			{
				int nbVersions = int.Parse(args["nbversions"]);
				filters.Add(p => p.GetPageData() != null && versionMgr.GetItemVersionHistory(p.GetPageData().Id).Count > nbVersions);
			}
			
			if (args.ContainsKey("cache"))
			{
				string cacheName = args["cache"];
				if (cacheName == "site") cacheName = "";

				if (cacheName.EndsWith("*"))
					filters.Add(p => p.GetPageData() != null &&
									 p.GetPageData().OutputCacheProfile.ToLower().StartsWith(cacheName.TrimEnd('*')));
				else
					filters.Add(p => p.GetPageData() != null &&
									 p.GetPageData().OutputCacheProfile.ToLower() == cacheName);
			}
			
			if (args.ContainsKey("template"))
			{
				string templateName = args["template"];

				if (templateName.EndsWith("*"))
					filters.Add(p => p.GetPageData() != null &&
									 p.GetPageData().Template.Name.ToLower().StartsWith(templateName.TrimEnd('*')));
				else
					filters.Add(p => p.GetPageData() != null &&
									 p.GetPageData().Template.Name.ToLower() == templateName);
			}

			if (filters.Count == 0) return;

			Func<PageNode, bool> filter = p => filters.All(predicate => predicate(p));
			pages.Filter(filter);
		}

		public override void CMD_update(Arguments args)
		{
			Action<PageNode> action = null;
			VersionManager versionMgr = VersionManager.GetManager();
			bool versionMgrSave = false;

			if (args.ContainsKey("requiressl")) {
				if (args["requiressl"] == "true")
					action = p => p.RequireSsl = true;
				else if (args["requiressl"] == "false")
					action = p => p.RequireSsl = false;
			}
			else if (args.ContainsKey("nbversions"))
			{
				int nbVersions = int.Parse(args["nbversions"]);
				versionMgrSave = true;
				action = p =>
				{
					PageData paged = p.GetPageData();
					if (paged == null) return;
					var changes = versionMgr.GetItemVersionHistory(paged.Id);
					var changeToRemove = changes
						.OrderByDescending(c => c.Version)
						.Skip(nbVersions)
						.FirstOrDefault();

					if (changeToRemove != null)
					{
						// Delete all changes with version number smaller or equal to the specified number
						versionMgr.TruncateVersions(paged.Id, changeToRemove.Version);
					}
				};
			}
			else if (args.ContainsKey("cache"))
			{
				string cacheName = args["cache"];
				string exactCacheName = null;
				if (cacheName == "site") exactCacheName = "";
				else
				{
					SystemConfig config = Config.Get<SystemConfig>();
					var profiles = config.CacheSettings.Profiles;
					exactCacheName = profiles.Keys.FirstOrDefault(k => k.ToLower() == cacheName);
				}
				if (exactCacheName == null) return;

				action = p =>
				{
					if (p.GetPageData() == null) return;
					p.GetPageData().OutputCacheProfile = exactCacheName;
				};

			}

			if (action != null)
			{
				pages.Update(action);
				pageMgr.SaveChanges();
				if (versionMgrSave) versionMgr.SaveChanges();
			}
		}

		public override void CMD_cd(Arguments args, Guid rootId)
		{
			if (args.Count == 0)
			{
				PageNode page = pageMgr.GetPageNode(rootId);
				if (page.RootNodeId == SiteInitializer.CurrentFrontendRootNodeId) svc.CMD_pages();
				else if (page.RootNodeId == SiteInitializer.BackendRootNodeId) svc.CMD_bpages();

				return;
			}

			if (args.ContainsKey(".."))
			{
				PageNode page = pageMgr.GetPageNode(rootId);
				if (page.ParentId == null || page.ParentId == Guid.Empty) return;
				page = pageMgr.GetPageNode(page.ParentId);
				svc.Set_Root(page.Id);
				svc.Set_Path(page.Title);
				return;
			}

			try
			{
				Guid newRootId = new Guid(args.FirstKey);
				PageNode page = pageMgr.GetPageNode(newRootId);
				svc.Set_Root(newRootId);
				svc.Set_Path(page.Title);
				return;
			}
			catch (Exception) { }

			svc.Set_Error("Invalid path: " + args.FirstKey);
		}

		public override string Serialize_Result()
		{
			if (summary != null) return summary;

			if (pages == null) return "";
			return pages.Print(display).TrimEnd();
		}

		public override void CMD_help()
		{
			summary =
				"list: displays the pages in the current folder\n" +
				"list all: displays the pages and their subpages\n" +
				"filter [requireSSL|nbversions|cache|template]=<value>: filters pages\n" +
				"display [id] [requiSSL] [cache] [template]: sets the fields to display in the results\n" +
				"cd <id>: goes the pages under <id>\n" +
				"update [requireSSL|nbversions|cache|template]=<value>: modifies a field\n";
		}

		public void CMD_help_end()
		{
			base.CMD_help();
		}

	}

	public class FrontendPageResource : PageResource
	{
		public FrontendPageResource(IShellService svc) : base(svc, "Pages") { }

		public override void CMD_touch()
		{
			if (pages == null) return;

			Action<PageNode> action = p => p.Title = p.Title + "";
			pages.Update(action);
			pageMgr.SaveChanges();
		}

		public override void CMD_republish(Arguments args)
		{
			if (pages == null) return;

			Action<PageNode> action = p =>
			{
				if (p.NodeType == NodeType.Group || p.NodeType == NodeType.InnerRedirect || p.NodeType == NodeType.OuterRedirect)
					p.Title = p.Title.Trim();
				else
				{
					var pageData = p.GetPageData();
					if (pageData == null) return;

					var pageEdit = pageMgr.PagesLifecycle.Edit(pageData);
					var master = pageMgr.PagesLifecycle.GetMaster(pageEdit);
					var temp = pageMgr.PagesLifecycle.CheckOut(master);
					temp.ApplicationName = temp.ApplicationName.Trim();
					master = pageMgr.PagesLifecycle.CheckIn(temp);
					pageMgr.PagesLifecycle.Publish(master);
				}
			};

			pages.Update(action);
			pageMgr.SaveChanges();
		}

		public override void CMD_help()
		{
			base.CMD_help();

			summary +=
				"touch: updates the pages, changing its last modified date\n" +
				"republish: republishes the pages\n" +
				"\n" +
				"Use a comma to chain multiple commands, e.g.\n" +
				"list all, filter template=bootstrap* requiressl=false, update requireSSL=true\n";

			base.CMD_help_end();
		}

		public override void Root()
		{
			svc.CMD_pages();
		}
	}

	// Because of the sensitivity of backend pages, we don't want users to
	// be able to do anything they could do for frontend pages
	public class BackendPageResource : PageResource
	{
		public BackendPageResource(IShellService svc) : base(svc, "Backend pages") { }

		public override void CMD_help()
		{
			base.CMD_help();

			summary +=
				"\n" +
				"Use a comma to chain multiple commands, e.g.\n" +
				"list all, filter template=right* requireSSL=true, display id template\n" +
				"list, update requireSSL=true\n";

			base.CMD_help_end();
		}

		public override void Root()
		{
			svc.CMD_bpages();
		}
	}
}