using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.SiteSync.Configuration;
using SitefinitySupport.Logs;

namespace SitefinitySupport.Shell
{
	public class SiteSyncResource : Resource
	{
		public static List<Synchronization> syncs;
		public List<string> output;

		public SiteSyncResource(IShellService theSvc)
			: base(theSvc, "SiteSync")
		{
			syncs = new List<Synchronization>();
		}
		
		public SitefinitySupport.Logs.Synchronization ReadRemote(string serverId)
		{
			SiteSyncConfig config = Config.Get<SiteSyncConfig>();
			var servers = config.ReceivingServers;
			var server = servers.Values.Where(s => s.ServerId == serverId).FirstOrDefault();
			if (server == null && servers.Values.Count == 1) server = servers.Values.First();
			if (server == null) return null;

			string URL = server.ServerAddress;

			var credentials = new NetworkCredential(server.UserName, server.Password);
			var handler = new HttpClientHandler { Credentials = credentials };

			HttpClient client = new HttpClient(handler);
			client.BaseAddress = new Uri(URL);

			// Add an Accept header for JSON format.
			client.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));

			// List data response.
			HttpResponseMessage response = client.GetAsync("api/shellservice").Result;  // Blocking call!
			if (response.IsSuccessStatusCode)
			{
				// Parse the response body. Blocking!
				string data = response.Content.ReadAsStringAsync().Result;
				JavaScriptSerializer JSserializer = new JavaScriptSerializer();
				//deserialize to your class
				List<SitefinitySupport.Logs.SyncItem> items = JSserializer.Deserialize<List<SitefinitySupport.Logs.SyncItem>>(data);

				SitefinitySupport.Logs.Synchronization sync = new SitefinitySupport.Logs.Synchronization();
				sync.items = items;

				return sync;
			}
			else
			{
				return null;
			}
		}
		
		public override string Serialize_Result()
		{
			if (summary != null) return summary;
			if (output == null) return "";

			return string.Join("\n", output);
		}

		public void GetSiteSyncDirections(out bool isSource, out bool isDest)
		{
			SiteSyncConfig config = Config.Get<SiteSyncConfig>();
			isSource = config.ReceivingServers.Count > 0;
			isDest = (config.EnabledAsTarget.HasValue && config.EnabledAsTarget.Value);
		}

		public override void CMD_list(Arguments args, Guid rootId)
		{
			bool isSource, isDest;
			GetSiteSyncDirections(out isSource, out isDest);

			if (isSource && isDest && !args.ContainsKey("src") && !args.ContainsKey("dst"))
			{
				svc.Set_Error("Please enter either 'list src' or 'list dst' ?");
				return;
			}

			string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data") + "\\Sitefinity\\Logs\\";
			bool detail = args.ContainsKey("detail");

			SynchronizationLog logs = new SynchronizationLog(path, detail, isSource && (!isDest || args.ContainsKey("src")));
			output = logs.GetDetail();
		}

		public override void CMD_compare(Arguments args)
		{
			bool isSource, isDest;
			GetSiteSyncDirections(out isSource, out isDest);

			if (!isSource)
			{
				svc.Set_Error("This command can only be run on a server configured as a SiteSync source");
				return;
			}

			string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data") + "\\Sitefinity\\Logs\\";
			bool detail = args.ContainsKey("detail");
			SynchronizationLog logs = new SynchronizationLog(path, detail, true);

			foreach (string serverId in logs.GetServerIds())
			{
				logs.AddRemoteDestination(serverId, ReadRemote(serverId));
			}

			output = logs.Compare();
		}

		public List<SyncItem> CMD_sitesync_dest()
		{
			string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data") + "\\Sitefinity\\Logs\\";
			SynchronizationLog logs = new SynchronizationLog(path, false, false);
			return logs.GetSyncItems();
		}

		public override void CMD_help()
		{
			summary =
				"list [detail]: displays the synchronizations that failed\n" +
				"compare [detail]: same as list but compares with the logs from the destination\n";

			base.CMD_help();
		}

	}
}