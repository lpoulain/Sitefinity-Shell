using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.Multisite.Model;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Model;
using Telerik.Sitefinity.Versioning;

namespace SitefinitySupport.Shell
{
	public class MediaTree
	{
		public IFolder root;
		public bool filterIn;
		public List<MediaContent> items;
		public List<MediaTree> folders;
		public LibrariesManager libMgr;
		public Dictionary<string, string> providers;
		public Dictionary<Guid, int> permissionGroup;

		// Looking inside a folder
		public MediaTree(IFolder folder, int level, LibrariesManager libMgr)
		{
			this.root = folder;
			this.libMgr = libMgr;
			this.filterIn = true;
			if (level <= 0)
			{
				items = new List<MediaContent>();
				folders = new List<MediaTree>();
				return;
			}

			if (this.root is Library)
			{
				items = libMgr.GetChildItems(root).Where(i => i.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Live &&
															  i.Parent.Id == root.Id &&
															  i.FolderId == null &&
															  i.Visible).ToList();
				folders = libMgr.GetChildFolders(root).Where(f => f.ParentId == null).Select<IFolder, MediaTree>(f => new MediaTree(f, level-1, libMgr)).ToList();
			}
			else
			{
				items = libMgr.GetChildItems(root).Where(i => i.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Live &&
															  i.FolderId == root.Id &&
															  i.Visible).ToList();
				folders = libMgr.GetChildFolders(root).Where(f => f.ParentId == root.Id).Select<IFolder, MediaTree>(f => new MediaTree(f, level - 1, libMgr)).ToList();
			}
		}

		// Top-level: go through all the Libraries for all the providers
		public MediaTree(MediaResource rsc, Site site, int level)
		{
			items = new List<MediaContent>();
			LibrariesManager libMgr;

			// Single site
			if (site == null)
			{
				libMgr = LibrariesManager.GetManager();
				folders = rsc.GetLibraries().Select<Library, MediaTree>(lib => new MediaTree(lib, level - 1, libMgr)).ToList();
			}
			// Multisite: we need to go through all the providers from the site
			else
			{
				var links = site.SiteDataSourceLinks.Where(s => s.DataSourceName == "Telerik.Sitefinity.Modules.Libraries.LibrariesManager");
				folders = new List<MediaTree>();

				foreach (var link in links)
				{
					libMgr = LibrariesManager.GetManager(link.ProviderName);
					folders.AddRange(rsc.GetLibraries(libMgr).Select<Library, MediaTree>(lib => new MediaTree(lib, level - 1, libMgr)).ToList());
				}
			}
		}

		public bool Filter(Func<MediaContent, bool> filter)
		{
			items = items.Where(i => filter(i)).ToList();
			folders = folders.Where(p => p.Filter(filter)).ToList();

			return (filterIn || items.Count > 0);
		}

		public void Update(Action<MediaTree> action)
		{
			action(this);
			foreach (MediaTree folder in folders) folder.Update(action);
		}

		public string Print(int level = 0)
		{
			var providers =
				level == 0 ?
				Telerik.Sitefinity.Configuration.Config.Get<Telerik.Sitefinity.Modules.Libraries.Configuration.LibrariesConfig>().Providers :
				null;

			string result = "";
			string tab = new String('.', level * 4);

			foreach (MediaTree folder in folders)
			{
				result += folder.root.Id.ToString() + " - " + tab;
				string itemName;

				if (folder.root is Library) {
					string providerName = (folder.root as Library).GetProviderName();
					string providerTitle = providers[providerName].Title;
					itemName = "[" + providerTitle + " - " + folder.root.Title + "]";
				}
				else itemName = "[" + folder.root.Title + "]";

				result += itemName;

				if (folder.permissionGroup != null && folder.permissionGroup.ContainsKey(folder.root.Id))
				{
					int group = folder.permissionGroup[folder.root.Id];
					if (group == 0) result += " - Inherits permissions";
					else result += " - Permission group #" + group.ToString();
				}

				result += "\n";

				result += folder.Print(level + 1);
			}

			foreach (MediaContent item in items)
			{
				result += item.Id.ToString() + " - " + tab;
				string itemName = item.Title;
//				if (!filterIn) itemName = "(" + string.Join(" ", itemName.Select<Char, String>(c => c.ToString())) + ")";
				result += itemName;

				if (permissionGroup != null && permissionGroup.ContainsKey(item.Id))
				{
					int group = permissionGroup[item.Id];
					if (group == 0) result += " - Inherits permissions";
					else result += " - Permission group #" + group.ToString();
				}

				result += "\n";
			}

			return result;
		}
	}

	public class MediaResource : Resource
	{
		protected LibrariesManager libMgr;
		protected string resourceName;
		protected MediaTree root;
		protected IQueryable<IFolder> folders;
		protected IQueryable<MediaContent> items;

		public MediaResource(IShellService theSvc, string name)
			: base(theSvc, name)
		{
			libMgr = LibrariesManager.GetManager(theSvc.Get_Provider());
			resourceName = name;
			folders = null;
			items = null;
		}

		public override void CMD_help()
		{
			summary =
				"cd <id>: go the the Library/Folder\n" +
				"list: lists all the folders and media content in the current folder\n" +
				"republish: republishes the documents/images/videos\n" +
				"update nbversions=<nb>: deletes older revisions beyond <nb>\n";

			base.CMD_help();
		}

		public virtual IQueryable<Library> GetLibraries() {
			return null;
		}

		public virtual IQueryable<Library> GetLibraries(LibrariesManager libMgr)
		{
			return null;
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			int level = args.ContainsKey("all") ? 100 : 1;

			// Top level - display the libraries for all the providers
			if (rootId == Guid.Empty)
			{
				MultisiteManager siteMgr = MultisiteManager.GetManager();
				Site site = svc.Get_Site();
				root = new MediaTree(this, site, level);
			}
			else
			{
				IFolder folder = libMgr.GetFolder(rootId);
				root = new MediaTree(folder, level, libMgr);
			}
		}

		public override void CMD_cd(Arguments args, Guid rootId)
		{
			if (args.Count == 0)
			{
				svc.Set_Path(resourceName);
				svc.Set_Root(Guid.Empty);
				svc.Set_Provider("");
				return;
			}

			if (args.ContainsKey(".."))
			{
				IFolder folder = null;
				try
				{
					folder = libMgr.GetFolder(rootId);
				}
				catch (Exception)
				{
					return;
				}
				if (folder == null || folder.ParentId == null || folder.ParentId == Guid.Empty)
				{
					svc.Set_Root(Guid.Empty);
					svc.Set_Path(resourceName);
					svc.Set_Provider("");
					return;
				}

				IFolder newFolder = libMgr.GetFolder(folder.ParentId);
				svc.Set_Root(newFolder.Id);
				svc.Set_Path(newFolder.Title);
				return;
			}

			// cd into <args>
			try
			{
				Guid newRootId = new Guid(args.FirstKey);
				Site site = svc.Get_Site();

				// Single site or already inside a Library
				if (site == null || rootId != Guid.Empty)
				{
					IFolder folder = libMgr.GetFolder(newRootId);
					svc.Set_Root(newRootId);
					svc.Set_Path(folder.Title);
					return;

				}

				// Multisite and on the root level
				var links = site.SiteDataSourceLinks.Where(s => s.DataSourceName == "Telerik.Sitefinity.Modules.Libraries.LibrariesManager");

				foreach (var link in links)
				{
					libMgr = LibrariesManager.GetManager(link.ProviderName);

					try
					{
						Library lib = GetLibraries(libMgr).Where(l => l.Id == newRootId).First();
						svc.Set_Root(newRootId);
						svc.Set_Path(lib.Title);
						svc.Set_Provider(link.ProviderName);
						return;
					}
					catch (Exception) { }
				}
			}
			catch (Exception) { }

			svc.Set_Error("Invalid path: " + args.FirstKey);
		}

		public override void CMD_republish(Arguments args)
		{
			if (root == null) return;

			Action<MediaTree> action = t =>
			{
				t.root.Title = t.root.Title.Trim();

				foreach (MediaContent item in t.items)
				{
					var master = libMgr.Lifecycle.GetMaster(item);
					var temp = libMgr.Lifecycle.CheckOut(master) as MediaContent;
					temp.Title = temp.Title.Trim();
					master = libMgr.Lifecycle.CheckIn(temp) as MediaContent;
					libMgr.Lifecycle.Publish(master);
				}
			};

			root.Update(action);
			libMgr.SaveChanges();
		}

		public override void CMD_update(Arguments args)
		{
			VersionManager versionMgr = VersionManager.GetManager();
			bool versionMgrSave = false;
			Action<MediaTree> action = null;

			if (args.ContainsKey("nbversions"))
			{
				int nbVersions = int.Parse(args["nbversions"]);
				versionMgrSave = true;
				action = t =>
				{
					foreach (MediaContent item in t.items)
					{
						var changes = versionMgr.GetItemVersionHistory(item.OriginalContentId);
						var changeToRemove = changes
							.OrderByDescending(c => c.Version)
							.Skip(nbVersions)
							.FirstOrDefault();

						if (changeToRemove != null)
						{
							// Delete all changes with version number smaller or equal to the specified number
							versionMgr.TruncateVersions(item.OriginalContentId, changeToRemove.Version);
						}
					}
				};
			}

			if (action != null)
			{
				root.Update(action);
				libMgr.SaveChanges();
				if (versionMgrSave) versionMgr.SaveChanges();
			}
		}

		public void FindPermissions()
		{
			Dictionary<string, int> permissionSig2Group = new Dictionary<string, int>();
			group2Permissions = new Dictionary<int, IQueryable<Telerik.Sitefinity.Security.Model.Permission>>();
			int nbGroups = 1;

			Action<MediaTree> action = mt =>
			{
				// If the MediaTree was filtered out, ignore it
				if (!mt.filterIn) return;

				mt.permissionGroup = new Dictionary<Guid, int>();
				List<ISecuredObject> allitems = new List<ISecuredObject>();
				allitems.AddRange(mt.items);
				if (mt.root is Library) allitems.Add(mt.root as Library);

				IQueryable<Telerik.Sitefinity.Security.Model.Permission> mediaPermissions = mt.libMgr.GetPermissions();

				foreach (ISecuredObject item in allitems)
				{
					// If the item inherits permissions => Group #0
					if (item.InheritsPermissions)
					{
						mt.permissionGroup.Add(item.Id, 0);
						continue;
					}

					var permissions = mediaPermissions.Where(p => p.ObjectId == item.Id && (p.Grant > 0 || p.Deny > 0))
													 .OrderBy(p => p.PrincipalId);

					// Builds a signature unique to the permissions for that Page				 
					string sig = string.Join("\n", permissions.Select(p => string.Format("{0}|{1}|{2}", p.PrincipalId, p.Grant, p.Deny)));

					// Another page has the same permission signature. Reuse the group
					if (permissionSig2Group.ContainsKey(sig))
					{
						mt.permissionGroup.Add(item.Id, permissionSig2Group[sig]);
						continue;
					}

					// New group
					permissionSig2Group.Add(sig, nbGroups);
					group2Permissions.Add(nbGroups, permissions);
					mt.permissionGroup.Add(item.Id, nbGroups++);
				}

			};

			// Sets the Permission Group # in the PageTree objects
			root.Update(action);
		}

		public override string PermissionText(int actions, string label)
		{
			string result = "";

			if ((actions & 1) != 0) result += "View" + label + " ";
			if ((actions & 2) != 0) result += "Modify" + label + " ";
			if ((actions & 4) != 0) result += "ChangeOwner" + label + " ";
			if ((actions & 8) != 0) result += "ChangePermissions" + label + " ";

			return result;
		}

		public string GetPrincipalName(Guid principalId, IQueryable<Role> roles, IQueryable<User> users)
		{
			string principalName = principalId.ToString();

			var role = roles.Where(r => r.Id == principalId).FirstOrDefault();
			if (role != null)
				principalName = "[" + role.Name + "]";
			else
			{
				var user = users.Where(u => u.Id == principalId).FirstOrDefault();
				if (user != null) principalName = user.UserName;
			}

			return principalName;
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
				string grant = "", deny = "";
				Guid principalId = Guid.Empty;

				foreach (var permission in group2Permissions[groupNb])
				{
					if (principalId != permission.PrincipalId)
					{
						if (principalId != Guid.Empty)
						{
							if (grant != "") grant = "GRANT " + grant;
							if (deny != "") grant = "DENY " + grant;
							summary += string.Format("- {0}: {1} {2}\n", GetPrincipalName(principalId, roles, users), grant, deny);

							grant = "";
							deny = "";
						}
						principalId = permission.PrincipalId;
					}
					
					string rsc = permission.SetName.Contains("Library") ? "Library" : "Media";
					grant += PermissionText(permission.Grant, rsc);
				}

				if (principalId != Guid.Empty)
				{
					if (grant != "") grant = "GRANT " + grant;
					if (deny != "") grant = "DENY " + grant;
					summary += string.Format("- {0}: {1} {2}\n", GetPrincipalName(principalId, roles, users), grant, deny);
				}

				summary += "\n";
			}

			return summary;
		}

		public override string Serialize_Result()
		{
			if (summary != null) return summary;
			if (root == null) return "";

			FindPermissions();
			summary = root.Print().TrimEnd();
			summary += PrintPermissionGroups();

			return summary;
		}
	}

	public class DocResource : MediaResource
	{
		public DocResource(IShellService theSvc)
			: base(theSvc, "Documents")
		{ }

		public override IQueryable<Library> GetLibraries()
		{
			return libMgr.GetDocumentLibraries();
		}

		public override IQueryable<Library> GetLibraries(LibrariesManager libMgr)
		{
			return libMgr.GetDocumentLibraries();
		}
	}

	public class ImageResource : MediaResource
	{
		public ImageResource(IShellService theSvc)
			: base(theSvc, "Images")
		{ }

		public override IQueryable<Library> GetLibraries()
		{
			return libMgr.GetAlbums();
		}

		public override IQueryable<Library> GetLibraries(LibrariesManager libMgr)
		{
			return libMgr.GetAlbums();
		}
	}

	public class VideoResource : MediaResource
	{
		public VideoResource(IShellService theSvc)
			: base(theSvc, "Videos")
		{ }

		public override IQueryable<Library> GetLibraries()
		{
			return libMgr.GetVideoLibraries();
		}

		public override IQueryable<Library> GetLibraries(LibrariesManager libMgr)
		{
			return libMgr.GetVideoLibraries();
		}
	}

}