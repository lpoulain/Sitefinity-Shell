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
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Configuration;
using Telerik.Sitefinity.Security.Model;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Versioning;

namespace SitefinitySupport.Shell
{
	public class PageTree
	{
		public PageNode root;
		public bool filterIn;
		public List<PageTree> children;
		public int permissionGroup;

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

		public bool Filter(Func<PageTree, bool> filter)
		{
			if (root != null) filterIn = filter(this);
			children = children.Where(p => p.Filter(filter)).ToList();

			return (filterIn || children.Count > 0);
		}

		public void Update(Action<PageTree> action) {
			if (root != null && filterIn) action(this);
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

				if (display.Contains("permissions"))
				{
					if (permissionGroup == 0) result += " - Inherits permissions";
					else result += " - Permission group #" + permissionGroup.ToString();
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
		Dictionary<int, IQueryable<Telerik.Sitefinity.Security.Model.Permission>> group2Permissions;

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
			List<Func<PageTree, bool>> filters = new List<Func<PageTree, bool>>();
			VersionManager versionMgr = VersionManager.GetManager();

			if (args.ContainsKey("requiressl"))
			{
				if (args["requiressl"] == "true")
					filters.Add(p => p.root.RequireSsl);
				if (args["requiressl"] == "false")
					filters.Add(p => !p.root.RequireSsl);
			}
			
			if (args.ContainsKey("nbversions"))
			{
				int nbVersions = int.Parse(args["nbversions"]);
				filters.Add(p => p.root.GetPageData() != null && versionMgr.GetItemVersionHistory(p.root.GetPageData().Id).Count > nbVersions);
			}
			
			if (args.ContainsKey("cache"))
			{
				string cacheName = args["cache"];
				if (cacheName == "site") cacheName = "";

				if (cacheName.EndsWith("*"))
					filters.Add(p => p.root.GetPageData() != null &&
									 p.root.GetPageData().OutputCacheProfile.ToLower().StartsWith(cacheName.TrimEnd('*')));
				else
					filters.Add(p => p.root.GetPageData() != null &&
									 p.root.GetPageData().OutputCacheProfile.ToLower() == cacheName);
			}
			
			if (args.ContainsKey("template"))
			{
				string templateName = args["template"];

				if (templateName.EndsWith("*"))
					filters.Add(p => p.root.GetPageData() != null &&
									 p.root.GetPageData().Template.Name.ToLower().StartsWith(templateName.TrimEnd('*')));
				else
					filters.Add(p => p.root.GetPageData() != null &&
									 p.root.GetPageData().Template.Name.ToLower() == templateName);
			}

			if (args.ContainsKey("inheritspermissions"))
			{
				string val = args["inheritspermissions"];
				if (val == "true")
					filters.Add(p => p.root.InheritsPermissions);
				else
					filters.Add(p => !p.root.InheritsPermissions);
			}

			if (filters.Count == 0) return;

			Func<PageTree, bool> filter = p => filters.All(predicate => predicate(p));
			pages.Filter(filter);
		}

		public override void CMD_update(Arguments args)
		{
			Action<PageTree> action = null;
			VersionManager versionMgr = VersionManager.GetManager();
			bool versionMgrSave = false;

			if (args.ContainsKey("requiressl")) {
				if (args["requiressl"] == "true")
					action = p => p.root.RequireSsl = true;
				else if (args["requiressl"] == "false")
					action = p => p.root.RequireSsl = false;
			}
			else if (args.ContainsKey("nbversions"))
			{
				int nbVersions = int.Parse(args["nbversions"]);
				versionMgrSave = true;
				action = p =>
				{
					PageData paged = p.root.GetPageData();
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
					if (p.root.GetPageData() == null) return;
					p.root.GetPageData().OutputCacheProfile = exactCacheName;
				};

			}
			else if (args.ContainsKey("inheritspermissions"))
			{
				string val = args["inheritspermissions"];
				if (val == "true")
					action = p => pageMgr.RestorePermissionsInheritance(p.root);
				if (val == "false")
					action = p => pageMgr.BreakPermiossionsInheritance(p.root);
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

		public string PermissionText(int actions, string label)
		{
			string result = "";

			if ((actions & 1) != 0) result += "View ";
			if ((actions & 2) != 0) result += "AddWidget ";
			if ((actions & 4) != 0) result += "EditContent ";
			if ((actions & 8) != 0) result += "CreateChildPages ";
			if ((actions & 16) != 0) result += "ModifyProperties ";
			if ((actions & 32) != 0) result += "Delete ";
			if ((actions & 64) != 0) result += "ChangeOwner ";
			if ((actions & 128) != 0) result += "ChangePermissions ";

			if (result != "") return label + result;
			return "";
		}

		public void FindPermissions()
		{
			var permissionPages = pageMgr.GetPermissions().Where(p => p.SetName == "Pages");

			Dictionary<string, int> permissionSig2Group = new Dictionary<string, int>();
			group2Permissions = new Dictionary<int, IQueryable<Telerik.Sitefinity.Security.Model.Permission>>();
			int nbGroups = 1;

			Action<PageTree> action = pt => {
				// If the PageTree was filtered out, ignore it
				if (!pt.filterIn) return;

				// If the PageNode inherits permissions => Group #0
				if (pt.root.InheritsPermissions)
				{
					pt.permissionGroup = 0;
					return;
				}


				var permissions = permissionPages.Where(p => p.ObjectId == pt.root.Id && (p.Grant > 0 || p.Deny > 0))
												 .OrderBy(p => p.PrincipalId);
				
				// Builds a signature unique to the permissions for that Page				 
				string sig = string.Join("\n", permissions.Select(p => string.Format("{0}|{1}|{2}", p.PrincipalId, p.Grant, p.Deny)));

				// Another page has the same permission signature. Reuse the group
				if (permissionSig2Group.ContainsKey(sig)) {
					pt.permissionGroup = permissionSig2Group[sig];
					return;
				}

				// New group
				permissionSig2Group.Add(sig, nbGroups);
				group2Permissions.Add(nbGroups, permissions);
				pt.permissionGroup = nbGroups++;
			};

			// Sets the Permission Group # in the PageTree objects
			pages.Update(action);
		}

		public string PrintPermissionGroups()
		{
			RoleManager roleManager = RoleManager.GetManager(SecurityManager.ApplicationRolesProviderName);
			UserManager userManager = UserManager.GetManager();

			var roles = roleManager.GetRoles();
			var users = userManager.GetUsers();

			string summary = "\n\n";
			int nbGroups = group2Permissions.Count() + 1;

			for (int groupNb=1; groupNb < nbGroups; groupNb++)
			{
				summary += string.Format("Permission Group #{0}\n", groupNb);
				foreach (var permission in group2Permissions[groupNb])
				{
					string principalName = permission.PrincipalId.ToString();

					var role = roles.Where(r => r.Id == permission.PrincipalId).FirstOrDefault();
					if (role != null)
						principalName = "[" + role.Name + "]";
					else
					{
						var user = users.Where(u => u.Id == permission.PrincipalId).FirstOrDefault();
						if (user != null) principalName = user.UserName;
					}
					summary += string.Format("- {0}: {1} {2}\n", principalName, PermissionText(permission.Grant, "GRANT "), PermissionText(permission.Deny, "DENY "));
				}
				summary += "\n";
			}

			return summary;
		}

		public override string Serialize_Result()
		{
			if (summary != null) return summary;

			if (pages == null) return "";

			if (!display.Contains("permissions"))
				return pages.Print(display).TrimEnd();

			FindPermissions();
			summary = pages.Print(display).TrimEnd();
			summary += PrintPermissionGroups();

			return summary;
		}

		public override void CMD_help()
		{
			summary =
				"list: displays the pages in the current folder\n" +
				"list all: displays the pages and their subpages\n" +
				"filter [requireSSL|nbversions|cache|template]=<value>: filters pages\n" +
				"display [id] [requiSSL] [cache] [template] [permissons]: sets the fields to display in the results\n" +
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

			Action<PageTree> action = p => p.root.Title = p.root.Title + "";
			pages.Update(action);
			pageMgr.SaveChanges();
		}

		public override void CMD_republish(Arguments args)
		{
			if (pages == null) return;

			Action<PageTree> action = p =>
			{
				if (p.root.NodeType == NodeType.Group || p.root.NodeType == NodeType.InnerRedirect || p.root.NodeType == NodeType.OuterRedirect)
					p.root.Title = p.root.Title.Trim();
				else
				{
					var pageData = p.root.GetPageData();
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
				"list all, filter template=bootstrap* requiressl=false, update requireSSL=true\n" +
				"list all, permissions\n";

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