using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SitefinitySupport.Logs
{
	public class SynchronizationLog
	{
		protected List<Synchronization> sourceSyncs;
		protected Dictionary<string, Synchronization> destSyncs;
		protected bool detail;

		public SynchronizationLog(string path, bool detail, bool isSrc=true)
		{
			this.detail = detail;
			destSyncs = new Dictionary<string, Synchronization>();
			sourceSyncs = Read(path, isSrc ? typeof(SourceSynchronization) : typeof(DestSynchronization));
		}

		public HashSet<string> GetServerIds()
		{
			return new HashSet<string>(sourceSyncs.Select(s => s.serverId));
		}

		public List<SyncItem> GetSyncItems()
		{
			if (sourceSyncs.Count() == 0) return new List<SyncItem>();

			return sourceSyncs.First().items;
		}

		public void AddRemoteDestination(string serverId, Synchronization sync)
		{
			if (!destSyncs.ContainsKey(serverId))
				destSyncs.Add(serverId, sync);
		}

		public void AddDestination(string path)
		{
			Synchronization dest = Read(path, typeof(DestSynchronization)).First();

			foreach (Synchronization sourceSync in sourceSyncs)
			{
				if (!destSyncs.ContainsKey(sourceSync.serverId))
					destSyncs.Add(sourceSync.serverId, dest);
			}
		}

		public List<string> GetDetail()
		{
			List<string> output = new List<string>();

			foreach (Synchronization sync in sourceSyncs)
			{
				DateTime? syncTime = null;
				int nbTotal = 0, nbFailed = 0;

				foreach (SyncItem itemSrc in sync.items.Where(i => i.type.StartsWith("Telerik.Sitefinity.") &&
																   i.type != "Telerik.Sitefinity.Configuration.ConfigSection"))
				{
					nbTotal++;

					if (!syncTime.HasValue)
					{
						syncTime = itemSrc.timestamp;
					}

					if (itemSrc.success) continue;

					nbFailed++;

					// Compact output
					if (!detail)
					{
						output.Add(string.Format("{0} {1} ({2})", itemSrc.id, itemSrc.type.Split('.').Last(), itemSrc.name));
						continue;
					}

					// Detailed output
					output.Add(string.Format("{0} - {1}", itemSrc.timestamp, itemSrc.type));
					output.Add(string.Format("{0} ({1})", itemSrc.id, itemSrc.name));
					output.Add(itemSrc.error);
					output.Add("");
				}

				if (nbTotal > 0)
				{
					output.Add(string.Format("Sync at {0} (server ID: {3}): {1}/{2} successfully", syncTime, nbTotal - nbFailed, nbTotal, sync.serverId));
					output.Add("=================================");
				}
			}

			return output;
		}

		public List<string> Compare()
		{
			List<string> output = new List<string>();

			foreach (Synchronization sync in sourceSyncs)
			{
				// Get the synchronization time from the first entry
				DateTime? syncTime = null;
				if (sync.items.Count > 0) syncTime = sync.items[0].timestamp;

				if (!destSyncs.ContainsKey(sync.serverId) ||
					destSyncs[sync.serverId] == null)
				{

					output.Add(string.Format("Sync at {0} (server ID: {1}): could not reach Destination server", syncTime, sync.serverId));
					output.Add("=================================");
					continue;
				}

				List<SyncItem> dest = destSyncs[sync.serverId].items;

				int nbTotal = 0, nbFailed = 0;

				foreach (SyncItem itemSrc in sync.items.Where(i => i.type.StartsWith("Telerik.Sitefinity.") &&
																   i.type != "Telerik.Sitefinity.Configuration.ConfigSection" &&
																   i.type != "Telerik.Sitefinity.Multisite.Model.Site"))
				{
					nbTotal++;

					SyncItem itemDst = dest.Where(i => i.id == itemSrc.id &&
													   i.timestamp >= itemSrc.timestamp.AddMinutes(-1) &&
													   i.timestamp <= itemSrc.timestamp.AddMinutes(1)).FirstOrDefault();

					if (itemDst != null && itemSrc.success && itemDst.success) continue;

					nbFailed++;

					// Compact output
					if (!detail)
					{
						if (itemDst == null)
							output.Add(string.Format("{0} {1} ({2})", itemSrc.id, itemSrc.type.Split('.').Last(), itemSrc.name));
						else
							output.Add(string.Format("{0} {1} ({2}): {3}", itemSrc.id, itemSrc.type.Split('.').Last(), itemSrc.name, itemDst.error.Split('\n')[0]));

						continue;
					}

					// Detailed output
					output.Add(string.Format("{0} - {1}", itemSrc.timestamp, itemSrc.type));
					output.Add(string.Format("{0} ({1})", itemSrc.id, itemSrc.name));

					if (itemDst == null)
					{
						if (itemSrc.success)
							output.Add(string.Format("Source says synced, no trace on target"));
						else
						{
							output.Add(string.Format("Source could not reach target"));
							output.Add(string.Format(itemSrc.error.Split('\n')[0]));
						}
					}
					else
					{
						output.Add(itemSrc.timestamp.ToString());

						if (!itemSrc.success && !itemDst.success &&
							itemSrc.error.StartsWith("Error details:System.ArgumentOutOfRangeException: InternalServerError (500)"))
						{
							output.Add(string.Format("Error on Destination"));
							output.Add(itemDst.error);
						}
						else if (itemSrc.success && !itemDst.success)
						{
							output.Add(string.Format("Error on Destination (Source is OK!!!)"));
							output.Add(itemDst.error);
						}
						else if (!itemSrc.success && itemDst.success)
						{
							output.Add(string.Format("Error on Source (Destination is OK!!!)"));
							output.Add(itemSrc.error.Split('\n')[0]);
						}
						else
						{
							output.Add(string.Format("{0} ({1}): error on both sides", itemSrc.id, itemSrc.name));
							output.Add(itemSrc.error.Split('\n')[0]);
							output.Add(itemDst.error);
						}
					}

					output.Add("");
				}

				if (nbTotal > 0)
				{
					output.Add(string.Format("Sync at {0} (server ID: {3}): {1}/{2} successfully", syncTime, nbTotal - nbFailed, nbTotal, sync.serverId));
					output.Add("=================================");
				}
			}

			return output;
		}

		// Helper functions
		protected IEnumerable<string> ReadFiles(string path)
		{
			string[] files = Directory.GetFiles(path, "Synchronization*.log");

			foreach (string file in files)
			{
				if (File.Exists(file))
				{
					using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
					using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
					{
						string line;
						while ((line = sr.ReadLine()) != null)
						{
							yield return line;
						}
					}
				}
			}
		}


		protected List<Synchronization> Read(string path, Type syncClass)
		{
			List<Synchronization> syncs = new List<Synchronization>();
			List<SyncItem> allItems = new List<SyncItem>();
			Synchronization sync = Activator.CreateInstance(syncClass) as Synchronization;
			SyncItem item = null;
			bool captureError = false;
			bool captureSyncHeader = false;

			foreach (string line in ReadFiles(path))
			{
				if (captureSyncHeader)
				{
					Regex regex = new Regex("^{\"FailedItemsRetryCount(.*)\"ServerId\":\"(.*?)\"");
					Match match = regex.Match(line);
					if (match.Success)
					{
						sync.serverId = match.Groups[2].Value;
					}
					captureSyncHeader = false;
				}
				else if (line.StartsWith("---------------------------------------"))
				{
					item = null;
					captureError = false;
				}
				else if (line.Contains("] Sync task ") && line.EndsWith(" execution started with the following settings:"))
				{
					captureSyncHeader = true;
				}
				else if (line.Contains("] Immediate sync requested by "))
				{
					sync = Activator.CreateInstance(syncClass) as Synchronization;
					syncs.Add(sync);
				}
				else if (sync.IsNewItem(line))
				{
					item = sync.NewItem(line);
					if (sync != null) sync.AddItem(item);
					allItems.Add(item);
				}
				else if (line.StartsWith("Item information: ") && item != null)
				{
					item.SetId(line);
				}
				else if (item != null && (line.StartsWith("Error details:") || captureError))
				{
					item.AddError(line);
					captureError = true;
				}
			}

			if (syncs.Count == 0) syncs.Add(sync);

			return syncs;
		}

	}
}
