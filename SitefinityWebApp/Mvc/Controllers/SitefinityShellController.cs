using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Telerik.Sitefinity.Blogs.Model;
using Telerik.Sitefinity.Events.Model;
using Telerik.Sitefinity.Modules.Blogs;
using Telerik.Sitefinity.Mvc;
using Telerik.Sitefinity.News.Model;


namespace SitefinityWebApp.Mvc.Controllers
{
	[ControllerToolboxItem(Name = "SitefinityShell", Title = "Sitefinity Shell", SectionName = "Sitefinity Support")]
	public class SitefinityShellController : Controller
	{

		public ActionResult Index()
		{
			return View("Master");
		}
	}
}