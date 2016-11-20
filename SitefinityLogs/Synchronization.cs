using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SitefinitySupport.Logs
{
	public class Synchronization
	{
		public int nbSyncs;
		public int nbErrors;
		public string serverId;
		public List<SyncItem> items;

		public void AddItem(SyncItem item)
		{
			if (item.success)
				nbSyncs++;
			else
				nbErrors++;
			items.Add(item);
		}

		public Synchronization()
		{
			nbErrors = 0;
			nbSyncs = 0;
			items = new List<SyncItem>();
		}

		public virtual bool IsNewItem(string line) { return false; }
		public virtual SyncItem NewItem(string descr) { return null; }

		public List<string> Output(List<Synchronization> dest)
		{
			List<string> output = new List<string>();



			return output;
		}
	}

	public class SourceSynchronization : Synchronization
	{
		public override bool IsNewItem(string line)
		{
			return SourceSyncItem.IsNewItem(line);
		}

		public override SyncItem NewItem(string descr)
		{
			return new SourceSyncItem(descr) as SyncItem;
		}
	}

	public class DestSynchronization : Synchronization
	{
		public override bool IsNewItem(string line)
		{
			return DestSyncItem.IsNewItem(line);
		}

		public override SyncItem NewItem(string descr)
		{
			return new DestSyncItem(descr) as SyncItem;
		}
	}
}
