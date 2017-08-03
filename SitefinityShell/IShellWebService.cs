using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using Telerik.Sitefinity.Utilities.MS.ServiceModel.Web;
using SitefinitySupport.Shell;

namespace SitefinityShell
{
	[ServiceContract]
	public interface IShellWebService
	{
		/// <summary>
		/// Tests the connection to the service.
		/// </summary>
		[WebHelp(Comment = "Tests the connection to the service. Result is returned in JSON format.")]
		[WebGet(UriTemplate = "/TestConnection/", ResponseFormat = WebMessageFormat.Json)]
		[OperationContract]
		bool TestService();

		[WebInvoke(Method = "GET", UriTemplate = "/Command?cmd={cmd}&root={root}&rsc={rsc}&site={site}&provider={provider}", ResponseFormat = WebMessageFormat.Json)]
		[OperationContract]
		Output Command(String cmd, String root, String rsc, String site, String provider);
	}
}
