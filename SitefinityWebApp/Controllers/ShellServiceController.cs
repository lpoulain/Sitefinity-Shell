using SitefinityWebApp.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Script.Serialization;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Claims;
using Telerik.Sitefinity.Security.Model;
using SitefinitySupport.Logs;
using SitefinitySupport.Shell;

namespace SitefinitySupport.Controllers
{

	public class ShellServiceController : ApiController
	{
		public Output GetShellService(string cmd, string root, string rsc, string site, string provider)
		{
			CheckUser();
			ShellService svc = new ShellService(cmd, root, rsc, site, provider);
			return svc.Process_Commands();
		}

		// Used for Sitesync on the Destination server
		public List<SyncItem> GetShellService()
		{
			CheckUser();
			return ShellService.CMD_sitesync_dest();
		}

		// Checks that the user is an part of the administrator role
		// If not throw an error 404
		protected void CheckUser()
		{
			UserManager userManager = UserManager.GetManager();
			RoleManager roleManager = RoleManager.GetManager(SecurityManager.ApplicationRolesProviderName);

			// Retrieve the current user
			ClaimsIdentityProxy identity = ClaimsManager.GetCurrentIdentity();
			Guid currentUserId = identity.UserId;

			// Checks whether the user exists and is an administrator
			if (currentUserId == Guid.Empty) throw new HttpResponseException(HttpStatusCode.NotFound);
			User user = userManager.GetUser(currentUserId);
			bool isUserInRole = roleManager.IsUserInRole(user.Id, "Administrators");
			if (!isUserInRole) throw new HttpResponseException(HttpStatusCode.NotFound);
		}

	}
}