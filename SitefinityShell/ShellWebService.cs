using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Telerik.Sitefinity.Web.Services;
using SitefinitySupport.Shell;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Claims;
using Telerik.Sitefinity.Security.Model;
using System.Net;

namespace SitefinityShell
{
	/// <summary>
	/// Sitefinity web service.
	/// </summary>
	/// <remarks>
	/// If this service is a part of a Sitefinity module,
	/// you can install it by adding this to the module's Initialize method:
	/// App.WorkWith()
	///     .Module(ModuleName)
	///     .Initialize()
	///         .WebService<ShellService>(ServiceUrl); // ServiceUrl example: "Sitefinity/Services/ModuleName/ShellService.svc/"
	/// </remarks>
	[ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public class ShellWebService : IShellWebService
	{
		#region IShellService implementation
		/// <summary>
		/// Tests the connection to the service.
		/// </summary>
		public bool TestService()
		{
			ServiceUtility.DisableCache();
			//ServiceUtility.RequestBackendUserAuthentication();
			return this.TestServiceInternal();
		}
		#endregion

		#region Private methods
		private bool TestServiceInternal()
		{
			return true;
		}
		#endregion

		public Output Command(String cmd, String root, String rsc, String site, String provider) {
			if (!CheckUser()) return null;
			ShellService svc = new ShellService(cmd, root, rsc, site, provider);
			return svc.Process_Commands();
		}

		protected bool CheckUser()
		{
			UserManager userManager = UserManager.GetManager();
			RoleManager roleManager = RoleManager.GetManager(SecurityManager.ApplicationRolesProviderName);

			// Retrieve the current user
			SitefinityIdentity identity = ClaimsManager.GetCurrentIdentity();
			Guid currentUserId = identity.UserId;

			// Checks whether the user exists and is an administrator
			if (currentUserId == Guid.Empty) return false;
			User user = userManager.GetUser(currentUserId);
			bool isUserInRole = roleManager.IsUserInRole(user.Id, "Administrators");
			if (!isUserInRole) return false;

			return true;
		}
	}
}