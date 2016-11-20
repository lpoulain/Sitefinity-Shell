using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SitefinitySupport.Logs;

namespace SitefinitySupport
{
	class Program
	{
		static void Main(string[] args)
		{
			int nbArgs = args.Length;
			if (nbArgs < 2 || args[0].ToLower() == "-detail" && nbArgs < 3) {
				Console.Out.WriteLine("Looks for errors in the SiteSync logs");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Usage:");
				Console.Out.WriteLine("  SitefinitySiteSyncLogs.exe [-detail] -src <log path>");
				Console.Out.WriteLine("  SitefinitySiteSyncLogs.exe [-detail] -dest <log path>");
				Console.Out.WriteLine("  SitefinitySiteSyncLogs.exe [-detail] <source log path> <destination log path>");
				return;
			}

			SynchronizationLog logs;
			List<string> output;
			bool detail = false;
			int argNb = 0;

			if (args[argNb].ToLower() == "-detail")
			{
				detail = true;
				argNb++;
			}

			if (args[argNb].ToLower() == "-src")
			{
				logs = new SynchronizationLog(args[argNb + 1], detail, true);
				output = logs.GetDetail();
			}
			else if (args[argNb].ToLower() == "-dest")
			{
				logs = new SynchronizationLog(args[argNb + 1], detail, false);
				output = logs.GetDetail();
			}
			else
			{
				logs = new SynchronizationLog(args[argNb], detail, true);
				logs.AddDestination(args[argNb + 1]);
				output = logs.Compare();
			}

			Console.Out.Write(string.Join("\n", output));
		}
	}
}
