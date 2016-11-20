using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity.Blogs.Model;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.Events.Model;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.Lifecycle;
using Telerik.Sitefinity.Lists.Model;
using Telerik.Sitefinity.Modules.Blogs;
using Telerik.Sitefinity.Modules.Events;
using Telerik.Sitefinity.Modules.Forms;
using Telerik.Sitefinity.Modules.GenericContent;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Modules.Lists;
using Telerik.Sitefinity.Modules.News;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.News.Model;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Claims;
using Telerik.Sitefinity.Security.Model;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Pages.Model;
using Telerik.Sitefinity.Taxonomies;
using Telerik.Sitefinity.Taxonomies.Model;
using Telerik.Sitefinity.Utilities.TypeConverters;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.DynamicModules.Builder.Model;
using Telerik.Sitefinity.DynamicModules.Builder;
using Telerik.Sitefinity.Multisite.Model;

namespace SitefinitySupport.Shell
{
	public class AllResource : Resource
	{
		protected Site site;

		public AllResource(IShellService svc)
			: base(svc, "all")
		{

		}
		public override string Serialize_Result()
		{
			if (summary == null) return "";
			return summary;
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			MultisiteManager siteMgr = MultisiteManager.GetManager();

			summary = string.Join("\n", siteMgr.GetSites().Select<Site, string>(s => s.Id + " - " + s.Name));
		}

		public List<string> GetProviders(string type)
		{
			return site.SiteDataSourceLinks
					   .Where(s => s.DataSourceName == type)
					   .Select(dsl => dsl.ProviderName)
					   .ToList();
		}

		public override void CMD_republish(Arguments args)
		{
			ModuleBuilderManager dynMgr = ModuleBuilderManager.GetManager(); ;
			var modules = dynMgr.GetItems(typeof(Telerik.Sitefinity.DynamicModules.Builder.Model.DynamicModule), "", "", 0, 0).Cast<Telerik.Sitefinity.DynamicModules.Builder.Model.DynamicModule>();

			MultisiteManager siteMgr = MultisiteManager.GetManager();
			site = siteMgr.GetSite(new Guid(args.FirstKey));

			var links = site.SiteDataSourceLinks.Where(s => s.DataSourceName == "Telerik.Sitefinity.Modules.Libraries.LibrariesManager");

			// Page Templates
			this.RepublishPageTemplatess();

			// Pages
			var currentFrontendRootNodeId = site.SiteMapRootNodeId;
			this.RepublishPages(currentFrontendRootNodeId);
			this.RepublishGroupPagesForSite(currentFrontendRootNodeId);

			// News items
			var newsItemsproviders = GetProviders(NewsManagerTypeName);
			this.RepublishNewsItems(newsItemsproviders);

			// Blogs and Blog posts
			var blogsProviders = GetProviders(BlogsManagerTypeName);
			this.RepublishBlogs(blogsProviders);
			this.RepublishBlogPosts(blogsProviders);

			// Calendars and Events
			var eventsProviders = GetProviders(EventsManagerTypeName);
			this.RepublishCalendars(eventsProviders);
			this.RepublishEvents(eventsProviders);

			// Libraries, Folders, Images, Documents, Videos
			var librariesProviders = GetProviders(LibrariesManagerTypeName);
			this.RepublishLibrariesAndFolders(librariesProviders);
			this.RepublishImages(librariesProviders);
			this.RepublishDocuments(librariesProviders);
			this.RepublishVideos(librariesProviders);

			// Lists and ListItems
			var listsProviders = GetProviders(ListsManagerTypeName);
			this.RepublishLists(listsProviders);
			this.RepublishListItems(listsProviders);

			// Forms
			var formsProviders = GetProviders(FormsManagerTypeName);
			this.RepublishForms(formsProviders);

			// Content Blocks
			var contentProviders = GetProviders(ContentManagerTypeName);
			this.RepublishContentBlocks(contentProviders);

			// Taxonomies
			foreach (var flatTaxonomyName in FlatTaxonomyNames)
			{
				this.RepublishTaxonomies(flatTaxonomyName, true);
				this.RepublishTaxonItems(flatTaxonomyName, true);
			}

			foreach (var hierarchicalTaxonomyName in HierarchicalTaxonomyNames)
			{
				this.RepublishTaxonomies(hierarchicalTaxonomyName, false);
				this.RepublishTaxonItems(hierarchicalTaxonomyName, false);
			}

			var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
			var theSite = multisiteContext.GetSiteById(site.Id);

			var dynTypes = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleTypes();

			// Dynamic Modules
			foreach (var datalink in site.SiteDataSourceLinks.Where(dsl => !dsl.DataSourceName.StartsWith("Telerik.Sitefinity.")))
			{
				foreach (var dynType in dynTypes.Where(mt => mt.ModuleName == datalink.DataSourceName)) {
					Type type = TypeResolutionService.ResolveType(dynType.GetFullTypeName());
					this.RepublishDynamicContent(datalink.ProviderName, type);
				}
			}
		}

		public override void CMD_help()
		{
			summary =
				"list: displays the sites\n" +
				"republish <site ID>: republish all items for that site\n";

			base.CMD_help();
		}

		private void RepublishPages(Guid frontendRootNodeId)
		{
			var pageManager = PageManager.GetManager();
			pageManager.Provider.SuppressSecurityChecks = true;
			var pages =
                pageManager.GetPageDataList()
					.Where(
						pd =>
							pd.NavigationNode.RootNodeId == frontendRootNodeId && pd.Visible &&
							pd.Status == ContentLifecycleStatus.Live && pd.NavigationNode.NodeType == NodeType.Standard)
					.ToList();
			var count = 0;

			foreach (var pageData in pages)
			{
				var pageEdit = pageManager.PagesLifecycle.Edit(pageData);
				var master = pageManager.PagesLifecycle.GetMaster(pageEdit);
				var temp = pageManager.PagesLifecycle.CheckOut(master);
				temp.ApplicationName = temp.ApplicationName.Trim();
				master = pageManager.PagesLifecycle.CheckIn(temp);
				pageManager.PagesLifecycle.Publish(master);

				count++;
				if (count % 200 == 0)
				{
					pageManager.SaveChanges();
				}
			}

			pageManager.SaveChanges();
			pageManager.Provider.SuppressSecurityChecks = false;
		}

		private void RepublishPageTemplatess()
		{
			var pageManager = PageManager.GetManager();
			pageManager.Provider.SuppressSecurityChecks = true;
			var pageTemplates =
                pageManager.GetTemplates().ToList()
					.Where(pt => !pt.IsBackend && pt.Visible && pt.Status == ContentLifecycleStatus.Live);
			var count = 0;

			foreach (var pageTemplate in pageTemplates)
			{
				var pageTemplateEdit = pageManager.TemplatesLifecycle.Edit(pageTemplate);
				var master = pageManager.TemplatesLifecycle.GetMaster(pageTemplateEdit);
				var temp = pageManager.TemplatesLifecycle.CheckOut(master);
				temp.ApplicationName = temp.ApplicationName.Trim();
				master = pageManager.TemplatesLifecycle.CheckIn(temp);
				pageManager.TemplatesLifecycle.Publish(master);

				count++;
				if (count % 200 == 0)
				{
					pageManager.SaveChanges();
				}
			}

			pageManager.SaveChanges();
			pageManager.Provider.SuppressSecurityChecks = false;
		}

		private void RepublishNewsItems(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var newsManager = NewsManager.GetManager(provider);
				newsManager.Provider.SuppressSecurityChecks = false;
				var newsItems = newsManager.GetNewsItems().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var newsItem in newsItems)
				{
					var master = newsManager.Lifecycle.GetMaster(newsItem);
					var temp = newsManager.Lifecycle.CheckOut(master) as NewsItem;
					temp.Title = temp.Title.Trim();
					master = newsManager.Lifecycle.CheckIn(temp) as NewsItem;
					newsManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						newsManager.SaveChanges();
					}
				}

				newsManager.SaveChanges();
				newsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishBlogs(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var blogsManager = BlogsManager.GetManager(provider);
				blogsManager.Provider.SuppressSecurityChecks = false;
				var blogs = blogsManager.GetBlogs().ToList();
				var count = 0;

				foreach (var blog in blogs)
				{
					blog.Title = blog.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						blogsManager.SaveChanges();
					}
				}

				blogsManager.SaveChanges();
				blogsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishBlogPosts(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var blogsManager = BlogsManager.GetManager(provider);
				blogsManager.Provider.SuppressSecurityChecks = false;
				var blogPosts = blogsManager.GetBlogPosts().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var blogPost in blogPosts)
				{
					var master = blogsManager.Lifecycle.GetMaster(blogPost);
					var temp = blogsManager.Lifecycle.CheckOut(master) as BlogPost;
					temp.Title = temp.Title.Trim();
					master = blogsManager.Lifecycle.CheckIn(temp) as BlogPost;
					blogsManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						blogsManager.SaveChanges();
					}
				}

				blogsManager.SaveChanges();
				blogsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishCalendars(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var eventsManager = EventsManager.GetManager(provider);
				eventsManager.Provider.SuppressSecurityChecks = false;
				var calendars = eventsManager.GetCalendars().ToList();
				var count = 0;

				foreach (var calendar in calendars)
				{
					calendar.Title = calendar.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						eventsManager.SaveChanges();
					}
				}

				eventsManager.SaveChanges();
				eventsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishEvents(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var eventsManager = EventsManager.GetManager(provider);
				eventsManager.Provider.SuppressSecurityChecks = false;
				var events = eventsManager.GetEvents().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var @event in events)
				{
					var master = eventsManager.Lifecycle.GetMaster(@event);
					var temp = eventsManager.Lifecycle.CheckOut(master) as Event;
					temp.Title = temp.Title.Trim();
					master = eventsManager.Lifecycle.CheckIn(temp) as Event;
					eventsManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						eventsManager.SaveChanges();
					}
				}

				eventsManager.SaveChanges();
				eventsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishLibrariesAndFolders(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var librariesManager = LibrariesManager.GetManager(provider);
				librariesManager.Provider.SuppressSecurityChecks = false;
				var documentLibraries = librariesManager.GetDocumentLibraries().ToList();
				var count = 0;

				foreach (var documentLibrary in documentLibraries)
				{
					documentLibrary.Title = documentLibrary.Title.Trim();
					var folders = librariesManager.GetAllFolders(documentLibrary);

					foreach (var folder in folders)
					{
						folder.Title = folder.Title.Trim();
					}

					count++;
					if (count % 200 == 0)
					{
						librariesManager.SaveChanges();
					}
				}

				var imageLibraries = librariesManager.GetAlbums().ToList();
				foreach (var imageLibrary in imageLibraries)
				{
					imageLibrary.Title = imageLibrary.Title.Trim();

					var folders = librariesManager.GetAllFolders(imageLibrary);

					foreach (var folder in folders)
					{
						folder.Title = folder.Title.Trim();
					}

					count++;
					if (count % 200 == 0)
					{
						librariesManager.SaveChanges();
					}
				}

				var videoLibraries = librariesManager.GetVideoLibraries().ToList();
				foreach (var videoLibrary in videoLibraries)
				{
					videoLibrary.Title = videoLibrary.Title.Trim();

					var folders = librariesManager.GetAllFolders(videoLibrary);

					foreach (var folder in folders)
					{
						folder.Title = folder.Title.Trim();
					}

					count++;
					if (count % 200 == 0)
					{
						librariesManager.SaveChanges();
					}
				}
			}
		}

		private void RepublishImages(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var librariesManager = LibrariesManager.GetManager(provider);
				librariesManager.Provider.SuppressSecurityChecks = false;
				var images = librariesManager.GetImages().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var image in images)
				{
					var master = librariesManager.Lifecycle.GetMaster(image);
					var temp = librariesManager.Lifecycle.CheckOut(master) as Image;
					temp.Title = temp.Title.Trim();
					master = librariesManager.Lifecycle.CheckIn(temp) as Image;
					librariesManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						librariesManager.SaveChanges();
					}
				}

				librariesManager.SaveChanges();
				librariesManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishDocuments(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var librariesManager = LibrariesManager.GetManager(provider);
				librariesManager.Provider.SuppressSecurityChecks = false;
				var documents = librariesManager.GetDocuments().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var document in documents)
				{
					var master = librariesManager.Lifecycle.GetMaster(document);
					var temp = librariesManager.Lifecycle.CheckOut(master) as Document;
					temp.Title = temp.Title.Trim();
					master = librariesManager.Lifecycle.CheckIn(temp) as Document;
					librariesManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						librariesManager.SaveChanges();
					}
				}

				librariesManager.SaveChanges();
				librariesManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishVideos(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var librariesManager = LibrariesManager.GetManager(provider);
				librariesManager.Provider.SuppressSecurityChecks = false;
				var videos = librariesManager.GetVideos().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var video in videos)
				{
					var master = librariesManager.Lifecycle.GetMaster(video);
					var temp = librariesManager.Lifecycle.CheckOut(master) as Video;
					temp.Title = temp.Title.Trim();
					master = librariesManager.Lifecycle.CheckIn(temp) as Video;
					librariesManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						librariesManager.SaveChanges();
					}
				}

				librariesManager.SaveChanges();
				librariesManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishLists(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var listsManager = ListsManager.GetManager(provider);
				listsManager.Provider.SuppressSecurityChecks = false;
				var lists = listsManager.GetLists().ToList();
				var count = 0;

				foreach (var list in lists)
				{
					list.Title = list.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						listsManager.SaveChanges();
					}
				}

				listsManager.SaveChanges();
				listsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishListItems(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var listsManager = ListsManager.GetManager(provider);
				listsManager.Provider.SuppressSecurityChecks = false;
				var listItems = listsManager.GetListItems().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var listItem in listItems)
				{
					var master = listsManager.Lifecycle.GetMaster(listItem);
					var temp = listsManager.Lifecycle.CheckOut(master) as ListItem;
					temp.Title = temp.Title.Trim();
					master = listsManager.Lifecycle.CheckIn(temp) as ListItem;
					listsManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						listsManager.SaveChanges();
					}
				}

				listsManager.SaveChanges();
				listsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishForms(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var formsManager = FormsManager.GetManager(provider);
				formsManager.Provider.SuppressSecurityChecks = false;
				var forms = formsManager.GetForms().ToList();
				var count = 0;

				foreach (var form in forms)
				{
					form.Title = form.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						formsManager.SaveChanges();
					}
				}

				formsManager.SaveChanges();
				formsManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishContentBlocks(IEnumerable<string> providers)
		{
			foreach (var provider in providers)
			{
				var contentManager = ContentManager.GetManager(provider);
				contentManager.Provider.SuppressSecurityChecks = false;
				var contentBlocks = contentManager.GetContent().Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
				var count = 0;

				foreach (var contentBlock in contentBlocks)
				{
					var master = contentManager.Lifecycle.GetMaster(contentBlock);
					var temp = contentManager.Lifecycle.CheckOut(master) as NewsItem;
					temp.Title = temp.Title.Trim();
					master = contentManager.Lifecycle.CheckIn(temp) as NewsItem;
					contentManager.Lifecycle.Publish(master);

					count++;
					if (count % 200 == 0)
					{
						contentManager.SaveChanges();
					}
				}

				contentManager.SaveChanges();
				contentManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishDynamicContent(string provider, Type dynamicContentType)
		{
			var dynamicContentManager = DynamicModuleManager.GetManager(provider);
			dynamicContentManager.Provider.SuppressSecurityChecks = false;
			var dynamicContentItems = dynamicContentManager.GetDataItems(dynamicContentType).Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
			var count = 0;

			foreach (var dynamicContentItem in dynamicContentItems)
			{
				var master = dynamicContentManager.Lifecycle.GetMaster(dynamicContentItem);
				var temp = dynamicContentManager.Lifecycle.CheckOut(master) as IDynamicFieldsContainer;
				temp.SetValue("Title", temp.GetValue("Title").ToString().Trim());
				master = dynamicContentManager.Lifecycle.CheckIn(temp as ILifecycleDataItem);
				dynamicContentManager.Lifecycle.Publish(master);

				count++;
				if (count % 200 == 0)
				{
					dynamicContentManager.SaveChanges();
				}
			}

			dynamicContentManager.SaveChanges();
			dynamicContentManager.Provider.SuppressSecurityChecks = true;
		}

		private void RepublishTaxonomies(string taxonomyName, bool isFlat)
		{
			var taxonomyManager = TaxonomyManager.GetManager();
			taxonomyManager.Provider.SuppressSecurityChecks = false;
			if (isFlat)
			{
				var taxonomies = taxonomyManager.GetTaxonomies<FlatTaxonomy>().Where(t => t.Name == taxonomyName).ToList();
				var count = 0;

				foreach (var taxonomy in taxonomies)
				{
					taxonomy.Title = taxonomy.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						taxonomyManager.SaveChanges();
					}
				}

				taxonomyManager.SaveChanges();
				taxonomyManager.Provider.SuppressSecurityChecks = true;
			}
			else
			{
				var taxonomies = taxonomyManager.GetTaxonomies<HierarchicalTaxonomy>().Where(t => t.Name == taxonomyName).ToList();
				var count = 0;

				foreach (var taxonomy in taxonomies)
				{
					taxonomy.Title = taxonomy.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						taxonomyManager.SaveChanges();
					}
				}

				taxonomyManager.SaveChanges();
				taxonomyManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishTaxonItems(string taxonomyName, bool isFlat)
		{
			var taxonomyManager = TaxonomyManager.GetManager();
			taxonomyManager.Provider.SuppressSecurityChecks = false;
			if (isFlat)
			{
				var taxonomies = taxonomyManager.GetTaxa<FlatTaxon>().Where(t => t.Taxonomy.Name == taxonomyName).ToList();
				var count = 0;

				foreach (var taxonomy in taxonomies)
				{
					taxonomy.Title = taxonomy.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						taxonomyManager.SaveChanges();
					}
				}

				taxonomyManager.SaveChanges();
				taxonomyManager.Provider.SuppressSecurityChecks = true;
			}
			else
			{
				var taxonomies = taxonomyManager.GetTaxa<HierarchicalTaxon>().Where(t => t.Taxonomy.Name == taxonomyName).ToList();
				var count = 0;

				foreach (var taxonomy in taxonomies)
				{
					taxonomy.Title = taxonomy.Title.Trim();

					count++;
					if (count % 200 == 0)
					{
						taxonomyManager.SaveChanges();
					}
				}

				taxonomyManager.SaveChanges();
				taxonomyManager.Provider.SuppressSecurityChecks = true;
			}
		}

		private void RepublishGroupPagesForSite(Guid frontendRootNodeId)
		{
			var pageManager = PageManager.GetManager();
			pageManager.Provider.SuppressSecurityChecks = true;
			var pages =
                pageManager.GetPageNodes()
					.Where(
						pn =>
							pn.RootNodeId == frontendRootNodeId &&
							(pn.NodeType == NodeType.Group || pn.NodeType == NodeType.InnerRedirect ||
							 pn.NodeType == NodeType.OuterRedirect))
					.ToList();
			var count = 0;

			foreach (var pageNode in pages)
			{
				pageNode.Title = pageNode.Title.Trim();

				count++;
				if (count % 200 == 0)
				{
					pageManager.SaveChanges();
				}
			}

			pageManager.SaveChanges();
			pageManager.Provider.SuppressSecurityChecks = false;
		}

		private bool IsUserInRole(Guid userId, string roleName)
		{
			bool isUserInRole = false;

			UserManager userManager = UserManager.GetManager();
			RoleManager roleManager = RoleManager.GetManager(SecurityManager.ApplicationRolesProviderName);

			bool roleExists = roleManager.RoleExists(roleName);

			if (roleExists)
			{
				User user = userManager.GetUser(userId);
				isUserInRole = roleManager.IsUserInRole(user.Id, roleName);
			}

			return isUserInRole;
		}

		#region constants
		// Built-in content
		private const string NewsManagerTypeName = "Telerik.Sitefinity.Modules.News.NewsManager";
		private const string BlogsManagerTypeName = "Telerik.Sitefinity.Modules.Blogs.BlogsManager";
		private const string EventsManagerTypeName = "Telerik.Sitefinity.Modules.Events.EventsManager";
		private const string LibrariesManagerTypeName = "Telerik.Sitefinity.Modules.Libraries.LibrariesManager";
		private const string ListsManagerTypeName = "Telerik.Sitefinity.Modules.Lists.ListsManager";
		private const string FormsManagerTypeName = "Telerik.Sitefinity.Modules.Forms.FormsManager";
		private const string ContentManagerTypeName = "Telerik.Sitefinity.Modules.GenericContent.ContentManager";

		private static readonly string[] FlatTaxonomyNames = { "Brands", "Image Galleries", "Investors", "Media Categories", "Media Descriptions", "Media Locations", "Multi-Lens", "Tags" };
		private static readonly string[] HierarchicalTaxonomyNames = { "Categories", "Departments" };
		#endregion constants
	}
}