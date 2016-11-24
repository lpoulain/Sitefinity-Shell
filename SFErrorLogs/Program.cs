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
			int nbArgs = args.Count();

			if (nbArgs == 0) {
				Console.Out.WriteLine("Usage:");
				Console.Out.WriteLine("  SitefinityErrorLog.exe <error log path> summary [url]");
				Console.Out.WriteLine("      url: groups by URL instead of error message");
				Console.Out.WriteLine("  SitefinityErrorLog.exe <error log path> [all] [-filter <field>=<value>] [display <field1> <field2> ...]");
				Console.Out.WriteLine("      all: reads all the Error.*.log files and not just Error.log");
				Console.Out.WriteLine("      filters: url, message");
				Console.Out.WriteLine("      display: timestamp, url, message, stack, fullstack");
				return;
			}

			string path = args.First();
			bool all = false;
			int argNb = 1;
			Dictionary<string, string> filters = new Dictionary<string,string>();
			HashSet<string> displayFields = new HashSet<string>();

			if (argNb < nbArgs && args[argNb].ToLower() == "summary")
			{
				argNb++;
				string output = ErrorLog.Summary(path, (nbArgs >= argNb + 1 ? args[argNb].ToLower() : ""));
				Console.Out.WriteLine(output);
				return;
			}

			if (argNb < nbArgs && args[argNb].ToLower() == "all")
			{
				all = true;
				argNb++;
			}

			if (argNb < nbArgs && args[argNb].ToLower() == "-filter")
			{
				argNb++;

				while (argNb < nbArgs && args[argNb].ToLower() != "-display")
				{
					string[] keyValuePair = args[argNb++].Split('=');
					if (keyValuePair.Length >= 2)
					{
						filters.Add(keyValuePair[0], keyValuePair[1]);
					}
				}
			}

			if (argNb < nbArgs && args[argNb].ToLower() == "-display")
			{
				argNb++;

				while (argNb < nbArgs)
				{
					displayFields.Add(args[argNb++]);
				}
			}

			ErrorLog logs = new ErrorLog(path, all);
			if (filters.Count() > 0) logs.Filter(filters);
			if (displayFields.Count() > 0) logs.SetDisplayFields(displayFields);

			// Prints the output
			Console.Out.WriteLine(logs.Display());
		}
	}
}
