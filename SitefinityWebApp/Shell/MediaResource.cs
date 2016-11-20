using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.Multisite.Model;

namespace SitefinitySupport.Shell
{
	public class MediaTree
	{
		public IFolder root;
		bool filterIn;
		public List<MediaContent> items;
		public List<MediaTree> folders;
		protected LibrariesManager libMgr;
		public Dictionary<string, string> providers;

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
				items = libMgr.GetChildItems(root).Where(i => i.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Live && i.Parent.Id == root.Id && i.FolderId == null).ToList();
				folders = libMgr.GetChildFolders(root).Where(f => f.ParentId == null).Select<IFolder, MediaTree>(f => new MediaTree(f, level-1, libMgr)).ToList();
			}
			else
			{
				items = libMgr.GetChildItems(root).Where(i => i.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Live && i.FolderId == root.Id).ToList();
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
				result += "\n";

				result += folder.Print(level + 1);
			}

			foreach (MediaContent item in items)
			{
				result += item.Id.ToString() + " - " + tab;
				string itemName = item.Title;
//				if (!filterIn) itemName = "(" + string.Join(" ", itemName.Select<Char, String>(c => c.ToString())) + ")";
				result += itemName;
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
				"republish: republishes the documents/images/videos\n";

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

		public override string Serialize_Result()
		{
			if (summary != null) return summary;
			if (root == null) return "";

			return root.Print();
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