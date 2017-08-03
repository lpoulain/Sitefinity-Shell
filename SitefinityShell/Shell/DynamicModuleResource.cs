using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.DynamicModules.Builder;
using Telerik.Sitefinity.DynamicModules.Model;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Multisite;
using Telerik.Sitefinity.Multisite.Model;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Utilities.TypeConverters;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.DynamicModules.Builder.Model;
using Telerik.Sitefinity.Data.Metadata;

namespace SitefinitySupport.Shell
{
	public class DynamicModuleResource : Resource
	{
		protected List<DynamicModuleType> types;
		protected List<DynamicContent> items;

		public DynamicModuleResource(IShellService svc) : base(svc, "Dynamic Modules") {
			display = new HashSet<string> { "Title" };
		}

		public string ProviderName(string name)
		{
			return name == "OpenAccessProvider" ? "Default" : name;
		}

		public override void CMD_cd(Arguments args, Guid rootId)
		{
			// Go to the root
			if (args.Count == 0)
			{
				svc.Set_Root(Guid.Empty);
				svc.Set_Path("Dynamic Modules");
				svc.Set_Provider("");
				return;
			}

			// Select a type
			try {
				Guid newRootId = new Guid(args.FirstKey);
				var dynType = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleType(newRootId);
				Type type = TypeResolutionService.ResolveType(dynType.GetFullTypeName());
				svc.Set_Root(newRootId);
				svc.Set_Path(ProviderName(svc.Get_Provider()) + " - " + type.FullName.Substring(38));
				return;
			}
			catch (Exception) { }

			svc.Set_Error("Invalid path: " + args.FirstKey);

		}

		public override void CMD_filter(Arguments args)
		{
			if (items == null) return;

			List<Func<DynamicContent, bool>> filters = new List<Func<DynamicContent, bool>>();

			foreach (string arg in args.Keys)
			{
				string key = arg.UpperFirstLetter();
				string value = args[arg];

				if (value.EndsWith("*"))
					filters.Add(c => c.GetValue<Lstring>(key).Value.ToLower().StartsWith(value.TrimEnd('*')));
				else
					filters.Add(c => c.GetValue<Lstring>(key).Value.ToLower() == value);
			}

			Func<DynamicContent, bool> finalFilter = p => filters.All(predicate => predicate(p));
			items = items.Where(finalFilter).ToList();
		}

		public override void CMD_provider(Arguments args, Guid rootId)
		{
			List<string> providers = new List<string>();

			var dynTypes = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleTypes();
			MultisiteManager msManager = MultisiteManager.GetManager();
			var links = msManager.GetSiteDataSourceLinks();

			foreach (var datalink in links.Where(l => !l.DataSourceName.StartsWith("Telerik.Sitefinity.")))
			{
				if (dynTypes.Where(mt => mt.ModuleName == datalink.DataSourceName).FirstOrDefault() != null)
					providers.Add(ProviderName(datalink.ProviderName));
			}

			// Displays the list of providers
			if (args.Count == 0)
			{
				string currentProvider = ProviderName(svc.Get_Provider());
				summary = string.Join("\n", providers.Select(provider => provider + (provider == currentProvider ? " <=" : "")));
				return;
			}

			// Selects a provider
			string newProvider = providers.Where(provider => provider.ToLower() == args.FirstKey).FirstOrDefault();

			if (newProvider == null)
			{
				svc.Set_Error("Invalid provider: " + args.FirstKey);
				return;
			}

			if (rootId != Guid.Empty)
			{
				var dynType = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleType(rootId);
				Type type = TypeResolutionService.ResolveType(dynType.GetFullTypeName());
				svc.Set_Path(newProvider + " - " + type.FullName.Substring(38));
			}
			newProvider = newProvider == "Default" ? "OpenAccessProvider" : newProvider;
			svc.Set_Provider(newProvider);
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			types = new List<DynamicModuleType>();

			// Top root: display the types
			if (rootId == Guid.Empty)
			{
				Site site = svc.Get_Site();

				var multisiteContext = SystemManager.CurrentContext as MultisiteContext;
				var theSite = multisiteContext.GetSiteById(site.Id);
				var dynTypes = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleTypes();

				HashSet<Guid> typeIds = new HashSet<Guid>();

				// Dynamic Modules
				foreach (var datalink in site.SiteDataSourceLinks.Where(dsl => !dsl.DataSourceName.StartsWith("Telerik.Sitefinity.")))
				{
					foreach (var dynType in dynTypes.Where(mt => mt.ModuleName == datalink.DataSourceName))
					{
						if (typeIds.Contains(dynType.Id)) continue;
						types.Add(dynType);
						typeIds.Add(dynType.Id);
					}
/*						types.Add(dynType);
						Type type = TypeResolutionService.ResolveType(dynType.GetFullTypeName());
						types.Add(type);
					}*/
				}
				return;
			}

			// Inside a specific type: display the items
			var dynType2 = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleType(rootId);
			Type type2 = TypeResolutionService.ResolveType(dynType2.GetFullTypeName());

			var dynamicContentManager = DynamicModuleManager.GetManager(svc.Get_Provider());
			dynamicContentManager.Provider.SuppressSecurityChecks = false;
			items = dynamicContentManager.GetDataItems(type2).Where(n => n.Visible && n.Status == ContentLifecycleStatus.Live).ToList();
		}

		private HashSet<string> GetDisplayFields(HashSet<string> display)
		{
			// Finds the fields
			var dynType = ModuleBuilderManager.GetManager().Provider.GetDynamicModuleType(svc.Get_Root());
			Type type = TypeResolutionService.ResolveType(dynType.GetFullTypeName());
			var metadataManager = MetadataManager.GetManager();
			var contentMetaType = metadataManager.GetMetaType(type);
			HashSet<string> fields = new HashSet<string>(contentMetaType.Fields.Select(f => f.FieldName));
			HashSet<string> actualDisplay = new HashSet<string>();
			foreach (string field in display)
			{
				if (fields.Contains(field) || field == "id")
				{
					actualDisplay.Add(field);
					continue;
				}

				if (fields.Contains(field.UpperFirstLetter()))
				{
					actualDisplay.Add(field.UpperFirstLetter());
					continue;
				}

				string actualField = fields.Where(f => f.ToLower() == field.ToLower()).FirstOrDefault();
				if (actualField != null)
					actualDisplay.Add(actualField);
			}

			return actualDisplay;
		}

		public override string Serialize_Result()
		{
			if (summary != null) return summary;

			// The type is already chosen, display the item types
			if (items != null)
			{
				HashSet<string> actualDisplay = GetDisplayFields(display);
				return string.Join("\n", items.Select(i => string.Join(" - ", actualDisplay.Select(fieldName => fieldName == "id" ? i.Id.ToString() : i.GetValue<Lstring>(fieldName).Value))));
			}

			// items and types are null: output-less command
			if (types == null) return "";

			// Displays the types
			return string.Join("\n", types.Select(t => t.Id.ToString() + " - " + TypeResolutionService.ResolveType(t.GetFullTypeName()).FullName.Substring(38)));
		}

		public override void CMD_help()
		{
			summary =
				"list: displays the content types (if at the root) or the content items\n" +
				"cd [id]: selects the content type\n" +
				"display <fields>: selects the fields to display\n";

			base.CMD_help();
		}

	}
}