using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Data;

namespace SitefinityWebApp
{
	public class Global : System.Web.HttpApplication
	{

		protected void Application_Start(object sender, EventArgs e)
		{
			Bootstrapper.Initialized += new EventHandler<ExecutedEventArgs>(Bootstrapper_Initialized);
		}

		protected void Bootstrapper_Initialized(object sender, ExecutedEventArgs e)
		{
			if (e.CommandName == "Bootstrapped")
			{
				RegisterRoutes(RouteTable.Routes);
			}
		}

		public static void RegisterRoutes(RouteCollection routes)
		{

			GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
			routes.Ignore("{resource}.axd/{*pathInfo}");

			routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/",
				defaults: new { id = RouteParameter.Optional }
				);

		}

		protected void Session_Start(object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{

		}

		protected void Application_Error(object sender, EventArgs e)
		{

		}

		protected void Session_End(object sender, EventArgs e)
		{

		}

		protected void Application_End(object sender, EventArgs e)
		{

		}
	}
}