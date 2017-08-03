using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Telerik.Sitefinity;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Modules.GenericContent.Web.UI;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Pages.Model;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Web.UI;

namespace SitefinityShell
{
	public class ShellModule
	{
		public static void Start()
		{
			SystemManager.ApplicationStart += SystemManager_ApplicationStart;
		}
		
		private static void SystemManager_ApplicationStart(object sender, EventArgs e)
		{
			//Use Sitefinity
			//Create a page with content
			//Register it in Backend menu
			//Register services
			//Modify Routing table if you want...
			App.WorkWith()
				.Module("Shell")
				.Initialize()
				.WebService<ShellWebService>("Sitefinity/Services/ShellModule/ShellService.svc/");

			CreateBackendPage.Create();
		}

	}
}
